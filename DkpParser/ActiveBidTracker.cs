// -----------------------------------------------------------------------
// ActiveBidTracker.cs Copyright 2026 Craig Gjeltema
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
    private readonly object _bossKilledLock = new();
    private readonly IEqLogTailFile _eqLogTailFile;
    private readonly ItemLinkValues _itemLinkValues;
    private readonly ConcurrentQueue<CharacterReadyCheckStatus> _readyCheckStatus = new();
    private readonly IDkpParserSettings _settings;
    private ImmutableList<LiveAuctionInfo> _activeAuctions;
    private ImmutableList<LiveBidInfo> _bids;
    private string _bossKilledName;
    private ImmutableList<CompletedAuction> _completedAuctions;
    private ImmutableList<string> _currentAfks;
    private string _lastBossKilled;
    private DateTime _lastBossTime = DateTime.MinValue;
    private bool _listeningForEvents = false;
    private ImmutableList<MezBreak> _mezBreaks;
    private bool _readyCheckInitiated;
    private ImmutableList<LiveSpentCall> _spentCalls;

    public ActiveBidTracker(IDkpParserSettings settings, IEqLogTailFile eqLogTailFile)
    {
        _settings = settings;
        _eqLogTailFile = eqLogTailFile;

        _activeBiddingAnalyzer = new(settings);
        _itemLinkValues = settings.ItemLinkIds;

        _activeAuctions = [];
        _spentCalls = [];
        _completedAuctions = [];
        _bids = [];
        _currentAfks = [];
        _mezBreaks = [];
    }

    public IEnumerable<LiveAuctionInfo> ActiveAuctions
        => _activeAuctions;

    public IEnumerable<LiveBidInfo> Bids
        => _bids;

    public IEnumerable<CompletedAuction> CompletedAuctions
        => _completedAuctions;

    public IEnumerable<string> CurrentAfks
        => _currentAfks;

    public bool IsParsingLogFile
        => _eqLogTailFile.IsSendingMessages;

    public IEnumerable<MezBreak> MezBreaks
        => _mezBreaks;

    public bool ReadyCheckInitiated
    {
        get
        {
            bool readyCheck = _readyCheckInitiated;
            _readyCheckInitiated = false;
            return readyCheck;
        }
    }

    public bool TrackBossKills
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            if (field)
                _eqLogTailFile.BossKilledMessage += HandleBossKilled;
            else
                _eqLogTailFile.BossKilledMessage -= HandleBossKilled;
        }
    }

    public bool TrackReadyCheck
    {
        get;
        set
        {
            field = value;
            if (field)
            {
                _eqLogTailFile.ReadyCheckInitiatedMessage += HandleReadyCheckInitiated;
                _eqLogTailFile.CharacterReadyCheckMessage += HandleCharacterReadyCheck;
            }
            else
            {
                _eqLogTailFile.ReadyCheckInitiatedMessage -= HandleReadyCheckInitiated;
                _eqLogTailFile.CharacterReadyCheckMessage -= HandleCharacterReadyCheck;
            }
        }
    }

    public bool Updated { get; set; }

    public bool CheckIfSnipe(SuggestedSpentCall spentCall)
    {
        bool spentNameInLastStatusMessage = spentCall.ParentAuction.LastStatusMessage?.Contains(spentCall.Winner, StringComparison.OrdinalIgnoreCase) ?? true;
        return !spentNameInLastStatusMessage;
    }

    public string GetBossKilledName()
    {
        string bossName = null;
        lock (_bossKilledLock)
        {
            bossName = _bossKilledName;
            _bossKilledName = null;
        }
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
                ParentAuction = auction,
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
        _eqLogTailFile.AddAuctionItem(auction.AuctionStart.ItemName);
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
        _eqLogTailFile.RemoveAuctionItem(auctionToComplete.ItemName);

        Updated = true;
    }

    public void StartTracking()
    {
        _eqLogTailFile.StartMessages();
        ListenForUpdates();
    }

    public void StartTracking(string fileName)
    {
        _eqLogTailFile.StartMessages(fileName);
        ListenForUpdates();
    }

    public void StopTracking()
    {
        _listeningForEvents = false;
        _eqLogTailFile.AuctionStartMessage -= HandleAuctionStart;
        _eqLogTailFile.BidInfoMessage -= HandleBid;
        _eqLogTailFile.RollMessage -= HandleRoll;
        _eqLogTailFile.AfkCommandMessage -= HandleAfkCommand;
        _eqLogTailFile.MezBreakMessage -= HandleMezBreak;
        _eqLogTailFile.SpentCallMessage -= HandleSpentCall;
        _eqLogTailFile.ReadyCheckInitiatedMessage -= HandleReadyCheckInitiated;
        _eqLogTailFile.CharacterReadyCheckMessage -= HandleCharacterReadyCheck;
        _eqLogTailFile.BossKilledMessage -= HandleBossKilled;
    }

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

    private string GetStatusString(StatusMarker statusMarker)
        => statusMarker switch
        {
            StatusMarker.Completed => "COMPLETED",
            StatusMarker.TenSeconds => "10s",
            StatusMarker.ThirtySeconds => "30s",
            StatusMarker.SixtySeconds => "60s",
            _ => "60s",
        };

    private void HandleAfkCommand(object sender, AfkCommandEventArgs e)
    {
        if (e.StartAfk)
        {
            _currentAfks = _currentAfks.Add(e.CharacterName);
        }
        else
        {
            _currentAfks = _currentAfks.Remove(e.CharacterName);
        }
        Updated = true;
    }

    private void HandleAuctionStart(object sender, AuctionStartEventArgs e)
    {
        LiveAuctionInfo newAuction = e.AuctionStart;
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

            return;
        }

        _activeAuctions = _activeAuctions.Add(newAuction);
        _eqLogTailFile.AddAuctionItem(newAuction.ItemName);
    }

    private void HandleBid(object sender, BidInfoEventArgs e)
    {
        LiveBidInfo bid = _activeBiddingAnalyzer.ProcessBidInfo(e.BidInfo, _activeAuctions);
        if (bid == null)
            return;

        LiveBidInfo possibleDuplicateBid = _bids.FirstOrDefault(x => x == bid);
        if (possibleDuplicateBid != null)
        {
            _bids = _bids.Remove(possibleDuplicateBid);
            Log.Debug($"{LogPrefix} Duplicate bid made.  Replacing old bid: {possibleDuplicateBid}, with new bid: {bid}");
        }

        _bids = _bids.Add(bid);

        Updated = true;
    }

    private void HandleBossKilled(object sender, BossKilledEventArgs e)
    {
        string bossName = e.BossName;
        Log.Debug($"{LogPrefix} Checking Boss Killed: {bossName}");
        if (_lastBossKilled == bossName && DateTime.Now.AddSeconds(-30) < _lastBossTime)
        {
            Log.Debug($"{LogPrefix} Ignoring. LastBossKilled: {_lastBossKilled}, LastTime: {_lastBossTime:T}");
            return;
        }

        if (_settings.RaidValue.UseTimeOnlyWithConfiguredKillCalls)
        {
            if (!_settings.RaidValue.OnlyKillCalls.Contains(bossName))
                return;
        }

        lock (_bossKilledLock)
        {
            _bossKilledName = bossName;
        }
        _lastBossKilled = bossName;
        _lastBossTime = DateTime.Now;

        Updated = true;
    }

    private void HandleCharacterReadyCheck(object sender, CharacterReadyCheckEventArgs e)
    {
        _readyCheckStatus.Enqueue(e.ReadyCheckStatus);
        Updated = true;
    }

    private void HandleMezBreak(object sender, MezBreakEventArgs e)
    {
        _mezBreaks = _mezBreaks.Add(e.MezBreak);
        Log.Debug($"{LogPrefix} Added Mez Break: [{e.MezBreak}].");
        Updated = true;
    }

    private void HandleReadyCheckInitiated(object sender, EventArgs e)
    {
        _readyCheckInitiated = true;
        Updated = true;
    }

    private void HandleRoll(object sender, RawRollInfoEventArgs e)
    {
        LiveBidInfo rollInfo = _activeBiddingAnalyzer.ProcessRollInfo(e.RollInfo, _activeAuctions);
        if (rollInfo != null)
        {
            _bids = _bids.Add(rollInfo);
            Updated = true;
        }
    }

    private void HandleSpentCall(object sender, LiveSpentCallEventArgs e)
    {
        LiveSpentCall spentCall = e.SpentCall;
        if (spentCall.IsRemoveCall)
        {
            LiveSpentCall existingSpentCall = _spentCalls
                .FirstOrDefault(x => x.Winner == spentCall.Winner && x.ItemName == spentCall.ItemName && x.DkpSpent == spentCall.DkpSpent);
            if (existingSpentCall != null)
            {
                _spentCalls.Remove(existingSpentCall);
                Log.Info($"{LogPrefix} REMOVE applied to spent call {existingSpentCall}");
                Updated = true;
                return;
            }

            Log.Info($"{LogPrefix} REMOVE call made, but no associated SPENT call found: {spentCall}");
            return;
        }

        LiveAuctionInfo existingAuction = _activeAuctions.FirstOrDefault(x => x.ItemName == spentCall.ItemName);
        if (existingAuction == null)
        {
            Log.Info($"{LogPrefix} SPENT call made, but no associated auction found: {spentCall}");
            return;
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
            _eqLogTailFile.RemoveAuctionItem(existingAuction.ItemName);
            CompletedAuction newCompletedCall = new()
            {
                AuctionStart = existingAuction,
                ItemName = spentCall.ItemName,
                SpentCalls = spentCalls
            };
            Log.Debug($"{LogPrefix} SPENT call made, Completed call created: {newCompletedCall}");
            _completedAuctions = _completedAuctions.Add(newCompletedCall);
            Updated = true;
            return;
        }

        Log.Debug($"{LogPrefix} SPENT call made, but not enough SPENT calls to complete the auction. SPENT call: {spentCall}; Associated Auction: {existingAuction}");
        Updated = true;
        return;
    }

    private void ListenForUpdates()
    {
        if (_listeningForEvents)
            return;

        _listeningForEvents = true;
        _eqLogTailFile.AuctionStartMessage += HandleAuctionStart;
        _eqLogTailFile.BidInfoMessage += HandleBid;
        _eqLogTailFile.RollMessage += HandleRoll;
        _eqLogTailFile.AfkCommandMessage += HandleAfkCommand;
        _eqLogTailFile.MezBreakMessage += HandleMezBreak;
        _eqLogTailFile.SpentCallMessage += HandleSpentCall;
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

    IEnumerable<string> CurrentAfks { get; }

    bool IsParsingLogFile { get; }

    IEnumerable<MezBreak> MezBreaks { get; }

    bool ReadyCheckInitiated { get; }

    bool TrackBossKills { get; set; }

    bool TrackReadyCheck { get; set; }

    bool Updated { get; set; }

    bool CheckIfSnipe(SuggestedSpentCall spentCall);

    string GetBossKilledName();

    ICollection<LiveBidInfo> GetHighBids(LiveAuctionInfo auction, bool lowRollWins);

    string GetNextStatusMarkerForSelection(string currentMarker);

    string GetRemoveSpentMessageWithLink(SuggestedSpentCall spentCall);

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

    bool TryGetReadyCheckStatus(out CharacterReadyCheckStatus readyStatus);
}
