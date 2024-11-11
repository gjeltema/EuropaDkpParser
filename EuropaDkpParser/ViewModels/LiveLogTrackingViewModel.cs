// -----------------------------------------------------------------------
// LiveLogTrackingViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows.Threading;
using DkpParser;
using DkpParser.LiveTracking;
using EuropaDkpParser.Utility;
using Microsoft.Win32;
using Prism.Commands;

internal sealed class LiveLogTrackingViewModel : EuropaViewModelBase, ILiveLogTrackingViewModel
{
    private readonly ActiveBidTracker _activeBidTracker;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(500);
    private readonly DispatcherTimer _updateTimer;
    private ICollection<LiveAuctionInfo> _activeAuctions;
    private string _auctionStatusMessagesToPaste;
    private ICollection<CompletedAuction> _completedAuctions;
    private ICollection<LiveBidInfo> _currentBids;
    private string _currentStatusMarker;
    private string _filePath;
    private ICollection<LiveBidInfo> _highBids;
    private string _itemLinkIdToAdd;
    private LiveAuctionInfo _selectedActiveAuction;
    private LiveBidInfo _selectedBid;
    private CompletedAuction _selectedCompletedAuction;
    private LiveSpentCall _selectedSpentMessageToPaste;
    private ICollection<LiveSpentCall> _spentMessagesToPaste;

    public LiveLogTrackingViewModel(IDkpParserSettings settings)
    {
        _settings = settings;

        _activeBidTracker = new(settings);
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);

        CopySelectedSpentCallToClipboardCommand = new DelegateCommand(CopySelectedSpentCallToClipboard);
        CopySelectedStatusMessageToClipboardCommand = new DelegateCommand(CopySelectedStatusMessageToClipboard);
        ReactivateCompletedAuctionCommand = new DelegateCommand(ReactivateCompletedAuction);
        RemoveBidCommand = new DelegateCommand(RemoveBid);
        SelectFileToTailCommand = new DelegateCommand(SelectFileToTail);
        SetActiveAuctionToCompletedCommand = new DelegateCommand(SetActiveAuctionToCompleted);
        CycleToNextStatusMarkerCommand = new DelegateCommand(CycleToNextStatusMarker);
        AddItemLinkIdCommand = new DelegateCommand(AddItemLinkId);

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

    public DelegateCommand RemoveBidCommand { get; }

    public LiveAuctionInfo SelectedActiveAuction
    {
        get => _selectedActiveAuction;
        set => SetProperty(ref _selectedActiveAuction, value);
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

    private void AddItemLinkId()
    {
        if (string.IsNullOrWhiteSpace(ItemLinkIdToAdd))
            return;

        if (SelectedActiveAuction == null)
            return;

        _settings.ItemLinkIds.AddItemId(SelectedActiveAuction.ItemName, ItemLinkIdToAdd);
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

    private void RemoveBid()
    {
        LiveBidInfo selectedBid = SelectedBid;
        if (selectedBid == null)
            return;

        SelectedBid = null;

        _activeBidTracker.RemoveBid(selectedBid);
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
    }

    private void SetStatusMessage()
    {
        StatusMarker marker = _activeBidTracker.GetStatusMarkerFromSelectionString(CurrentStatusMarker);
        AuctionStatusMessageToPaste = _activeBidTracker.GetStatusMessage(SelectedActiveAuction, marker);
    }

    private void UpdateDisplay()
    {
        _activeBidTracker.Updated = false;

        ActiveAuctions = new List<LiveAuctionInfo>(_activeBidTracker.ActiveAuctions);
        CompletedAuctions = new List<CompletedAuction>(_activeBidTracker.CompletedAuctions);
        CurrentBids = new List<LiveBidInfo>(_activeBidTracker.Bids.Where(x => x.ParentAuctionId == SelectedActiveAuction.Id).OrderByDescending(x => x.Timestamp));
        HighBids = new List<LiveBidInfo>(_activeBidTracker.GetHighBids(SelectedActiveAuction));

        SpentMessagesToPaste = _activeBidTracker.GetSpentInfoForCurrentHighBids(SelectedActiveAuction);
        SetStatusMessage();
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

    DelegateCommand RemoveBidCommand { get; }

    LiveAuctionInfo SelectedActiveAuction { get; set; }

    LiveBidInfo SelectedBid { get; set; }

    CompletedAuction SelectedCompletedAuction { get; set; }

    LiveSpentCall SelectedSpentMessageToPaste { get; set; }

    DelegateCommand SelectFileToTailCommand { get; }

    DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    ICollection<LiveSpentCall> SpentMessagesToPaste { get; }
}
