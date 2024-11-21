// -----------------------------------------------------------------------
// LiveLogTrackingViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows.Threading;
using DkpParser;
using DkpParser.LiveTracking;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
using Microsoft.Win32;
using Prism.Commands;

internal sealed class LiveLogTrackingViewModel : EuropaViewModelBase, ILiveLogTrackingViewModel
{
    private readonly ActiveBidTracker _activeBidTracker;
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    private readonly DispatcherTimer _updateTimer;
    private ICollection<LiveAuctionDisplay> _activeAuctions;
    private DispatcherTimer _attendanceReminderTimer;
    private string _auctionStatusMessagesToPaste;
    private ICollection<CompletedAuction> _completedAuctions;
    private ICollection<LiveBidInfo> _currentBids;
    private string _currentStatusMarker;
    private string _filePath;
    private ICollection<LiveBidInfo> _highBids;
    private bool _incrementRaidNameOnNextReminder = true;
    private string _itemLinkIdToAdd;
    private DispatcherTimer _killCallReminderTimer;
    private DateTime _nextForcedUpdate = DateTime.MinValue;
    private bool _remindAttendances;
    private LiveAuctionDisplay _selectedActiveAuction;
    private LiveBidInfo _selectedBid;
    private CompletedAuction _selectedCompletedAuction;
    private SuggestedSpentCall _selectedSpentMessageToPaste;
    private ICollection<SuggestedSpentCall> _spentMessagesToPaste;

    public LiveLogTrackingViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;

        _activeBidTracker = new(settings, new TailFile());
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);

        CopySelectedSpentCallToClipboardCommand = new DelegateCommand(CopySelectedSpentCallToClipboard, () => SelectedSpentMessageToPaste != null)
            .ObservesProperty(() => SelectedSpentMessageToPaste);
        CopySelectedStatusMessageToClipboardCommand = new DelegateCommand(CopySelectedStatusMessageToClipboard, () => AuctionStatusMessageToPaste != null)
            .ObservesProperty(() => AuctionStatusMessageToPaste);
        ReactivateCompletedAuctionCommand = new DelegateCommand(ReactivateCompletedAuction, () => SelectedCompletedAuction != null)
            .ObservesProperty(() => SelectedCompletedAuction);
        RemoveBidCommand = new DelegateCommand(RemoveBid, () => SelectedBid != null).ObservesProperty(() => SelectedBid);
        SelectFileToTailCommand = new DelegateCommand(SelectFileToTail);
        SetActiveAuctionToCompletedCommand = new DelegateCommand(SetActiveAuctionToCompleted, () => SelectedActiveAuction != null)
            .ObservesProperty(() => SelectedActiveAuction);
        CycleToNextStatusMarkerCommand = new DelegateCommand(CycleToNextStatusMarker);
        AddItemLinkIdCommand = new DelegateCommand(AddItemLinkId, () => SelectedActiveAuction != null && !string.IsNullOrWhiteSpace(ItemLinkIdToAdd))
            .ObservesProperty(() => SelectedActiveAuction).ObservesProperty(() => ItemLinkIdToAdd);

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
        get => _auctionStatusMessagesToPaste;
        set => SetProperty(ref _auctionStatusMessagesToPaste, value);
    }

    public ICollection<CompletedAuction> CompletedAuctions
    {
        get => _completedAuctions;
        private set => SetProperty(ref _completedAuctions, value);
    }

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

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
                StartTailingFile(value);
        }
    }

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

    public DelegateCommand ReactivateCompletedAuctionCommand { get; }

    public bool RemindAttendances
    {
        get => _remindAttendances;
        set
        {
            SetProperty(ref _remindAttendances, value);
            SetReminderForAttendances();
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

    public LiveBidInfo SelectedBid
    {
        get => _selectedBid;
        set => SetProperty(ref _selectedBid, value);
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

    public ICollection<SuggestedSpentCall> SpentMessagesToPaste
    {
        get => _spentMessagesToPaste;
        private set => SetProperty(ref _spentMessagesToPaste, value);
    }

    public void Close()
    {
        _updateTimer.Stop();
        _activeBidTracker.StopTracking();
        _attendanceReminderTimer?.Stop();
    }

    private void AddItemLinkId()
    {
        if (string.IsNullOrWhiteSpace(ItemLinkIdToAdd))
            return;

        if (SelectedActiveAuction == null)
            return;

        _settings.ItemLinkIds.AddAndSaveItemId(SelectedActiveAuction.ItemName, ItemLinkIdToAdd);
    }

    private void CheckAndUpdateDisplay()
    {
        if (!_activeBidTracker.Updated && DateTime.Now < _nextForcedUpdate)
            return;

        UpdateDisplay();
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

    private TimeSpan GetAttendanceReminderInterval()
    {
        int minutes = DateTime.Now.Minute;
        if (minutes < 30)
            return TimeSpan.FromMinutes(30 - minutes);
        else
            return TimeSpan.FromMinutes(60 - minutes);
    }

    private IReminderDialogViewModel GetReminderDialog(string reminderDisplayText, AttendanceCallType callType)
    {
        IReminderDialogViewModel reminderDialogViewModel = _dialogFactory.CreateReminderDialogViewModel();
        reminderDialogViewModel.ReminderText = reminderDisplayText;
        reminderDialogViewModel.AttendanceType = callType;

        return reminderDialogViewModel;
    }

    private void HandleAttendanceReminderTimer(object sender, EventArgs e)
    {
        _attendanceReminderTimer.Stop();

        IReminderDialogViewModel reminder = GetReminderDialog(Strings.GetString("TimeAttendanceReminderMessage"), AttendanceCallType.Time);
        if (_incrementRaidNameOnNextReminder)
            reminder.IncrementToNextTimeCall();

        bool ok = reminder.ShowDialog() == true;
        TimeSpan nextInterval = ok ? GetAttendanceReminderInterval() : TimeSpan.FromMinutes(reminder.ReminderInterval);
        _incrementRaidNameOnNextReminder = ok;

        _attendanceReminderTimer.Interval = nextInterval;
        _attendanceReminderTimer.Start();
    }

    private void HandleKillCallReminder(string bossName)
    {
        _killCallReminderTimer?.Stop();
        _killCallReminderTimer = null;

        RemindForKillAttendance(bossName);
    }

    private void HandleUpdate(object sender, EventArgs e)
    {
        CheckAndUpdateDisplay();
    }

    private void ReactivateCompletedAuction()
    {
        CompletedAuction selectedAuction = SelectedCompletedAuction;
        if (selectedAuction == null)
            return;

        SelectedCompletedAuction = null;
        _activeBidTracker.ReactivateCompletedAuction(selectedAuction);
    }

    private void RemindForKillAttendance(string bossName)
    {
        if (!RemindAttendances || string.IsNullOrEmpty(bossName))
            return;

        string statusMessageFormat = Strings.GetString("KillAttendanceReminderMessageFormat");
        string statusMessage = string.Format(statusMessageFormat, bossName);

        IReminderDialogViewModel reminder = GetReminderDialog(statusMessage, AttendanceCallType.Kill);
        reminder.AttendanceName = bossName;

        bool doneWithReminder = reminder.ShowDialog() == true;
        if (!doneWithReminder)
        {
            TimeSpan userSpecifiedInterval = TimeSpan.FromMinutes(reminder.ReminderInterval);
            _killCallReminderTimer = new DispatcherTimer(
                userSpecifiedInterval,
                DispatcherPriority.Normal,
                (s, e) => HandleKillCallReminder(bossName),
                Dispatcher.CurrentDispatcher);
            _killCallReminderTimer.Start();
        }
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
        OpenFileDialog fileDialog = new()
        {
            Title = "Select Log File to Monitor",
            InitialDirectory = _settings.EqDirectory,
            DefaultDirectory = _settings.EqDirectory
        };

        if (fileDialog.ShowDialog() != true)
            return;

        string logFile = fileDialog.FileName;
        FilePath = logFile;
    }

    private void SetActiveAuctionToCompleted()
    {
        LiveAuctionDisplay selectedAuction = SelectedActiveAuction;
        if (selectedAuction == null)
            return;

        SelectedActiveAuction = null;

        _activeBidTracker.SetAuctionToCompleted(selectedAuction.Auction);
    }

    private void SetAuctionStatusMessage()
    {
        StatusMarker marker = _activeBidTracker.GetStatusMarkerFromSelectionString(CurrentStatusMarker);
        AuctionStatusMessageToPaste = _activeBidTracker.GetStatusMessage(SelectedActiveAuction?.Auction, marker);
    }

    private void SetReminderForAttendances()
    {
        if (RemindAttendances)
        {
            TimeSpan interval = GetAttendanceReminderInterval();
            _attendanceReminderTimer = new DispatcherTimer(
                interval,
                DispatcherPriority.Normal,
                HandleAttendanceReminderTimer,
                Dispatcher.CurrentDispatcher);
        }
        else
        {
            _attendanceReminderTimer?.Stop();
            _attendanceReminderTimer = null;
        }
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

            CurrentBids = new List<LiveBidInfo>(_activeBidTracker.Bids
                .Where(x => x.ParentAuctionId == SelectedActiveAuction.Id).OrderByDescending(x => x.Timestamp));
            if (selectedCurrentBid != null)
            {
                LiveBidInfo matchingBid = CurrentBids.FirstOrDefault(x => x.ParentAuctionId == selectedCurrentBid.ParentAuctionId
                    && x.ItemName == selectedCurrentBid.ItemName
                    && x.BidAmount == selectedCurrentBid.BidAmount
                    && x.CharacterName == selectedCurrentBid.CharacterName);
                SelectedBid = matchingBid;
            }

            HighBids = new List<LiveBidInfo>(_activeBidTracker.GetHighBids(SelectedActiveAuction.Auction));

            SpentMessagesToPaste = _activeBidTracker.GetSpentInfoForCurrentHighBids(SelectedActiveAuction.Auction);
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
        CompletedAuctions = new List<CompletedAuction>(_activeBidTracker.CompletedAuctions);
        if (selectedCompleted != null)
        {
            CompletedAuction matchingCompleted = CompletedAuctions.FirstOrDefault(x => x.AuctionStart.Id == selectedCompleted.AuctionStart.Id);
            if (matchingCompleted != null)
                SelectedCompletedAuction = matchingCompleted;
        }

        RemindForKillAttendance(_activeBidTracker.GetBossKilledName());

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

public interface ILiveLogTrackingViewModel : IEuropaViewModel
{
    ICollection<LiveAuctionDisplay> ActiveAuctions { get; }

    DelegateCommand AddItemLinkIdCommand { get; }

    string AuctionStatusMessageToPaste { get; set; }

    ICollection<CompletedAuction> CompletedAuctions { get; }

    DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    DelegateCommand CopySelectedStatusMessageToClipboardCommand { get; }

    ICollection<LiveBidInfo> CurrentBids { get; }

    string CurrentStatusMarker { get; }

    DelegateCommand CycleToNextStatusMarkerCommand { get; }

    string FilePath { get; set; }

    ICollection<LiveBidInfo> HighBids { get; }

    string ItemLinkIdToAdd { get; set; }

    ICollection<string> LogFileNames { get; }

    DelegateCommand ReactivateCompletedAuctionCommand { get; }

    bool RemindAttendances { get; set; }

    DelegateCommand RemoveBidCommand { get; }

    LiveAuctionDisplay SelectedActiveAuction { get; set; }

    LiveBidInfo SelectedBid { get; set; }

    CompletedAuction SelectedCompletedAuction { get; set; }

    SuggestedSpentCall SelectedSpentMessageToPaste { get; set; }

    DelegateCommand SelectFileToTailCommand { get; }

    DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    ICollection<SuggestedSpentCall> SpentMessagesToPaste { get; }

    void Close();
}
