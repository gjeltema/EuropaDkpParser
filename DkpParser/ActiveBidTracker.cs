﻿// -----------------------------------------------------------------------
// ActiveBidTracker.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using DkpParser.LiveTracking;
using Gjeltema.Logging;

public sealed class ActiveBidTracker : IActiveBidTracker
{
    private const string ErrorFileName = "Errors_LiveBidTracking.txt";
    private readonly ActiveBiddingAnalyzer _activeBiddingAnalyzer;
    private readonly ActiveBossKillAnalyzer _activeBossKillAnalyzer;
    private readonly ActiveAuctionEndAnalyzer _auctionEndAnalyzer;
    private readonly ActiveAuctionStartAnalyzer _auctionStartAnalyzer;
    private readonly ChannelAnalyzer _channelAnalyzer;
    private readonly string _errorFileName;
    private readonly ItemLinkValues _itemLinkValues;
    private readonly IMessageProvider _messageProvider;
    private readonly ConcurrentQueue<CharacterReadyCheckStatus> _readyCheckStatus = new();
    private readonly IDkpParserSettings _settings;
    private ImmutableList<LiveAuctionInfo> _activeAuctions;
    private ImmutableList<LiveBidInfo> _bids;
    private string _bossKilledName;
    private ImmutableList<CompletedAuction> _completedAuctions;
    private bool _readyCheckInitiated;
    private ImmutableList<LiveSpentCall> _spentCalls;

    public ActiveBidTracker(IDkpParserSettings settings, IMessageProvider messageProvider)
    {
        _settings = settings;

        _channelAnalyzer = new(settings);
        _messageProvider = messageProvider;
        _auctionStartAnalyzer = new();
        _auctionEndAnalyzer = new(ProcessErrorMessage);
        _activeBiddingAnalyzer = new(settings);
        _activeBossKillAnalyzer = new();
        _itemLinkValues = settings.ItemLinkIds;

        _activeAuctions = [];
        _spentCalls = [];
        _completedAuctions = [];
        _bids = [];

        _errorFileName = Path.Combine(settings.OutputDirectory, ErrorFileName);
    }

    public IEnumerable<LiveAuctionInfo> ActiveAuctions
        => _activeAuctions;

    public IEnumerable<LiveBidInfo> Bids
        => _bids;

    public IEnumerable<CompletedAuction> CompletedAuctions
        => _completedAuctions;

    public bool ReadyCheckInitiated
    {
        get
        {
            bool readyCheck = _readyCheckInitiated;
            _readyCheckInitiated = false;
            return readyCheck;
        }
    }

    public bool TrackReadyCheck { get; set; }

    public bool Updated { get; set; }

    public string GetBossKilledName()
    {
        string bossName = _bossKilledName;
        _bossKilledName = null;
        return bossName;
    }

    public ICollection<LiveBidInfo> GetHighBids(LiveAuctionInfo auction, bool lowRollWins)
    {
        if (auction == null)
            return [];

        if (auction.IsRoll)
            return GetAllMaxRolls(auction, lowRollWins);
        else
            return _bids
                .Where(x => x.ParentAuctionId == auction.Id)
                .OrderBy(x => x.Timestamp)
                .ThenByDescending(x => x.BidAmount)
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

    public ICollection<SuggestedSpentCall> GetSpentInfoForCurrentHighBids(LiveAuctionInfo auction, bool lowRollWins)
    {
        if (auction == null)
            return [];

        IEnumerable<LiveBidInfo> highBids = GetHighBids(auction, lowRollWins);

        string channel = GetChannelShortcut(auction.Channel);
        return highBids
            .Select(x => new SuggestedSpentCall
            {
                Channel = auction.Channel,
                ItemName = x.ItemName,
                DkpSpent = x.BidAmount,
                Winner = x.CharacterBeingBidFor,
                IsRoll = auction.IsRoll,
                SpentCallSent = SpentCallExists(x)
            })
            .ToList();
    }

    public string GetSpentMessageWithLink(SuggestedSpentCall spentCall)
    {
        if (spentCall == null)
            return string.Empty;

        string channel = GetChannelShortcut(spentCall.Channel);
        string itemLink = _itemLinkValues.GetItemLink(spentCall.ItemName);
        return spentCall.IsRoll
            ? $"{channel} {Constants.AttendanceDelimiter}{itemLink}{Constants.AttendanceDelimiter} {spentCall.Winner} rolled {spentCall.DkpSpent} {Constants.RollWin}"
            : $"{channel} {Constants.AttendanceDelimiter}{itemLink}{Constants.AttendanceDelimiter} {spentCall.Winner} {spentCall.DkpSpent} {Constants.DkpSpent}";
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

    public string GetStatusMessage(LiveAuctionInfo auction, StatusMarker statusMarker, bool lowRollWins)
    {
        if (auction == null)
            return string.Empty;

        ICollection<LiveBidInfo> highBids = GetHighBids(auction, lowRollWins);
        if (highBids.Count == 0)
            return string.Empty;

        string statusString = GetStatusString(statusMarker);

        if (auction.IsRoll)
        {
            string channel = GetChannelShortcut(auction.Channel);
            string highRollersString = string.Join(", ", highBids.Select(x => $"{x.CharacterPlacingBid} {x.BidAmount}"));
            string itemLink = _itemLinkValues.GetItemLink(auction.ItemName);
            string highOrLow = lowRollWins ? "Low" : "High";
            string rollsText = highBids.Count > 1 ? "rolls" : "roll";
            return $"{channel} {highOrLow} {rollsText} for {itemLink} by {highRollersString} {statusString}";
        }
        else
        {
            string channel = GetChannelShortcut(auction.Channel);
            string highBiddersString = string.Join(", ", highBids.Select(x => $"{x.CharacterBeingBidFor} {x.BidAmount} DKP"));
            string itemLink = _itemLinkValues.GetItemLink(auction.ItemName);
            return $"{channel} {Constants.AttendanceDelimiter}{itemLink}{Constants.AttendanceDelimiter} {highBiddersString} {statusString}";
        }
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

        LiveAuctionInfo auction = _activeAuctions.FirstOrDefault(x => x.Id == bidToRemove.ParentAuctionId);
        if (auction != null)
        {
            if (!_bids.Any(x => x.ParentAuctionId == auction.Id))
                auction.HasBids = false;
        }

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
        => _messageProvider.StartMessages(fileName, ProcessMessage, ProcessErrorMessage);

    public void StopTracking()
        => _messageProvider.StopMessages();

    public bool TryGetReadyCheckStatus(out CharacterReadyCheckStatus readyStatus)
        => _readyCheckStatus.TryDequeue(out readyStatus);

    private List<LiveBidInfo> GetAllMaxRolls(LiveAuctionInfo auction, bool lowRollWins)
    {
        IOrderedEnumerable<LiveBidInfo> orderedBids = _bids
            .Where(x => x.ParentAuctionId == auction.Id)
            .OrderBy(x => x.Timestamp);

        if (lowRollWins)
            return orderedBids
                .ThenBy(x => x.BidAmount)
                .Take(auction.TotalNumberOfItems)
                .ToList();
        else
            return orderedBids
                .ThenByDescending(x => x.BidAmount)
                .Take(auction.TotalNumberOfItems)
                .ToList();
    }

    private string GetChannelShortcut(EqChannel channel)
        => channel switch
        {
            EqChannel.Raid => "/rs",
            EqChannel.Guild => "/gu",
            EqChannel.Ooc => "/ooc",
            EqChannel.Auction => "/auc",
            EqChannel.Group => "/g",
            _ => "/rs",
        };

    private string GetMessageSenderName(string logLine)
    {
        int indexOfSpace = logLine.IndexOf(' ');
        if (indexOfSpace < 3)
            return string.Empty;

        string auctioneerName = logLine[0..indexOfSpace].Trim();
        return auctioneerName;
    }

    private string GetStatusString(StatusMarker statusMarker)
        => statusMarker switch
        {
            StatusMarker.Completed => "COMPLETED",
            StatusMarker.TenSeconds => "10s",
            StatusMarker.ThirtySeconds => "30s",
            StatusMarker.SixtySeconds => "60s",
            _ => "60s",
        };

    private void HandleRollByPlayer(LiveBidInfo rollInfo)
    {
        if (rollInfo == null)
            return;

        _bids = _bids.Add(rollInfo);

        Updated = true;
    }

    private void ProcessAuctionStart(ICollection<LiveAuctionInfo> auctionStarts)
    {
        foreach (LiveAuctionInfo newAuction in auctionStarts)
        {
            LiveAuctionInfo existingAuction = _activeAuctions.FirstOrDefault(x => x == newAuction);
            if (existingAuction != null)
            {
                if (!existingAuction.IsRoll)
                    existingAuction.TotalNumberOfItems = Math.Max(existingAuction.TotalNumberOfItems, newAuction.TotalNumberOfItems);

                continue;
            }

            _activeAuctions = _activeAuctions.Add(newAuction);
        }
    }

    private void ProcessErrorMessage(string message)
    {
        string messageToWrite = $"{DateTime.Now:HH:mm:ss} {message}";
        Task.Run(() => WriteToErrorFile(messageToWrite));
    }

    private void ProcessMessage(string message)
    {
        if (!message.TryExtractEqLogTimeStamp(out DateTime timestamp))
            return;

        try
        {
            // +1 to remove the following space.
            string logLineNoTimestamp = message[(Constants.LogDateTimeLength + 1)..];
            string messageSenderName = GetMessageSenderName(logLineNoTimestamp);

            if (TrackReadyCheck && logLineNoTimestamp.Contains(Constants.PossibleErrorDelimiter))
            {
                string noWhitespaceLogLine = logLineNoTimestamp.RemoveAllWhitespace();

                if (noWhitespaceLogLine.Contains(Constants.ReadyCheck))
                {
                    _readyCheckInitiated = true;
                    Updated = true;
                    return;
                }
                else if (noWhitespaceLogLine.Contains(Constants.Ready))
                {
                    _readyCheckStatus.Enqueue(new CharacterReadyCheckStatus { CharacterName = messageSenderName, IsReady = true });
                    Updated = true;
                    return;
                }
                else if (noWhitespaceLogLine.Contains(Constants.NotReady))
                {
                    _readyCheckStatus.Enqueue(new CharacterReadyCheckStatus { CharacterName = messageSenderName, IsReady = false });
                    Updated = true;
                    return;
                }
            }

            string bossKilledName = _activeBossKillAnalyzer.GetBossKillName(logLineNoTimestamp);
            if (bossKilledName != null)
            {
                _bossKilledName = bossKilledName;
                Updated = true;
                return;
            }

            LiveBidInfo rollInfo = _activeBiddingAnalyzer.GetRollInfo(logLineNoTimestamp, timestamp, _activeAuctions);
            if (rollInfo != null)
            {
                HandleRollByPlayer(rollInfo);
            }

            EqChannel channel = _channelAnalyzer.GetChannel(logLineNoTimestamp);
            if (channel == EqChannel.None)
                return;

            bool isValidDkpChannel = _channelAnalyzer.IsValidDkpChannel(channel);

            // Include Group so that the tool can be used in xp groups
            if (!isValidDkpChannel && channel != EqChannel.Group)
                return;

            int indexOfFirstQuote = logLineNoTimestamp.IndexOf('\'');
            string messageFromPlayer = logLineNoTimestamp[(indexOfFirstQuote + 1)..^1].Trim();

            ICollection<LiveAuctionInfo> auctionStarts = _auctionStartAnalyzer.GetAuctionStart(messageFromPlayer, channel, timestamp, messageSenderName);
            if (auctionStarts.Count > 0)
            {
                ProcessAuctionStart(auctionStarts);

                Updated = true;
                return;
            }

            LiveSpentCall spentCall = _auctionEndAnalyzer.GetSpentCall(messageFromPlayer, channel, timestamp, messageSenderName);
            if (spentCall != null)
            {
                Updated = ProcessSpentCall(spentCall);
                return;
            }

            if (isValidDkpChannel)
            {
                LiveBidInfo bid = _activeBiddingAnalyzer.GetBidInformation(messageFromPlayer, channel, timestamp, messageSenderName, _activeAuctions);
                if (bid != null)
                {
                    LiveBidInfo possibleDuplicateBid = _bids.FirstOrDefault(x => x == bid);
                    if (possibleDuplicateBid != null)
                    {
                        _bids = _bids.Remove(possibleDuplicateBid);
                    }

                    _bids = _bids.Add(bid);

                    Updated = true;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ProcessErrorMessage($"Error processing message: {ex}");
            Log.Error($"Error processing messages: {ex}");
        }
    }

    private bool ProcessSpentCall(LiveSpentCall spentCall)
    {
        if (spentCall.IsRemoveCall)
        {
            LiveSpentCall existingSpentCall = _spentCalls
                .FirstOrDefault(x => x.Winner == spentCall.Winner && x.ItemName == spentCall.ItemName && x.DkpSpent == spentCall.DkpSpent);
            if (existingSpentCall != null)
            {
                _spentCalls.Remove(existingSpentCall);
                return true;
            }

            return false;
        }

        LiveAuctionInfo existingAuction = _activeAuctions.FirstOrDefault(x => x.ItemName == spentCall.ItemName);
        if (existingAuction == null)
            return false;

        spentCall.AuctionStart = existingAuction;
        if (existingAuction.TotalNumberOfItems > 1)
            existingAuction.SetStatusUpdate(spentCall.Auctioneer, spentCall.Timestamp);

        LiveBidInfo bid = _bids.FirstOrDefault(x => x.CharacterBeingBidFor == spentCall.Winner);
        spentCall.CharacterPlacingBid = bid?.CharacterPlacingBid ?? spentCall.Winner;

        _spentCalls = _spentCalls.Add(spentCall);

        ICollection<LiveSpentCall> spentCalls = _spentCalls.Where(x => x.AuctionStart.Id == existingAuction.Id).ToList();

        if (spentCalls.Count >= existingAuction.TotalNumberOfItems)
        {
            _activeAuctions = _activeAuctions.Remove(existingAuction);
            _completedAuctions = _completedAuctions.Add(new CompletedAuction
            {
                AuctionStart = existingAuction,
                ItemName = spentCall.ItemName,
                SpentCalls = spentCalls
            });

            return true;
        }

        return false;
    }

    private bool SpentCallExists(LiveBidInfo bid)
        => _spentCalls.Any(x => x.AuctionStart.Id == bid.ParentAuctionId
                && x.DkpSpent == bid.BidAmount
                && x.ItemName == bid.ItemName
                && x.Winner.Equals(bid.CharacterBeingBidFor, StringComparison.OrdinalIgnoreCase));

    private void WriteToErrorFile(string message)
    {
        try
        {
            File.AppendAllLines(_errorFileName, [message]);
        }
        catch { }
    }
}

public interface IActiveBidTracker
{
    IEnumerable<LiveAuctionInfo> ActiveAuctions { get; }

    IEnumerable<LiveBidInfo> Bids { get; }

    IEnumerable<CompletedAuction> CompletedAuctions { get; }

    bool ReadyCheckInitiated { get; }

    bool TrackReadyCheck { get; set; }

    bool Updated { get; set; }

    string GetBossKilledName();

    ICollection<LiveBidInfo> GetHighBids(LiveAuctionInfo auction, bool lowRollWins);

    string GetNextStatusMarkerForSelection(string currentMarker);

    ICollection<SuggestedSpentCall> GetSpentInfoForCurrentHighBids(LiveAuctionInfo auction, bool lowRollWins);

    string GetSpentMessageWithLink(SuggestedSpentCall spentCall);

    StatusMarker GetStatusMarkerFromSelectionString(string statusString);

    string GetStatusMessage(LiveAuctionInfo auction, StatusMarker statusMarker, bool lowRollWins);

    void ReactivateCompletedAuction(CompletedAuction auction);

    void RemoveBid(LiveBidInfo bidToRemove);

    void SetAuctionToCompleted(LiveAuctionInfo auctionToComplete);

    void StartTracking(string fileName);

    void StopTracking();
}
