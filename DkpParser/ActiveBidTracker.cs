// -----------------------------------------------------------------------
// ActiveBidTracker.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using DkpParser.LiveTracking;
using System.Collections.Immutable;

public sealed class ActiveBidTracker : IActiveBidTracker
{
    private readonly List<LiveAuctionInfo> _activeAuctions;
    private readonly ActiveBiddingAnalyzer _activeBiddingAnalyzer;
    private readonly ActiveAuctionEndAnalyzer _auctionEndAnalyzer;
    private readonly ActiveAuctionStartAnalyzer _auctionStartAnalyzer;
    private readonly List<LiveBidInfo> _bids;
    private readonly ChannelAnalyzer _channelAnalyzer;
    private readonly ICollection<CompletedAuction> _completedAuctions = new HashSet<CompletedAuction>(60);
    private readonly IMessageProvider _messageProvider;
    private readonly IDkpParserSettings _settings;
    private readonly List<LiveSpentCall> _spentCalls;

    //** To DO
    // Get item links
    //      Ability to specify item number, and save it
    // Handle REMOVE calls
    // Make collections concurrent

    public ActiveBidTracker(IDkpParserSettings settings)
    {
        _settings = settings;

        _channelAnalyzer = new(settings);
        _messageProvider = new TailFile(ProcessMessage, ProcessErrorMessage);
        _auctionStartAnalyzer = new();
        _auctionEndAnalyzer = new(ProcessErrorMessage);
        _activeBiddingAnalyzer = new();
        _activeAuctions = new(8);
        _spentCalls = new(60);
        _bids = new(1000);
    }

    public ICollection<LiveAuctionInfo> ActiveAuctions
        => _activeAuctions;

    public ICollection<LiveBidInfo> Bids
        => _bids;

    public ICollection<CompletedAuction> CompletedAuctions
        => _completedAuctions;

    public bool Updated { get; set; }

    //** Change to have item link
    public ICollection<LiveBidInfo> GetHighBids(LiveAuctionInfo auction)
        => _bids
            .Where(x => x.ParentAuctionId == auction.Id)
            .OrderByDescending(x => x.BidAmount)
            .OrderBy(x => x.Timestamp)
            .Take(auction.TotalNumberOfItems)
            .ToList();

    public ICollection<LiveSpentCall> GetSpentMessagesForCurrentHighBids(LiveAuctionInfo auction)
    {
        ICollection<LiveBidInfo> highBids = GetHighBids(auction);
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

    public StatusMarker GetStatusMarkerFromString(string statusString)
        => statusString switch
        {
            "COMPLETED" => StatusMarker.Completed,
            "10s" => StatusMarker.TenSeconds,
            "30s" => StatusMarker.ThirtySeconds,
            "60s" => StatusMarker.SixtySeconds,
            _ => StatusMarker.SixtySeconds,
        };

    public string GetStatusMessage(LiveAuctionInfo auction, StatusMarker statusMarker)
    {
        string channel = GetChannelShortcut(auction.Channel);
        ICollection<LiveBidInfo> highBids = GetHighBids(auction);
        string highBiddersString = string.Join(',', highBids.Select(x => $"{x.CharacterName} {x.BidAmount}"));
        string statusString = GetStatusString(statusMarker);
        return $"{channel} {Constants.AttendanceDelimiter}{auction.ItemName}{Constants.AttendanceDelimiter} {highBiddersString} {statusString}";
    }

    public void ReactivateCompletedAuction(CompletedAuction auction)
    {
        _activeAuctions.Add(auction.AuctionStart);
        _completedAuctions.Remove(auction);
        Updated = true;
    }

    public void RemoveBid(LiveBidInfo bidToRemove)
    {
        _bids.Remove(bidToRemove);
        Updated = true;
    }

    public void SetAuctionToCompleted(LiveAuctionInfo auctionToComplete)
    {
        ICollection<LiveSpentCall> spentCalls = _spentCalls.Where(x => x.AuctionStart.Id == auctionToComplete.Id).ToList();
        CompletedAuction completedAuction = new()
        {
            AuctionStart = auctionToComplete,
            ItemName = auctionToComplete.ItemName,
            SpentCalls = spentCalls
        };

        _completedAuctions.Add(completedAuction);
        _activeAuctions.Remove(auctionToComplete);
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

                _activeAuctions.Add(newAuction);
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
                _spentCalls.Add(spentCall);

                ICollection<LiveSpentCall> spentCalls = _spentCalls.Where(x => x.AuctionStart.Id == existingAuction.Id).ToList();

                if (spentCalls.Count == existingAuction.TotalNumberOfItems)
                {
                    _activeAuctions.Remove(existingAuction);
                    _completedAuctions.Add(new CompletedAuction
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
            _bids.Add(bid);

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
    ICollection<LiveAuctionInfo> ActiveAuctions { get; }

    ICollection<LiveBidInfo> Bids { get; }

    ICollection<CompletedAuction> CompletedAuctions { get; }

    ICollection<LiveBidInfo> GetHighBids(LiveAuctionInfo auction);

    ICollection<LiveSpentCall> GetSpentMessagesForCurrentHighBids(LiveAuctionInfo auction);

    StatusMarker GetStatusMarkerFromString(string statusString);

    string GetStatusMessage(LiveAuctionInfo auction, StatusMarker statusMarker);

    void ReactivateCompletedAuction(CompletedAuction auction);

    void RemoveBid(LiveBidInfo bidToRemove);

    void SetAuctionToCompleted(LiveAuctionInfo auctionToComplete);

    void StartTracking(string fileName);

    void StopTracking();
}
