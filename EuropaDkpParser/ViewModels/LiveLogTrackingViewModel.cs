// -----------------------------------------------------------------------
// LiveLogTrackingViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
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
    private readonly IReadyCheckOverlayViewModel _readyCheckOverlayViewModel;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    private readonly DispatcherTimer _updateTimer;
    private readonly IZealMessageProvider _zealMessages;
    private ICollection<LiveAuctionDisplay> _activeAuctions;
    private string _auctionStatusMessageToPaste;
    private ICollection<CompletedAuction> _completedAuctions;
    private ICollection<LiveBidInfo> _currentBids;
    private string _currentCharacterName = string.Empty;
    private string _currentStatusMarker;
    private bool _enableReadyCheck;
    private string _filePath;
    private bool _forceShowOverlay;
    private ICollection<LiveBidInfo> _highBids;
    private string _itemLinkIdToAdd;
    private bool _lowRollWins;
    private DateTime _nextForcedUpdate = DateTime.MinValue;
    private bool _remindAttendances;
    private LiveAuctionDisplay _selectedActiveAuction;
    private LiveBidInfo _selectedBid;
    private string _selectedBidCharacterName;
    private CompletedAuction _selectedCompletedAuction;
    private SuggestedSpentCall _selectedSpentMessageToPaste;
    private ICollection<SuggestedSpentCall> _spentMessagesToPaste;
    private bool _useAudioReminder;
    private bool _useOverlayForAttendanceReminder;

    public LiveLogTrackingViewModel(
        IWindowViewFactory windowViewFactory,
        IDkpParserSettings settings,
        IDialogFactory dialogFactory,
        IOverlayFactory overlayFactory,
        IWindowFactory windowFactory)
        : base(windowViewFactory)
    {
        _settings = settings;

        _dkpDataRetriever = new DkpDataRetriever(settings);
        _activeBidTracker = new(settings, new TailFile());
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);
        _attendanceTimerHandler = new AttendanceTimerHandler(settings, overlayFactory, dialogFactory);

        _zealMessages = ZealAttendanceMessageProvider.Instance;
        _zealMessages.PipeError += HandleZealPipeError;

        _readyCheckOverlayViewModel = overlayFactory.CreateReadyCheckOverlayViewModel(_settings);

        CopySelectedSpentCallToClipboardCommand = new DelegateCommand(CopySelectedSpentCallToClipboard, () => SelectedSpentMessageToPaste != null)
            .ObservesProperty(() => SelectedSpentMessageToPaste);
        CopySelectedStatusMessageToClipboardCommand = new DelegateCommand(CopySelectedStatusMessageToClipboard, () => AuctionStatusMessageToPaste != null)
            .ObservesProperty(() => AuctionStatusMessageToPaste);
        CopyRemoveSpentMessageToClipboardCommand = new DelegateCommand(CopyRemoveSelectedSpentCallToClipboard, () => SelectedSpentMessageToPaste != null)
            .ObservesProperty(() => SelectedSpentMessageToPaste);
        ReactivateCompletedAuctionCommand = new DelegateCommand(ReactivateCompletedAuction, () => SelectedCompletedAuction != null)
            .ObservesProperty(() => SelectedCompletedAuction);
        RemoveBidCommand = new DelegateCommand(RemoveBid, () => SelectedBid != null).ObservesProperty(() => SelectedBid);
        SelectFileToTailCommand = new DelegateCommand(SelectFileToTail);
        SetActiveAuctionToCompletedCommand = new DelegateCommand(SetActiveAuctionToCompleted, () => SelectedActiveAuction != null)
            .ObservesProperty(() => SelectedActiveAuction);
        CycleToNextStatusMarkerCommand = new DelegateCommand(CycleToNextStatusMarker);
        AddItemLinkIdCommand = new DelegateCommand(AddItemLinkId, () => SelectedActiveAuction != null && !string.IsNullOrWhiteSpace(ItemLinkIdToAdd))
            .ObservesProperty(() => SelectedActiveAuction).ObservesProperty(() => ItemLinkIdToAdd);
        GetUserDkpCommand = new DelegateCommand(GetUserDkp, () => SelectedBid != null && !string.IsNullOrWhiteSpace(_settings.ApiReadToken))
            .ObservesProperty(() => SelectedBid);
        ChangeBidCharacterNameCommand = new DelegateCommand(ChangeBidCharacterName, () => SelectedBid != null && !string.IsNullOrWhiteSpace(SelectedBidCharacterName))
            .ObservesProperty(() => SelectedBid).ObservesProperty(() => SelectedBidCharacterName);
        SpawnTimeAttendanceCall = new DelegateCommand(SpawnTimeAttendanceCallNow, () => RemindAttendances).ObservesProperty(() => RemindAttendances);
        StartReadyCheckCommand = new DelegateCommand(StartReadyCheck, () => EnableReadyCheck).ObservesProperty(() => EnableReadyCheck);

        CurrentStatusMarker = _activeBidTracker.GetNextStatusMarkerForSelection("");

        LogFileNames = [.. _settings.SelectedLogFiles];
    }

    public ICollection<LiveAuctionDisplay> ActiveAuctions
    {
        get => _activeAuctions;
        private set => SetProperty(ref _activeAuctions, value);
    }

    public DelegateCommand AddItemLinkIdCommand { get; }

    public string AuctionStatusMessageToPaste
    {
        get => _auctionStatusMessageToPaste;
        set => SetProperty(ref _auctionStatusMessageToPaste, value);
    }

    public DelegateCommand ChangeBidCharacterNameCommand { get; }

    public ICollection<CompletedAuction> CompletedAuctions
    {
        get => _completedAuctions;
        private set => SetProperty(ref _completedAuctions, value);
    }

    public DelegateCommand CopyRemoveSpentMessageToClipboardCommand { get; }

    public DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    public DelegateCommand CopySelectedStatusMessageToClipboardCommand { get; }

    public ICollection<LiveBidInfo> CurrentBids
    {
        get => _currentBids;
        private set => SetProperty(ref _currentBids, value);
    }

    public string CurrentStatusMarker
    {
        get => _currentStatusMarker;
        set => SetProperty(ref _currentStatusMarker, value);
    }

    public DelegateCommand CycleToNextStatusMarkerCommand { get; }

    public bool EnableReadyCheck
    {
        get => _enableReadyCheck;
        set
        {
            if (SetProperty(ref _enableReadyCheck, value))
            {
                Log.Debug($"{LogPrefix} {nameof(EnableReadyCheck)} set to {EnableReadyCheck}");
                _activeBidTracker.TrackReadyCheck = value;
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                string characterName = ExtractCharacterNameFromLogFile(value);
                Log.Info($"{LogPrefix} {nameof(FilePath)} being set to {value}, characterName is {characterName}.");
                StartTailingFile(value);

                if (!string.IsNullOrEmpty(characterName))
                {
                    _currentCharacterName = characterName;
                    _zealMessages.StartMessageProcessing(characterName);
                }
            }
        }
    }

    public bool ForceShowOverlay
    {
        get => _forceShowOverlay;
        set
        {
            if (SetProperty(ref _forceShowOverlay, value))
            {
                if (!_attendanceTimerHandler.TogglePositioningOverlay(value))
                {
                    _forceShowOverlay = false;
                    RaisePropertyChanged(nameof(ForceShowOverlay));
                }
            }
        }
    }

    public DelegateCommand GetUserDkpCommand { get; }

    public ICollection<LiveBidInfo> HighBids
    {
        get => _highBids;
        private set => SetProperty(ref _highBids, value);
    }

    public string ItemLinkIdToAdd
    {
        get => _itemLinkIdToAdd;
        set => SetProperty(ref _itemLinkIdToAdd, value);
    }

    public ICollection<string> LogFileNames { get; }

    public bool LowRollWins
    {
        get => _lowRollWins;
        set => SetProperty(ref _lowRollWins, value);
    }

    public DelegateCommand ReactivateCompletedAuctionCommand { get; }

    public bool RemindAttendances
    {
        get => _remindAttendances;
        set
        {
            if (SetProperty(ref _remindAttendances, value))
            {
                _attendanceTimerHandler.RemindAttendances = value;
                Log.Debug($"{LogPrefix} {nameof(RemindAttendances)} set to {value}.");
            }
        }
    }

    public DelegateCommand RemoveBidCommand { get; }

    public LiveAuctionDisplay SelectedActiveAuction
    {
        get => _selectedActiveAuction;
        set
        {
            SetProperty(ref _selectedActiveAuction, value);
            UpdateActiveAuctionSelected();
        }
    }

    public ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; } = [];

    public LiveBidInfo SelectedBid
    {
        get => _selectedBid;
        set
        {
            SetProperty(ref _selectedBid, value);
            SelectedBidCharacterName = value?.CharacterBeingBidFor ?? string.Empty;
        }
    }

    public string SelectedBidCharacterName
    {
        get => _selectedBidCharacterName;
        set => SetProperty(ref _selectedBidCharacterName, value);
    }

    public CompletedAuction SelectedCompletedAuction
    {
        get => _selectedCompletedAuction;
        set => SetProperty(ref _selectedCompletedAuction, value);
    }

    public SuggestedSpentCall SelectedSpentMessageToPaste
    {
        get => _selectedSpentMessageToPaste;
        set => SetProperty(ref _selectedSpentMessageToPaste, value);
    }

    public DelegateCommand SelectFileToTailCommand { get; }

    public DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    public DelegateCommand SpawnTimeAttendanceCall { get; }

    public ICollection<SuggestedSpentCall> SpentMessagesToPaste
    {
        get => _spentMessagesToPaste;
        private set => SetProperty(ref _spentMessagesToPaste, value);
    }

    public DelegateCommand StartReadyCheckCommand { get; }

    public bool UseAudioReminder
    {
        get => _useAudioReminder;
        set
        {
            if (SetProperty(ref _useAudioReminder, value))
                _attendanceTimerHandler.UseAudioReminder = value;
        }
    }

    public bool UseOverlayForAttendanceReminder
    {
        get => _useOverlayForAttendanceReminder;
        set
        {
            if (SetProperty(ref _useOverlayForAttendanceReminder, value))
            {
                Log.Debug($"{LogPrefix} {nameof(UseOverlayForAttendanceReminder)} being set to {value}.");

                _attendanceTimerHandler.UseOverlayForAttendanceReminder = value;

                if (!value)
                    _attendanceTimerHandler.CloseOverlays();
            }
        }
    }

    protected override sealed IWindowView CreateWindowView(IWindowViewFactory viewFactory)
        => viewFactory.CreateLiveLogTrackingWindow(this);

    protected override sealed void HandleClosing()
    {
        _attendanceTimerHandler.CloseAll();
        _updateTimer.Stop();
        _activeBidTracker.StopTracking();
        _zealMessages.StopMessageProcessing();
        _readyCheckOverlayViewModel?.Close();
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
    }

    private void CopySelectedStatusMessageToClipboard()
    {
        string selectedStatsuMessage = AuctionStatusMessageToPaste;
        if (selectedStatsuMessage == null)
            return;

        Clip.Copy(selectedStatsuMessage);
    }

    private void CycleToNextStatusMarker()
    {
        CurrentStatusMarker = _activeBidTracker.GetNextStatusMarkerForSelection(CurrentStatusMarker);
        SetAuctionStatusMessage();
    }

    private string ExtractCharacterNameFromLogFile(string fullLogFilePath)
    {
        int lastIndexOfSlash = fullLogFilePath.LastIndexOf('\\');
        if (lastIndexOfSlash < 1)
            return string.Empty;

        string fileName = fullLogFilePath[(lastIndexOfSlash + 1)..];
        string[] fileNameParts = fileName.Split('_');
        if (fileNameParts.Length < 2)
            return string.Empty;

        return fileNameParts[1];
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

    private void SelectFileToTail()
    {
        // Switched to using configured files. Leaving this here for now in case I change my mind.
        //OpenFileDialog fileDialog = new()
        //{
        //    Title = "Select Log File to Monitor",
        //    InitialDirectory = _settings.EqDirectory,
        //    DefaultDirectory = _settings.EqDirectory
        //};

        //if (fileDialog.ShowDialog() != true)
        //    return;

        //string logFile = fileDialog.FileName;
        //FilePath = logFile;
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

    private void SpawnTimeAttendanceCallNow()
        => _attendanceTimerHandler.ShowTimeAttendanceReminder();

    private void StartReadyCheck()
    {
        Log.Debug($"{LogPrefix} {nameof(StartReadyCheck)} called with {nameof(EnableReadyCheck)} set to {EnableReadyCheck}.");
        if (!EnableReadyCheck)
            return;

        if (_zealMessages.RaidInfo.RaidAttendees.Count == 0)
        {
            Log.Debug($"{LogPrefix} No raid attendees found from Zeal.  Ending.");
            return;
        }

        Clip.Copy($"/rs {Constants.ReadyCheckWithDelimiter}");

        if (!_readyCheckOverlayViewModel.ContentIsVisible)
        {
            IEnumerable<string> charactersInRaid = _zealMessages.RaidInfo.RaidAttendees
                .Where(x => x.Name != _currentCharacterName)
                .Select(x => x.Name);

            _readyCheckOverlayViewModel.SetInitialCharacterList(charactersInRaid);
        }

        Log.Debug($"{LogPrefix} Showing ReadyCheck overlay.");

        _readyCheckOverlayViewModel.Show();
    }

    private void StartTailingFile(string fileToTail)
    {
        if (string.IsNullOrWhiteSpace(fileToTail))
            return;

        if (!File.Exists(fileToTail))
            return;

        _activeBidTracker.StopTracking();
        _activeBidTracker.StartTracking(fileToTail);
    }

    private void UpdateActiveAuctionSelected()
        => UpdateActiveAuctionSelected(SelectedBid, SelectedSpentMessageToPaste);

    private void UpdateActiveAuctionSelected(LiveBidInfo selectedCurrentBid, SuggestedSpentCall selectedSpent)
    {
        if (SelectedActiveAuction != null)
        {
            SelectedActiveAuction.HasNewBidsAdded = false;

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
            LiveBidInfo matchingBid = CurrentBids.FirstOrDefault(x => x.ParentAuctionId == selectedCurrentBid.ParentAuctionId
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
        CompletedAuctions = new List<CompletedAuction>(_activeBidTracker.CompletedAuctions.OrderByDescending(GetSortingTimestamp));
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

public interface ILiveLogTrackingViewModel : IWindowViewModel
{
    ICollection<LiveAuctionDisplay> ActiveAuctions { get; }

    DelegateCommand AddItemLinkIdCommand { get; }

    string AuctionStatusMessageToPaste { get; set; }

    DelegateCommand ChangeBidCharacterNameCommand { get; }

    ICollection<CompletedAuction> CompletedAuctions { get; }

    DelegateCommand CopyRemoveSpentMessageToClipboardCommand { get; }

    DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    DelegateCommand CopySelectedStatusMessageToClipboardCommand { get; }

    ICollection<LiveBidInfo> CurrentBids { get; }

    string CurrentStatusMarker { get; }

    DelegateCommand CycleToNextStatusMarkerCommand { get; }

    bool EnableReadyCheck { get; set; }

    string FilePath { get; set; }

    bool ForceShowOverlay { get; set; }

    DelegateCommand GetUserDkpCommand { get; }

    ICollection<LiveBidInfo> HighBids { get; }

    string ItemLinkIdToAdd { get; set; }

    ICollection<string> LogFileNames { get; }

    bool LowRollWins { get; set; }

    DelegateCommand ReactivateCompletedAuctionCommand { get; }

    bool RemindAttendances { get; set; }

    DelegateCommand RemoveBidCommand { get; }

    LiveAuctionDisplay SelectedActiveAuction { get; set; }

    ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; }

    LiveBidInfo SelectedBid { get; set; }

    string SelectedBidCharacterName { get; set; }

    CompletedAuction SelectedCompletedAuction { get; set; }

    SuggestedSpentCall SelectedSpentMessageToPaste { get; set; }

    DelegateCommand SelectFileToTailCommand { get; }

    DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    DelegateCommand SpawnTimeAttendanceCall { get; }

    ICollection<SuggestedSpentCall> SpentMessagesToPaste { get; }

    DelegateCommand StartReadyCheckCommand { get; }

    bool UseAudioReminder { get; set; }

    bool UseOverlayForAttendanceReminder { get; set; }
}
