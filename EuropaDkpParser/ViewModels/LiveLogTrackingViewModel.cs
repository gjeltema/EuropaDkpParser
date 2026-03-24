// -----------------------------------------------------------------------
// LiveLogTrackingViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows.Threading;
using DkpParser;
using DkpParser.LiveTracking;
using DkpParser.Zeal;
using EuropaDkpParser.Utility;
using Gjeltema.Logging;
using Prism.Commands;

internal sealed class LiveLogTrackingViewModel : WindowViewModelBase, ILiveLogTrackingViewModel
{
    private const int DkpDisplayFontSize = 16;
    private const string LogPrefix = $"[{nameof(LiveLogTrackingViewModel)}]";
    private readonly ActiveBidTracker _activeBidTracker;
    private readonly AttendanceTimerHandler _attendanceTimerHandler;
    private readonly IDkpDataRetriever _dkpDataRetriever;
    private readonly IEqLogTailFile _eqLogTailFile;
    private readonly IOverlayFactory _overlayFactory;
    private readonly IReadyCheckOverlayViewModel _readyCheckOverlayViewModel;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    private readonly DispatcherTimer _updateTimer;
    private readonly IZealMessageProvider _zealMessages;
    private IAuctioneerOverlayViewModel _auctioneerOverlay;
    private DateTime _nextForcedUpdate = DateTime.MinValue;
    private ISpellTrackerOverlayViewModel _spellTracker;

    public LiveLogTrackingViewModel(
        IWindowViewFactory windowViewFactory,
        IDkpParserSettings settings,
        IEqLogTailFile eqLogTailFile,
        IDialogFactory dialogFactory,
        IOverlayFactory overlayFactory,
        IWindowFactory windowFactory)
        : base(windowViewFactory)
    {
        _settings = settings;
        _overlayFactory = overlayFactory;
        _eqLogTailFile = eqLogTailFile;

        _dkpDataRetriever = new DkpDataRetriever(settings);
        _activeBidTracker = new(settings, eqLogTailFile);
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);
        _attendanceTimerHandler = new AttendanceTimerHandler(settings, this, overlayFactory, dialogFactory);

        _zealMessages = ZealAttendanceMessageProvider.Instance;
        _zealMessages.PipeError += HandleZealPipeError;

        _readyCheckOverlayViewModel = overlayFactory.CreateReadyCheckOverlayViewModel(_settings);

        CopySelectedSpentCallToClipboardCommand = new DelegateCommand(CopySelectedSpentCallToClipboard, () => SelectedSpentMessageToPaste != null)
            .ObservesProperty(() => SelectedSpentMessageToPaste);
        CopySelectedStatusMessageToClipboardCommand = new DelegateCommand(CopySelectedStatusMessageToClipboard, () => !string.IsNullOrWhiteSpace(AuctionStatusMessageToPaste))
            .ObservesProperty(() => AuctionStatusMessageToPaste);
        CopyRemoveSpentMessageToClipboardCommand = new DelegateCommand(CopyRemoveSelectedSpentCallToClipboard, () => SelectedSpentMessageToPaste != null)
            .ObservesProperty(() => SelectedSpentMessageToPaste);
        ReactivateCompletedAuctionCommand = new DelegateCommand(ReactivateCompletedAuction, () => SelectedCompletedAuction != null)
            .ObservesProperty(() => SelectedCompletedAuction);
        RemoveBidCommand = new DelegateCommand(RemoveBid, () => SelectedBid != null).ObservesProperty(() => SelectedBid);
        SetActiveAuctionToCompletedCommand = new DelegateCommand(SetActiveAuctionToCompleted, () => SelectedActiveAuction != null)
            .ObservesProperty(() => SelectedActiveAuction);
        CycleToNextStatusMarkerCommand = new DelegateCommand(CycleToNextStatusMarker);
        AddItemLinkIdCommand = new DelegateCommand(AddItemLinkId, () => SelectedActiveAuction != null && !string.IsNullOrWhiteSpace(ItemLinkIdToAdd))
            .ObservesProperty(() => SelectedActiveAuction).ObservesProperty(() => ItemLinkIdToAdd);
        GetUserDkpCommand = new DelegateCommand(GetUserDkp, () => SelectedBid != null && !string.IsNullOrWhiteSpace(_settings.ApiReadToken))
            .ObservesProperty(() => SelectedBid);
        ChangeBidCharacterNameCommand = new DelegateCommand(ChangeBidCharacterName, () => SelectedBid != null && !string.IsNullOrWhiteSpace(SelectedBidCharacterName))
            .ObservesProperty(() => SelectedBid).ObservesProperty(() => SelectedBidCharacterName);
        SpawnAttendanceCall = new DelegateCommand(SpawnAttendanceCallNow, () => RemindAttendances).ObservesProperty(() => RemindAttendances);
        StartReadyCheckCommand = new DelegateCommand(StartReadyCheck, () => EnableReadyCheck).ObservesProperty(() => EnableReadyCheck);

        CurrentStatusMarker = _activeBidTracker.GetNextStatusMarkerForSelection("");

        LogFileNames = [.. _settings.SelectedLogFiles];

        AttendanceNowTimeCall = true;

        TrackSpellsConfigured = _settings.SpellTrackers.Count > 0;

        _activeBidTracker.StartTracking();
    }

    public ICollection<LiveAuctionDisplay> ActiveAuctions { get; private set => SetProperty(ref field, value); }

    public DelegateCommand AddItemLinkIdCommand { get; }

    public string AttendanceNowBossName { get; set => SetProperty(ref field, value); }

    public bool AttendanceNowKillCall { get; set => SetProperty(ref field, value); }

    public bool AttendanceNowTimeCall { get; set => SetProperty(ref field, value); }

    public string AuctionStatusMessageToPaste { get; set => SetProperty(ref field, value); }

    public DelegateCommand ChangeBidCharacterNameCommand { get; }

    public ICollection<CompletedAuction> CompletedAuctions { get; private set => SetProperty(ref field, value); }

    public DelegateCommand CopyRemoveSpentMessageToClipboardCommand { get; }

    public DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    public DelegateCommand CopySelectedStatusMessageToClipboardCommand { get; }

    public ICollection<string> CurrentAfks { get; private set => SetProperty(ref field, value); }

    public ICollection<LiveBidInfo> CurrentBids { get; private set => SetProperty(ref field, value); }

    public string CurrentStatusMarker { get; set => SetProperty(ref field, value); }

    public DelegateCommand CycleToNextStatusMarkerCommand { get; }

    public bool EnableAuctionOverlayMove
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (field)
                    _auctioneerOverlay.EnableMove();
                else
                    _auctioneerOverlay.DisableMove();
            }
        }
    }

    public bool EnableReadyCheck
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Log.Debug($"{LogPrefix} {nameof(EnableReadyCheck)} set to {EnableReadyCheck}");
                _activeBidTracker.TrackReadyCheck = value;
            }
        }
    }

    public string FilePath
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Log.Info($"{LogPrefix} {nameof(FilePath)} being set to {value}.");
            }

            if (!IsReadingLogFile && !string.IsNullOrWhiteSpace(field))
                _activeBidTracker.StartTracking(value);
        }
    }

    public bool ForceShowOverlay
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (!_attendanceTimerHandler.TogglePositioningOverlay(value))
                {
                    field = false;
                    RaisePropertyChanged(nameof(ForceShowOverlay));
                }
            }
        }
    }

    public DelegateCommand GetUserDkpCommand { get; }

    public ICollection<LiveBidInfo> HighBids { get; private set => SetProperty(ref field, value); }

    public bool IsReadingLogFile { get; private set => SetProperty(ref field, value); }

    public bool IsReadyToTakeZealAttendance { get; set => SetProperty(ref field, value); }

    public bool IsZealConnected { get; set => SetProperty(ref field, value); }

    public string ItemLinkIdToAdd { get; set => SetProperty(ref field, value); }

    public ICollection<string> LogFileNames { get; }

    public bool LowRollWins { get; set => SetProperty(ref field, value); }

    public ICollection<MezBreak> MezBreaks { get; set => SetProperty(ref field, value); }

    public DelegateCommand ReactivateCompletedAuctionCommand { get; }

    public bool RemindAttendances
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _attendanceTimerHandler.RemindAttendances = value;
                _activeBidTracker.TrackBossKills = value;
                Log.Debug($"{LogPrefix} {nameof(RemindAttendances)} set to {value}.");
            }
        }
    }

    public DelegateCommand RemoveBidCommand { get; }

    public LiveAuctionDisplay SelectedActiveAuction
    {
        get;
        set
        {
            SetProperty(ref field, value);
            UpdateActiveAuctionSelected();
        }
    }

    public ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; } = [];

    public LiveBidInfo SelectedBid
    {
        get;
        set
        {
            SetProperty(ref field, value);
            SelectedBidCharacterName = value?.CharacterBeingBidFor ?? string.Empty;
        }
    }

    public string SelectedBidCharacterName { get; set => SetProperty(ref field, value); }

    public CompletedAuction SelectedCompletedAuction { get; set => SetProperty(ref field, value); }

    public SuggestedSpentCall SelectedSpentMessageToPaste { get; set => SetProperty(ref field, value); }

    public DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    public bool ShowAuctionOverlay
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                DisplayAuctionOverlay(field);
        }
    }

    public DelegateCommand SpawnAttendanceCall { get; }

    public ICollection<SuggestedSpentCall> SpentMessagesToPaste { get; private set => SetProperty(ref field, value); }

    public DelegateCommand StartReadyCheckCommand { get; }

    public bool TrackSpells
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (field)
                ShowSpellTracker();
            else
                CloseSpellTracker();
        }
    }

    public bool TrackSpellsConfigured { get; set; }

    public bool UseAudioReminder
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                _attendanceTimerHandler.UseAudioReminder = value;
        }
    }

    public bool UseOverlayForAttendanceReminder
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Log.Debug($"{LogPrefix} {nameof(UseOverlayForAttendanceReminder)} being set to {value}.");

                _attendanceTimerHandler.UseOverlayForAttendanceReminder = value;

                if (!value)
                    _attendanceTimerHandler.CloseOverlays();
            }
        }
    }

    public void TakeAttendanceSnapshot(string raidName, AttendanceCallType callType)
    {
        try
        {
            _activeBidTracker.TakeAttendanceSnapshot(raidName, callType);
        }
        catch (InvalidZealAttendanceData)
        {
            MessageDialog.ShowDialog(
                $"Error when taking Zeal attendance.{Environment.NewLine}You should do a normal attendance call, and try to reconnect to Zeal (set log file to monitor).",
                "Zeal Attendance Error",
                170);
        }
    }

    protected override sealed IWindowView CreateWindowView(IWindowViewFactory viewFactory)
        => viewFactory.CreateLiveLogTrackingWindow(this);

    protected override sealed void HandleClosing()
    {
        _updateTimer.Stop();
        _activeBidTracker.StopTracking();
        _zealMessages.StopMessageProcessing();
        _attendanceTimerHandler.CloseAll();
        _readyCheckOverlayViewModel?.Close();
        _auctioneerOverlay?.Close();
        _spellTracker?.Close();
    }

    private void AddItemLinkId()
    {
        if (string.IsNullOrWhiteSpace(ItemLinkIdToAdd))
            return;

        if (SelectedActiveAuction == null)
            return;

        _settings.ItemLinkIds.AddAndSaveItemId(SelectedActiveAuction.ItemName, ItemLinkIdToAdd);
    }

    private void ChangeBidCharacterName()
    {
        if (SelectedBid == null)
            return;

        if (string.IsNullOrWhiteSpace(SelectedBidCharacterName))
            return;

        LiveBidInfo selectedBid = SelectedBid;
        string selectedBidCharacterName = SelectedBidCharacterName;

        SelectedBid = null;

        selectedBid.CharacterBeingBidFor = selectedBidCharacterName.NormalizeName();
        selectedBid.CharacterNotOnDkpServer = _settings.CharactersOnDkpServer.CharacterConfirmedNotOnDkpServer(selectedBid.CharacterBeingBidFor);

        UpdateBidsListing(selectedBid);
    }

    private void CheckAndUpdateDisplay()
    {
        if (!_activeBidTracker.Updated && DateTime.Now < _nextForcedUpdate)
            return;

        UpdateDisplay();
    }

    private void CloseSpellTracker()
    {
        _spellTracker?.Close();
        _spellTracker = null;
    }

    private void CopyRemoveSelectedSpentCallToClipboard()
    {
        SuggestedSpentCall selectedSpentCall = SelectedSpentMessageToPaste;
        if (selectedSpentCall == null)
            return;

        string spentCallWithLink = _activeBidTracker.GetRemoveSpentMessageWithLink(selectedSpentCall);
        Clip.Copy(spentCallWithLink);
    }

    private void CopySelectedSpentCallToClipboard()
    {
        SuggestedSpentCall selectedSpentCall = SelectedSpentMessageToPaste;
        if (selectedSpentCall == null)
            return;

        string spentCallWithLink = _activeBidTracker.GetSpentMessageWithLink(selectedSpentCall);
        Clip.Copy(spentCallWithLink);

        SelectedActiveAuction?.HasNewBidsAdded = false;
    }

    private void CopySelectedStatusMessageToClipboard()
    {
        string selectedStatsuMessage = AuctionStatusMessageToPaste;
        if (string.IsNullOrWhiteSpace(selectedStatsuMessage))
            return;

        Clip.Copy(selectedStatsuMessage);

        SelectedActiveAuction?.HasNewBidsAdded = false;
    }

    private void CycleToNextStatusMarker()
    {
        CurrentStatusMarker = _activeBidTracker.GetNextStatusMarkerForSelection(CurrentStatusMarker);
        SetAuctionStatusMessage();
    }

    private void DisplayAuctionOverlay(bool showOverlay)
    {
        _auctioneerOverlay?.Close();
        if (showOverlay)
        {
            _auctioneerOverlay = _overlayFactory.CreateAuctioneerOverlayViewModel(_settings, _eqLogTailFile);
            _auctioneerOverlay.CreateAndShowOverlay();
        }
        else
        {
            _auctioneerOverlay = null;
        }
    }

    private DateTime GetSortingTimestamp(CompletedAuction completed)
    {
        if (completed.SpentCalls.Count > 0)
            return completed.SpentCalls.Max(x => x.Timestamp);
        else
            return completed.AuctionStart.Timestamp;
    }

    private async void GetUserDkp()
        => await GetUserDkpAsync();

    private async Task GetUserDkpAsync()
    {
        if (SelectedBid == null)
            return;

        string characterName = SelectedBid.CharacterBeingBidFor;
        if (string.IsNullOrWhiteSpace(characterName))
        {
            MessageDialog.ShowDialog($"Character name is invalid.", "Unable To Retrieve DKP");
            return;
        }

        DkpUserCharacter dkpCharacter = _settings.CharactersOnDkpServer.GetUserCharacter(characterName);
        int userDkp = dkpCharacter == null
            ? await _dkpDataRetriever.GetUserDkp(characterName)
            : await _dkpDataRetriever.GetUserDkp(dkpCharacter);

        if (userDkp < -100000)
        {
            MessageDialog.ShowDialog($"Unable to retrieve DKP for {characterName}, likely does not exist on server.", "Unable To Retrieve DKP");
        }
        else
        {
            MessageDialog.ShowDialog($"{characterName} has {userDkp} DKP", "DKP Amount", fontSize: DkpDisplayFontSize);
        }
    }

    private void HandleUpdate(object sender, EventArgs e)
    {
        CheckAndUpdateDisplay();
    }

    private void HandleZealPipeError(object sender, ZealPipeErrorEventArgs e)
        => MessageDialog.ShowDialog(e.ErrorMessage, "Zeal Pipe Error");

    private void ReactivateCompletedAuction()
    {
        CompletedAuction selectedAuction = SelectedCompletedAuction;
        if (selectedAuction == null)
            return;

        SelectedCompletedAuction = null;
        _activeBidTracker.ReactivateCompletedAuction(selectedAuction);
    }

    private void RemoveBid()
    {
        LiveBidInfo selectedBid = SelectedBid;
        if (selectedBid == null)
            return;

        SelectedBid = null;

        _activeBidTracker.RemoveBid(selectedBid);
        UpdateActiveAuctionSelected();
    }

    private void SetActiveAuctionToCompleted()
    {
        if (SelectedActiveAuctions.Count == 0)
            return;

        ICollection<LiveAuctionDisplay> selectedAuctions = [.. SelectedActiveAuctions];
        SelectedActiveAuction = null;

        foreach (LiveAuctionDisplay selectedAuction in selectedAuctions)
        {
            _activeBidTracker.SetAuctionToCompleted(selectedAuction.Auction);
        }
    }

    private void SetAuctionStatusMessage()
    {
        StatusMarker marker = _activeBidTracker.GetStatusMarkerFromSelectionString(CurrentStatusMarker);
        AuctionStatusMessageToPaste = _activeBidTracker.GetStatusMessage(SelectedActiveAuction?.Auction, marker, LowRollWins);
    }

    private void ShowSpellTracker()
    {
        _spellTracker = _overlayFactory.CreateSpellTrackerkOverlayViewModel(_settings);
        _spellTracker.CreateAndShowOverlay();
    }

    private void SpawnAttendanceCallNow()
    {
        if (AttendanceNowTimeCall)
            _attendanceTimerHandler.ShowTimeAttendanceReminder();
        else
            _attendanceTimerHandler.RemindForKillAttendance(AttendanceNowBossName);
    }

    private void StartReadyCheck()
    {
        Log.Debug($"{LogPrefix} {nameof(StartReadyCheck)} called with {nameof(EnableReadyCheck)} set to {EnableReadyCheck}.");
        if (!EnableReadyCheck)
            return;

        if (_zealMessages.RaidInfo.RaidAttendees.Count == 0)
        {
            Log.Info($"{LogPrefix} No raid attendees found from Zeal.  Ending ready check.");
            return;
        }

        Clip.Copy($"/rs {Constants.ReadyCheckWithDelimiter} {Constants.ReadyCheckRespondMessage}");

        if (!_readyCheckOverlayViewModel.ContentIsVisible)
        {
            IEnumerable<string> charactersInRaid = _zealMessages.RaidInfo.RaidAttendees
                .Where(x => x.Name != _zealMessages?.CharacterInfo?.CharacterName)
                .Select(x => x.Name);

            _readyCheckOverlayViewModel.SetInitialCharacterList(charactersInRaid);
        }

        Log.Debug($"{LogPrefix} Showing ReadyCheck overlay.");

        _readyCheckOverlayViewModel.Show();
    }

    private void UpdateActiveAuctionSelected()
        => UpdateActiveAuctionSelected(SelectedBid, SelectedSpentMessageToPaste);

    private void UpdateActiveAuctionSelected(LiveBidInfo selectedCurrentBid, SuggestedSpentCall selectedSpent)
    {
        if (SelectedActiveAuction != null)
        {
            UpdateBidsListing(selectedCurrentBid);

            HighBids = new List<LiveBidInfo>(_activeBidTracker.GetHighBids(SelectedActiveAuction.Auction, LowRollWins));

            SpentMessagesToPaste = _activeBidTracker.GetSpentInfoForCurrentHighBids(SelectedActiveAuction.Auction, LowRollWins);
            if (selectedSpent != null)
            {
                SuggestedSpentCall matchingSpent = SpentMessagesToPaste.FirstOrDefault(x => x.Winner == selectedSpent.Winner
                    && x.ItemName == selectedSpent.ItemName
                    && x.DkpSpent == selectedSpent.DkpSpent);
                SelectedSpentMessageToPaste = matchingSpent;
            }
        }
        else
        {
            CurrentBids = [];
            HighBids = [];
            SpentMessagesToPaste = [];
        }

        SetAuctionStatusMessage();
    }

    private void UpdateBidsListing(LiveBidInfo selectedCurrentBid)
    {
        CurrentBids = new List<LiveBidInfo>(_activeBidTracker.Bids
            .Where(x => x.ParentAuctionId == SelectedActiveAuction.Id)
            .OrderByDescending(x => x.Timestamp));

        if (selectedCurrentBid != null)
        {
            LiveBidInfo matchingBid = CurrentBids.FirstOrDefault(x =>
                x.ParentAuctionId == selectedCurrentBid.ParentAuctionId
                && x.ItemName == selectedCurrentBid.ItemName
                && x.BidAmount == selectedCurrentBid.BidAmount
                && x.CharacterBeingBidFor == selectedCurrentBid.CharacterBeingBidFor);
            SelectedBid = matchingBid;
        }
    }

    private void UpdateDisplay()
    {
        _activeBidTracker.Updated = false;

        LiveBidInfo selectedBid = SelectedBid;
        SuggestedSpentCall selectedSpent = SelectedSpentMessageToPaste;
        LiveAuctionDisplay selectedAuction = SelectedActiveAuction;

        ActiveAuctions = _activeBidTracker.ActiveAuctions
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new LiveAuctionDisplay(x))
            .ToList();

        if (selectedAuction != null)
        {
            LiveAuctionDisplay matchingAuction = ActiveAuctions.FirstOrDefault(x => x.Id == selectedAuction.Id);
            SelectedActiveAuction = matchingAuction;
        }

        UpdateActiveAuctionSelected(selectedBid, selectedSpent);

        CompletedAuction selectedCompleted = SelectedCompletedAuction;
        CompletedAuctions = [.. _activeBidTracker.CompletedAuctions.OrderByDescending(GetSortingTimestamp)];
        if (selectedCompleted != null)
        {
            CompletedAuction matchingCompleted = CompletedAuctions.FirstOrDefault(x => x.AuctionStart.Id == selectedCompleted.AuctionStart.Id);
            if (matchingCompleted != null)
                SelectedCompletedAuction = matchingCompleted;
        }

        _attendanceTimerHandler.RemindForKillAttendance(_activeBidTracker.GetBossKilledName());

        if (_activeBidTracker.ReadyCheckInitiated)
            StartReadyCheck();

        if (EnableReadyCheck)
        {
            while (_activeBidTracker.TryGetReadyCheckStatus(out CharacterReadyCheckStatus readyStatus))
            {
                _readyCheckOverlayViewModel.SetCharacterReadyStatus(readyStatus);
            }
        }

        IsZealConnected = _zealMessages.IsConnected && !_zealMessages.CharacterInfo.IsDataStale;

        IsReadyToTakeZealAttendance = _zealMessages.IsConnected && !_zealMessages.CharacterInfo.IsDataStale
            && !_zealMessages.RaidInfo.IsDataStale
            && _zealMessages.RaidInfo.RaidAttendees.Count > 2;

        IsReadingLogFile = _activeBidTracker.IsParsingLogFile;

        CurrentAfks = [.. _activeBidTracker.CurrentAfks.Order()];

        MezBreaks = [.. _activeBidTracker.MezBreaks.OrderByDescending(x => x.TimeOfBreak)];

        _nextForcedUpdate = DateTime.Now.AddSeconds(10);
    }
}

public sealed class LiveAuctionDisplay : EuropaViewModelBase
{
    private readonly LiveAuctionInfo _liveAuctionInfo;

    public LiveAuctionDisplay(LiveAuctionInfo liveAuctionInfo)
    {
        _liveAuctionInfo = liveAuctionInfo;
    }

    public LiveAuctionInfo Auction
        => _liveAuctionInfo;

    public string Auctioneer
        => _liveAuctionInfo.Auctioneer;

    public string FullInfo { get; init; }

    public bool HasNewBidsAdded
    {
        get => _liveAuctionInfo.HasNewBidsAdded;
        set
        {
            if (_liveAuctionInfo.HasNewBidsAdded != value)
            {
                _liveAuctionInfo.HasNewBidsAdded = value;
                RaisePropertyChanged(nameof(HasNewBidsAdded));
            }
        }
    }

    public int Id
        => _liveAuctionInfo.Id;

    public string ItemName
        => _liveAuctionInfo.ItemName;

    public override string ToString()
        => _liveAuctionInfo.ToString();
}

public interface ILiveLogTrackingViewModel : IAttendanceSnapshot, IWindowViewModel
{
    ICollection<LiveAuctionDisplay> ActiveAuctions { get; }

    DelegateCommand AddItemLinkIdCommand { get; }

    string AttendanceNowBossName { get; set; }

    bool AttendanceNowKillCall { get; set; }

    bool AttendanceNowTimeCall { get; set; }

    string AuctionStatusMessageToPaste { get; set; }

    DelegateCommand ChangeBidCharacterNameCommand { get; }

    ICollection<CompletedAuction> CompletedAuctions { get; }

    DelegateCommand CopyRemoveSpentMessageToClipboardCommand { get; }

    DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    DelegateCommand CopySelectedStatusMessageToClipboardCommand { get; }

    ICollection<string> CurrentAfks { get; }

    ICollection<LiveBidInfo> CurrentBids { get; }

    string CurrentStatusMarker { get; }

    DelegateCommand CycleToNextStatusMarkerCommand { get; }

    bool EnableAuctionOverlayMove { get; set; }

    bool EnableReadyCheck { get; set; }

    string FilePath { get; set; }

    bool ForceShowOverlay { get; set; }

    DelegateCommand GetUserDkpCommand { get; }

    ICollection<LiveBidInfo> HighBids { get; }

    bool IsReadingLogFile { get; }

    bool IsReadyToTakeZealAttendance { get; set; }

    bool IsZealConnected { get; set; }

    string ItemLinkIdToAdd { get; set; }

    ICollection<string> LogFileNames { get; }

    bool LowRollWins { get; set; }

    ICollection<MezBreak> MezBreaks { get; }

    DelegateCommand ReactivateCompletedAuctionCommand { get; }

    bool RemindAttendances { get; set; }

    DelegateCommand RemoveBidCommand { get; }

    LiveAuctionDisplay SelectedActiveAuction { get; set; }

    ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; }

    LiveBidInfo SelectedBid { get; set; }

    string SelectedBidCharacterName { get; set; }

    CompletedAuction SelectedCompletedAuction { get; set; }

    SuggestedSpentCall SelectedSpentMessageToPaste { get; set; }

    DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    bool ShowAuctionOverlay { get; set; }

    DelegateCommand SpawnAttendanceCall { get; }

    ICollection<SuggestedSpentCall> SpentMessagesToPaste { get; }

    DelegateCommand StartReadyCheckCommand { get; }

    bool TrackSpells { get; set; }

    bool TrackSpellsConfigured { get; set; }

    bool UseAudioReminder { get; set; }

    bool UseOverlayForAttendanceReminder { get; set; }
}

public interface IAttendanceSnapshot
{
    void TakeAttendanceSnapshot(string raidName, AttendanceCallType callType);
}
