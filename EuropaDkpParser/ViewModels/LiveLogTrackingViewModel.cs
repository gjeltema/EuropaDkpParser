﻿// -----------------------------------------------------------------------
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
    private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(500);
    private readonly DispatcherTimer _updateTimer;
    private ICollection<LiveAuctionInfo> _activeAuctions;
    private DispatcherTimer _attendanceReminderTimer;
    private string _auctionStatusMessagesToPaste;
    private ICollection<CompletedAuction> _completedAuctions;
    private ICollection<LiveBidInfo> _currentBids;
    private string _currentStatusMarker;
    private string _filePath;
    private ICollection<LiveBidInfo> _highBids;
    private string _itemLinkIdToAdd;
    private bool _remindAttendances;
    private LiveAuctionInfo _selectedActiveAuction;
    private LiveBidInfo _selectedBid;
    private CompletedAuction _selectedCompletedAuction;
    private LiveSpentCall _selectedSpentMessageToPaste;
    private ICollection<LiveSpentCall> _spentMessagesToPaste;

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
    }

    public ICollection<LiveAuctionInfo> ActiveAuctions
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
        set => SetProperty(ref _filePath, value);
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

    public LiveAuctionInfo SelectedActiveAuction
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

    public LiveSpentCall SelectedSpentMessageToPaste
    {
        get => _selectedSpentMessageToPaste;
        set => SetProperty(ref _selectedSpentMessageToPaste, value);
    }

    public DelegateCommand SelectFileToTailCommand { get; }

    public DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    public ICollection<LiveSpentCall> SpentMessagesToPaste
    {
        get => _spentMessagesToPaste;
        private set => SetProperty(ref _spentMessagesToPaste, value);
    }

    public void Close()
    {
        _updateTimer.Stop();
        _activeBidTracker.StopTracking();
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
        if (!_activeBidTracker.Updated)
        {
            //** Check for 5 seconds elapsed
            return;
        }

        UpdateDisplay();
    }

    private void CopySelectedSpentCallToClipboard()
    {
        LiveSpentCall selectedSpentCall = SelectedSpentMessageToPaste;
        if (selectedSpentCall == null)
            return;

        string spentCallWithLink = _activeBidTracker.GetSpentMessageWithLink(selectedSpentCall);
        bool success = Clip.Copy(spentCallWithLink);
        if (!success)
            Clip.Copy(spentCallWithLink);
    }

    private void CopySelectedStatusMessageToClipboard()
    {
        string selectedStatsuMessage = AuctionStatusMessageToPaste;
        if (selectedStatsuMessage == null)
            return;

        bool success = Clip.Copy(selectedStatsuMessage);
        if (!success)
            Clip.Copy(selectedStatsuMessage);
    }

    private void CycleToNextStatusMarker()
    {
        CurrentStatusMarker = _activeBidTracker.GetNextStatusMarkerForSelection(CurrentStatusMarker);
        SetStatusMessage();
    }

    private TimeSpan GetAttendanceReminderInterval()
    {
        return TimeSpan.FromMinutes(1); //** Temp
        int minutes = DateTime.Now.Minute;
        if (minutes < 30)
            return TimeSpan.FromMinutes(30 - minutes);
        else
            return TimeSpan.FromMinutes(60 - minutes);
    }

    private void HandleAttendanceReminderTimer(object sender, EventArgs e)
    {
        _attendanceReminderTimer.Stop();

        TimeSpan interval = RemindAboutAttendance(Strings.GetString("TimeAttendanceReminderMessage"));

        _attendanceReminderTimer.Interval = interval;
        _attendanceReminderTimer.Start();
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
        UpdateActiveAuctionSelected();
    }

    private TimeSpan RemindAboutAttendance(string reminderText)
    {
        IReminderDialogViewModel reminderDialogViewModel = _dialogFactory.CreateReminderDialogViewModel();
        reminderDialogViewModel.ReminderText = reminderText;

        if (reminderDialogViewModel.ShowDialog() == true)
        {
            return GetAttendanceReminderInterval();
        }
        else
        {
            return TimeSpan.FromMinutes(reminderDialogViewModel.ReminderInterval);
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
            DefaultDirectory = _settings.EqDirectory
        };

        if (fileDialog.ShowDialog() != true)
            return;

        string logFile = fileDialog.FileName;
        FilePath = logFile;

        if (string.IsNullOrWhiteSpace(logFile))
            return;

        if (!File.Exists(logFile))
            return;

        _activeBidTracker.StopTracking();
        _activeBidTracker.StartTracking(logFile);
    }

    private void SetActiveAuctionToCompleted()
    {
        LiveAuctionInfo selectedAuction = SelectedActiveAuction;
        if (selectedAuction == null)
            return;

        SelectedActiveAuction = null;

        _activeBidTracker.SetAuctionToCompleted(selectedAuction);
        UpdateActiveAuctionSelected();
    }

    private void SetReminderForAttendances()
    {
        if (RemindAttendances)
        {
            TimeSpan interval = GetAttendanceReminderInterval();
            _attendanceReminderTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, HandleAttendanceReminderTimer, Dispatcher.CurrentDispatcher);
        }
        else
        {
            _attendanceReminderTimer?.Stop();
            _attendanceReminderTimer = null;
        }
    }

    private void SetStatusMessage()
    {
        StatusMarker marker = _activeBidTracker.GetStatusMarkerFromSelectionString(CurrentStatusMarker);
        AuctionStatusMessageToPaste = _activeBidTracker.GetStatusMessage(SelectedActiveAuction, marker);
    }

    private void UpdateActiveAuctionSelected()
    {
        if (SelectedActiveAuction != null)
        {
            CurrentBids = new List<LiveBidInfo>(_activeBidTracker.Bids.Where(x => x.ParentAuctionId == SelectedActiveAuction.Id).OrderByDescending(x => x.Timestamp));
            HighBids = new List<LiveBidInfo>(_activeBidTracker.GetHighBids(SelectedActiveAuction));
            SpentMessagesToPaste = _activeBidTracker.GetSpentInfoForCurrentHighBids(SelectedActiveAuction);
        }
        else
        {
            CurrentBids = [];
            HighBids = [];
            SpentMessagesToPaste = [];
        }

        SetStatusMessage();
    }

    private void UpdateDisplay()
    {
        _activeBidTracker.Updated = false;

        ActiveAuctions = new List<LiveAuctionInfo>(_activeBidTracker.ActiveAuctions);
        CompletedAuctions = new List<CompletedAuction>(_activeBidTracker.CompletedAuctions);
        UpdateActiveAuctionSelected();
    }
}

public interface ILiveLogTrackingViewModel : IEuropaViewModel
{
    ICollection<LiveAuctionInfo> ActiveAuctions { get; }

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

    DelegateCommand ReactivateCompletedAuctionCommand { get; }

    bool RemindAttendances { get; set; }

    DelegateCommand RemoveBidCommand { get; }

    LiveAuctionInfo SelectedActiveAuction { get; set; }

    LiveBidInfo SelectedBid { get; set; }

    CompletedAuction SelectedCompletedAuction { get; set; }

    LiveSpentCall SelectedSpentMessageToPaste { get; set; }

    DelegateCommand SelectFileToTailCommand { get; }

    DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    ICollection<LiveSpentCall> SpentMessagesToPaste { get; }

    void Close();
}
