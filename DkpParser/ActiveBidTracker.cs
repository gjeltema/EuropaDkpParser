// -----------------------------------------------------------------------
// ActiveBidTracker.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Collections.Immutable;
using System.Diagnostics;
using DkpParser.LiveTracking;

public sealed class ActiveBidTracker : IActiveBidTracker
{
    private readonly ActiveBiddingAnalyzer _activeBiddingAnalyzer;
    private readonly ActiveAuctionEndAnalyzer _auctionEndAnalyzer;
    private readonly ActiveAuctionStartAnalyzer _auctionStartAnalyzer;
    private readonly ChannelAnalyzer _channelAnalyzer;
    private readonly ItemLinkValues _itemLinkValues;
    private readonly IMessageProvider _messageProvider;
    private readonly IDkpParserSettings _settings;
    private ImmutableList<LiveAuctionInfo> _activeAuctions;
    private ImmutableList<LiveBidInfo> _bids;
    private ImmutableList<CompletedAuction> _completedAuctions;
    private ImmutableList<LiveSpentCall> _spentCalls;

    //** To DO
    // Handle REMOVE calls
    // Error reporting

    public ActiveBidTracker(IDkpParserSettings settings)
    {
        _settings = settings;

        _channelAnalyzer = new(settings);
        _messageProvider = new TailFile(ProcessMessage, ProcessErrorMessage);
        _auctionStartAnalyzer = new();
        _auctionEndAnalyzer = new(ProcessErrorMessage);
        _activeBiddingAnalyzer = new();
        _itemLinkValues = settings.ItemLinkIds;

        _activeAuctions = [];
        _spentCalls = [];
        _completedAuctions = [];
        _bids = [];
    }

    public IEnumerable<LiveAuctionInfo> ActiveAuctions
        => _activeAuctions;

    public IEnumerable<LiveBidInfo> Bids
        => _bids;

    public IEnumerable<CompletedAuction> CompletedAuctions
        => _completedAuctions;

    public bool Updated { get; set; }

    public ICollection<LiveBidInfo> GetHighBids(LiveAuctionInfo auction)
    {
        if (auction == null)
            return [];

        return _bids
            .Where(x => x.ParentAuctionId == auction.Id)
            .OrderBy(x => x.Timestamp)
            .OrderByDescending(x => x.BidAmount)
            .Take(auction.TotalNumberOfItems)
            .ToList();
    }

    public string GetNextStatusMarkerForSelection(string currentMarker)
        => currentMarker switch
        {
            "COMP" => "60s",
            "10s" => "COMP",
            "30s" => "10s",
            "60s" => "30s",
            _ => "60s",
        };

    public ICollection<LiveSpentCall> GetSpentInfoForCurrentHighBids(LiveAuctionInfo auction)
    {
        if (auction == null)
            return [];

        IEnumerable<LiveBidInfo> highBids = GetHighBids(auction);
        string channel = GetChannelShortcut(auction.Channel);
        return highBids
            .Select(x => new LiveSpentCall
            {
                Timestamp = x.Timestamp,
                Auctioneer = auction.Auctioneer,
                AuctionStart = auction,
                Channel = auction.Channel,
                ItemName = x.ItemName,
                DkpSpent = x.BidAmount,
                Winner = x.CharacterName
            })
            .ToList();
    }

    public string GetSpentMessageWithLink(LiveSpentCall spentCall)
    {
        if (spentCall == null)
            return string.Empty;

        string channel = GetChannelShortcut(spentCall.Channel);
        string itemLink = _itemLinkValues.GetItemLink(spentCall.ItemName);
        return $"{channel} {Constants.AttendanceDelimiter}{itemLink}{Constants.AttendanceDelimiter} {spentCall.Winner} {spentCall.DkpSpent} {Constants.DkpSpent}";
    }

    public StatusMarker GetStatusMarkerFromSelectionString(string statusString)
        => statusString switch
        {
            "COMP" => StatusMarker.Completed,
            "10s" => StatusMarker.TenSeconds,
            "30s" => StatusMarker.ThirtySeconds,
            "60s" => StatusMarker.SixtySeconds,
            _ => StatusMarker.SixtySeconds,
        };

    public string GetStatusMessage(LiveAuctionInfo auction, StatusMarker statusMarker)
    {
        if (auction == null)
            return string.Empty;

        string channel = GetChannelShortcut(auction.Channel);
        IEnumerable<LiveBidInfo> highBids = GetHighBids(auction);
        string highBiddersString = string.Join(',', highBids.Select(x => $"{x.CharacterName} {x.BidAmount}"));
        string statusString = GetStatusString(statusMarker);
        string itemLink = _itemLinkValues.GetItemLink(auction.ItemName);
        return $"{channel} {Constants.AttendanceDelimiter}{itemLink}{Constants.AttendanceDelimiter} {highBiddersString} {statusString}";
    }

    public void ReactivateCompletedAuction(CompletedAuction auction)
    {
        if (auction == null)
            return;

        _activeAuctions = _activeAuctions.Add(auction.AuctionStart);
        _completedAuctions = _completedAuctions.Remove(auction);
        Updated = true;
    }

    public void RemoveBid(LiveBidInfo bidToRemove)
    {
        if (bidToRemove == null)
            return;

        _bids = _bids.Remove(bidToRemove);
        Updated = true;
    }

    public void SetAuctionToCompleted(LiveAuctionInfo auctionToComplete)
    {
        if (auctionToComplete == null)
            return;

        ICollection<LiveSpentCall> spentCalls = _spentCalls.Where(x => x.AuctionStart.Id == auctionToComplete.Id).ToList();
        CompletedAuction completedAuction = new()
        {
            AuctionStart = auctionToComplete,
            ItemName = auctionToComplete.ItemName,
            SpentCalls = spentCalls
        };

        _completedAuctions = _completedAuctions.Add(completedAuction);
        _activeAuctions = _activeAuctions.Remove(auctionToComplete);

        Updated = true;
    }

    public void StartTracking(string fileName)
    {
        _messageProvider.StartMessages(fileName);
    }

    public void StopTracking()
    {
        _messageProvider.StopMessages();
    }

    private string GetChannelShortcut(EqChannel channel)
        => channel switch
        {
            EqChannel.Raid => "/rs",
            EqChannel.Guild => "/gu",
            EqChannel.Ooc => "/ooc",
            EqChannel.Auction => "/auc",
            _ => "/rs",
        };

    private string GetStatusString(StatusMarker statusMarker)
        => statusMarker switch
        {
            StatusMarker.Completed => "COMPLETED",
            StatusMarker.TenSeconds => "10s",
            StatusMarker.ThirtySeconds => "30s",
            StatusMarker.SixtySeconds => "60s",
            _ => "60s",
        };

    private void ProcessErrorMessage(string message)
    {
    }

    private void ProcessMessage(string message)
    {
        if (!message.TryExtractEqLogTimeStamp(out DateTime timestamp))
            return;

        string logLineNoTimestamp = message[(Constants.LogDateTimeLength + 1)..];

        EqChannel channel = _channelAnalyzer.GetValidDkpChannel(logLineNoTimestamp);
        if (channel == EqChannel.None)
            return;

        ICollection<LiveAuctionInfo> auctionStarts = _auctionStartAnalyzer.GetAuctionStart(logLineNoTimestamp, channel, timestamp);
        if (auctionStarts.Count > 0)
        {
            foreach (LiveAuctionInfo newAuction in auctionStarts)
            {
                LiveAuctionInfo existingAuction = _activeAuctions.FirstOrDefault(x => x == newAuction);
                if (existingAuction != null)
                {
                    existingAuction.TotalNumberOfItems = Math.Max(existingAuction.TotalNumberOfItems, newAuction.TotalNumberOfItems);
                    continue;
                }

                _activeAuctions = _activeAuctions.Add(newAuction);
            }

            Updated = true;
            return;
        }

        LiveSpentCall spentCall = _auctionEndAnalyzer.GetSpentCall(logLineNoTimestamp, channel, timestamp);
        if (spentCall != null)
        {
            LiveAuctionInfo existingAuction = _activeAuctions.FirstOrDefault(x => x.ItemName == spentCall.ItemName);
            if (existingAuction != null)
            {
                spentCall.AuctionStart = existingAuction;
                _spentCalls = _spentCalls.Add(spentCall);

                ICollection<LiveSpentCall> spentCalls = _spentCalls.Where(x => x.AuctionStart.Id == existingAuction.Id).ToList();

                if (spentCalls.Count == existingAuction.TotalNumberOfItems)
                {
                    _activeAuctions = _activeAuctions.Remove(existingAuction);
                    _completedAuctions = _completedAuctions.Add(new CompletedAuction
                    {
                        AuctionStart = existingAuction,
                        ItemName = spentCall.ItemName,
                        SpentCalls = spentCalls
                    });
                }
            }

            Updated = true;
            return;
        }

        LiveBidInfo bid = _activeBiddingAnalyzer.GetBidInformation(logLineNoTimestamp, channel, timestamp, _activeAuctions);
        if (bid != null)
        {
            _bids = _bids.Add(bid);

            Updated = true;
            return;
        }
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class CompletedAuction
{
    public LiveAuctionInfo AuctionStart { get; set; }

    public string ItemName { get; set; }

    public ICollection<LiveSpentCall> SpentCalls { get; set; }

    private string DebugText
        => $"{ItemName} {AuctionStart.Id}";

    public override string ToString()
        => $"{AuctionStart.Timestamp:HH:mm} {ItemName}";
}

public interface IActiveBidTracker
{
    IEnumerable<LiveAuctionInfo> ActiveAuctions { get; }

    IEnumerable<LiveBidInfo> Bids { get; }

    IEnumerable<CompletedAuction> CompletedAuctions { get; }

    ICollection<LiveBidInfo> GetHighBids(LiveAuctionInfo auction);

    string GetNextStatusMarkerForSelection(string currentMarker);

    ICollection<LiveSpentCall> GetSpentInfoForCurrentHighBids(LiveAuctionInfo auction);

    string GetSpentMessageWithLink(LiveSpentCall spentCall);

    StatusMarker GetStatusMarkerFromSelectionString(string statusString);

    string GetStatusMessage(LiveAuctionInfo auction, StatusMarker statusMarker);

    void ReactivateCompletedAuction(CompletedAuction auction);

    void RemoveBid(LiveBidInfo bidToRemove);

    void SetAuctionToCompleted(LiveAuctionInfo auctionToComplete);

    void StartTracking(string fileName);

    void StopTracking();
}
