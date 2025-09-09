// -----------------------------------------------------------------------
// ActiveBidTracker.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using DkpParser.LiveTracking;
using DkpParser.Zeal;
using Gjeltema.Logging;

public sealed class ActiveBidTracker : IActiveBidTracker
{
    private const string LogPrefix = $"[{nameof(ActiveBidTracker)}]";
    private readonly ActiveBiddingAnalyzer _activeBiddingAnalyzer;
    private readonly ActiveBossKillAnalyzer _activeBossKillAnalyzer;
    private readonly ActiveAuctionEndAnalyzer _auctionEndAnalyzer;
    private readonly ActiveAuctionStartAnalyzer _auctionStartAnalyzer;
    private readonly ChannelAnalyzer _channelAnalyzer;
    private readonly ItemLinkValues _itemLinkValues;
    private readonly IMessageProvider _messageProvider;
    private readonly ConcurrentQueue<CharacterReadyCheckStatus> _readyCheckStatus = new();
    private readonly DelimiterStringSanitizer _sanitizer = new();
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
        _auctionEndAnalyzer = new();
        _activeBiddingAnalyzer = new(settings);
        _activeBossKillAnalyzer = new();
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
                .OrderByDescending(x => x.BidAmount)
                .ThenBy(x => x.Timestamp)
                .DistinctBy(x => x.CharacterBeingBidFor)
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

    public string GetRemoveSpentMessageWithLink(SuggestedSpentCall spentCall)
    {
        if (spentCall == null)
            return string.Empty;

        string channel = GetChannelShortcut(spentCall.Channel);
        string itemLink = _itemLinkValues.GetItemLink(spentCall.ItemName);
        return spentCall.IsRoll
            ? $"{channel} {Constants.AttendanceDelimiter}{itemLink}{Constants.AttendanceDelimiter} {spentCall.Winner} rolled {spentCall.DkpSpent} {Constants.RollWin} {Constants.Remove}"
            : $"{channel} {Constants.AttendanceDelimiter}{itemLink}{Constants.AttendanceDelimiter} {spentCall.Winner} {spentCall.DkpSpent} {Constants.DkpSpent} {Constants.Remove}";
    }

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
        => _messageProvider.StartMessages(fileName, ProcessMessage);

    public void StopTracking()
        => _messageProvider.StopMessages();

    public void TakeAttendanceSnapshot(string raidName, AttendanceCallType callType)
    {
        string eqDirectory = _settings.EqDirectory;
        if (string.IsNullOrEmpty(eqDirectory))
            return;

        if (ZealAttendanceMessageProvider.Instance.RaidInfo.RaidAttendees.Count == 0)
        {
            Log.Info($"{LogPrefix} No values in Zeal {nameof(ZealAttendanceMessageProvider.Instance.RaidInfo.RaidAttendees)}, ending processing of AttendanceCall: {raidName} {callType}.");
            throw new InvalidZealAttendanceData("No attendandees in Zeal.");
        }

        if (ZealAttendanceMessageProvider.Instance.RaidInfo.IsDataStale)
        {
            Log.Info($"{LogPrefix} Data in Zeal {nameof(ZealAttendanceMessageProvider.Instance.RaidInfo)} is stale. Ending processing of AttendanceCall: {raidName} {callType}.");
            throw new InvalidZealAttendanceData("Zeal attendees list is stale.");
        }

        if (ZealAttendanceMessageProvider.Instance.CharacterInfo.IsDataStale)
        {
            Log.Info($"{LogPrefix} Data in Zeal {nameof(ZealAttendanceMessageProvider.Instance.CharacterInfo)} is stale. Ending processing of AttendanceCall: {raidName} {callType}.");
            throw new InvalidZealAttendanceData("Zeal character data is stale.");
        }

        int zoneId = ZealAttendanceMessageProvider.Instance.CharacterInfo.ZoneId;
        if (!_settings.ZoneIdMapping.TryGetValue(zoneId, out string zoneName))
        {
            Log.Debug($"{LogPrefix} Zone mapping not found for Zone ID {zoneId} in attendance call, ending processing of AttendanceCall: {raidName} {callType}.");
            throw new InvalidZealAttendanceData("Invalid Zeal Zone ID.");
        }

        string fileName = string.Format(Constants.ZealAttendanceBasedFileNameFormat, DateTime.Now.ToString(Constants.ZealRaidAttendanceFileNameTimeFormat));
        string fullFilePath = Path.Combine(eqDirectory, fileName);

        string firstLine = ZealRaidAttendanceFile.GetFirstLine(raidName, zoneName, callType);
        IEnumerable<string> characters = ZealAttendanceMessageProvider.Instance.RaidInfo.RaidAttendees
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Name)
            .Select(x => ZealRaidAttendanceFile.GetFileLine(x.Group, x.Name, x.Class, x.Level, x.Rank));

        IEnumerable<string> fileContents = [firstLine, .. characters];

        WriteToFile(fullFilePath, fileContents);
    }

    public bool TryGetReadyCheckStatus(out CharacterReadyCheckStatus readyStatus)
        => _readyCheckStatus.TryDequeue(out readyStatus);

    private List<LiveBidInfo> GetAllMaxRolls(LiveAuctionInfo auction, bool lowRollWins)
    {
        IEnumerable<LiveBidInfo> filteredBids = _bids
            .Where(x => x.ParentAuctionId == auction.Id);

        if (lowRollWins)
            return filteredBids
                .OrderBy(x => x.BidAmount)
                .ThenBy(x => x.Timestamp)
                .DistinctBy(x => x.CharacterPlacingBid)
                .Take(auction.TotalNumberOfItems)
                .ToList();
        else
            return filteredBids
                .OrderByDescending(x => x.BidAmount)
                .ThenBy(x => x.Timestamp)
                .DistinctBy(x => x.CharacterPlacingBid)
                .Take(auction.TotalNumberOfItems)
                .ToList();
    }

    private string GetChannelShortcut(EqChannel channel)
        => channel switch
        {
            EqChannel.Raid => "/rs",
            EqChannel.Guild => "/gu",
            EqChannel.Group => "/g",
            _ => "/rs",
        };

    private string GetMessageSenderName(ReadOnlySpan<char> logLine)
    {
        int indexOfSpace = logLine.IndexOf(' ');
        if (indexOfSpace < 3)
            return string.Empty;

        string auctioneerName = logLine[0..indexOfSpace].Trim().ToString();
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
                {
                    int newNumberOfItems = Math.Max(existingAuction.TotalNumberOfItems, newAuction.TotalNumberOfItems);
                    Log.Debug($"{LogPrefix} Overwiting existing auction [{existingAuction}] {nameof(LiveAuctionInfo.TotalNumberOfItems)} with {newNumberOfItems} due to auction [{newAuction}]");
                    existingAuction.TotalNumberOfItems = newNumberOfItems;
                    existingAuction.Channel = newAuction.Channel;
                }

                continue;
            }

            _activeAuctions = _activeAuctions.Add(newAuction);
        }
    }

    private void ProcessMessage(string message)
    {
        if (!message.TryExtractEqLogTimeStamp(out DateTime timestamp))
            return;

        try
        {
            // +1 to remove the following space.
            string logLineNoTimestamp = message[(Constants.EqLogDateTimeLength + 1)..];

            string bossKilledName = _activeBossKillAnalyzer.GetBossKillName(logLineNoTimestamp);
            if (bossKilledName != null)
            {
                _bossKilledName = bossKilledName;
                Log.Debug($"{LogPrefix} Extracted boss name: {bossKilledName} from line: {message}");
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

            if (TrackReadyCheck && (isValidDkpChannel || channel == EqChannel.ReadyCheck))
            {
                if (logLineNoTimestamp.Contains(Constants.PossibleErrorDelimiter) || logLineNoTimestamp.Contains(Constants.AlternateDelimiter))
                {
                    string sanitizedLogLine = _sanitizer.SanitizeDelimiterString(logLineNoTimestamp);
                    string noWhitespaceLogLine = sanitizedLogLine.RemoveAllWhitespace();

                    if (noWhitespaceLogLine.Contains(Constants.ReadyCheckWithDelimiter) || noWhitespaceLogLine.Contains(Constants.ReadyCheckAlternateDelimiter))
                    {
                        Log.Debug($"{LogPrefix} Ready Check initiated: {message}");
                        _readyCheckInitiated = true;
                        Updated = true;
                        return;
                    }
                    else if (noWhitespaceLogLine.Contains(Constants.ReadyWithDelimiter, StringComparison.OrdinalIgnoreCase)
                        || noWhitespaceLogLine.Contains(Constants.ReadyAlternateDelimiter, StringComparison.OrdinalIgnoreCase))
                    {
                        string senderName = GetMessageSenderName(logLineNoTimestamp);
                        _readyCheckStatus.Enqueue(new CharacterReadyCheckStatus { CharacterName = senderName, IsReady = true });
                        Log.Debug($"{LogPrefix} {senderName} is READY: {message}");
                        Updated = true;
                        return;
                    }
                    else if (noWhitespaceLogLine.Contains(Constants.NotReadyWithDelimiter, StringComparison.OrdinalIgnoreCase)
                        || noWhitespaceLogLine.Contains(Constants.NotReadyAlternateDelimiter, StringComparison.OrdinalIgnoreCase))
                    {
                        string senderName = GetMessageSenderName(logLineNoTimestamp);
                        _readyCheckStatus.Enqueue(new CharacterReadyCheckStatus { CharacterName = senderName, IsReady = false });
                        Log.Debug($"{LogPrefix} {senderName} is NOT READY: {message}");
                        Updated = true;
                        return;
                    }
                }
            }

            // Include Group so that the tool can be used in xp groups
            if (!isValidDkpChannel && channel != EqChannel.Group)
                return;

            string messageSenderName = GetMessageSenderName(logLineNoTimestamp);
            int indexOfFirstQuote = logLineNoTimestamp.IndexOf('\'');
            string messageFromPlayer = logLineNoTimestamp.AsSpan()[(indexOfFirstQuote + 1)..^1].Trim().ToString();

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
                        Log.Debug($"{LogPrefix} Duplicate bid made.  Replacing old bid: {possibleDuplicateBid}, with new bid: {bid}");
                    }

                    _bids = _bids.Add(bid);

                    Updated = true;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error processing messages: {ex.ToLogMessage()}");
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
                Log.Info($"{LogPrefix} REMOVE applied to spent call {existingSpentCall}");
                return true;
            }

            Log.Info($"{LogPrefix} REMOVE call made, but no associated SPENT call found: {spentCall}");
            return false;
        }

        LiveAuctionInfo existingAuction = _activeAuctions.FirstOrDefault(x => x.ItemName == spentCall.ItemName);
        if (existingAuction == null)
        {
            Log.Info($"{LogPrefix} SPENT call made, but no associated auction found: {spentCall}");
            return false;
        }

        spentCall.AuctionStart = existingAuction;
        if (existingAuction.TotalNumberOfItems > 1)
            existingAuction.SetStatusUpdate(spentCall.Auctioneer, spentCall.Timestamp, "SPENT");

        LiveBidInfo bid = _bids.FirstOrDefault(x => x.CharacterBeingBidFor == spentCall.Winner);
        spentCall.CharacterPlacingBid = bid?.CharacterPlacingBid ?? spentCall.Winner;

        _spentCalls = _spentCalls.Add(spentCall);

        List<LiveSpentCall> spentCalls = _spentCalls.Where(x => x.AuctionStart.Id == existingAuction.Id).ToList();

        if (spentCalls.Count >= existingAuction.TotalNumberOfItems)
        {
            _activeAuctions = _activeAuctions.Remove(existingAuction);
            CompletedAuction newCompletedCall = new()
            {
                AuctionStart = existingAuction,
                ItemName = spentCall.ItemName,
                SpentCalls = spentCalls
            };
            Log.Debug($"{LogPrefix} SPENT call made, Completed call created: {newCompletedCall}");
            _completedAuctions = _completedAuctions.Add(newCompletedCall);
            return true;
        }

        Log.Debug($"{LogPrefix} SPENT call made, but not enough SPENT calls to complete the auction. SPENT call: {spentCall}; Associated Auction: {existingAuction}");
        return true;
    }

    private bool SpentCallExists(LiveBidInfo bid)
        => _spentCalls.Any(x => x.AuctionStart.Id == bid.ParentAuctionId
                && x.DkpSpent == bid.BidAmount
                && x.ItemName == bid.ItemName
                && x.Winner.Equals(bid.CharacterBeingBidFor, StringComparison.OrdinalIgnoreCase));

    private void WriteToFile(string fileToWriteTo, IEnumerable<string> fileContents)
    {
        try
        {
            Task.Run(() => File.WriteAllLines(fileToWriteTo, fileContents));
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error writing out Zeal attendance info: {ex.ToLogMessage()}");
            throw new InvalidZealAttendanceData("Unable to write out Zeal attendance data to a file.");
        }
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

    void TakeAttendanceSnapshot(string raidName, AttendanceCallType callType);
}
