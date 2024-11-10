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

internal sealed class LiveLogTrackingViewModel : DialogViewModelBase, ILiveLogTrackingViewModel
{
    private readonly ActiveBidTracker _activeBidTracker;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(500);
    private ICollection<LiveAuctionInfo> _activeAuctions;
    private ICollection<string> _auctionStatusMarkers;
    private string _auctionStatusMessagesToPaste;
    private ICollection<CompletedAuction> _completedAuctions;
    private ICollection<LiveBidInfo> _currentBids;
    private string _filePath;
    private ICollection<LiveBidInfo> _highBids;
    private LiveAuctionInfo _selectedActiveAuction;
    private string _selectedAuctionStatusMarker;
    private LiveBidInfo _selectedBid;
    private CompletedAuction _selectedCompletedAuction;
    private LiveSpentCall _selectedSpentMessageToPaste;
    private ICollection<LiveSpentCall> _spentMessagesToPaste;
    private DispatcherTimer _updateTimer;

    public LiveLogTrackingViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        _settings = settings;

        Title = Strings.GetString("LiveLogTrackingDialogTitleText");

        _activeBidTracker = new(settings);
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);

        CopySelectedSpentCallToClipboardCommand = new DelegateCommand(CopySelectedSpentCallToClipboard);
        ReactivateCompletedAuctionCommand = new DelegateCommand(ReactivateCompletedAuction);
        RemoveBidCommand = new DelegateCommand(RemoveBid);
        SelectFileToTailCommand = new DelegateCommand(SelectFileToTail);
        SetActiveAuctionToCompletedCommand = new DelegateCommand(SetActiveAuctionToCompleted);
        SetAsHighBidCommand = new DelegateCommand(SetAsHighBid);
    }

    public ICollection<LiveAuctionInfo> ActiveAuctions
    {
        get => _activeAuctions;
        private set => SetProperty(ref _activeAuctions, value);
    }

    public ICollection<string> AuctionStatusMarkers
    {
        get => _auctionStatusMarkers;
        private set => SetProperty(ref _auctionStatusMarkers, value);
    }

    public string AuctionStatusMessageToPaste
    {
        get => _auctionStatusMessagesToPaste;
        private set => SetProperty(ref _auctionStatusMessagesToPaste, value);
    }

    public ICollection<CompletedAuction> CompletedAuctions
    {
        get => _completedAuctions;
        private set => SetProperty(ref _completedAuctions, value);
    }

    public DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    public ICollection<LiveBidInfo> CurrentBids
    {
        get => _currentBids;
        private set => SetProperty(ref _currentBids, value);
    }

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

    public DelegateCommand ReactivateCompletedAuctionCommand { get; }

    public DelegateCommand RemoveBidCommand { get; }

    public LiveAuctionInfo SelectedActiveAuction
    {
        get => _selectedActiveAuction;
        set => SetProperty(ref _selectedActiveAuction, value);
    }

    public string SelectedAuctionStatusMarker
    {
        get => _selectedAuctionStatusMarker;
        set => SetProperty(ref _selectedAuctionStatusMarker, value);
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

    public DelegateCommand SetAsHighBidCommand { get; }

    public ICollection<LiveSpentCall> SpentMessagesToPaste
    {
        get => _spentMessagesToPaste;
        private set => SetProperty(ref _spentMessagesToPaste, value);
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
        string messageWithLink = selectedSpentCall.ToMessageWithLink();
        bool success = Clip.Copy(messageWithLink);
        if (!success)
            Clip.Copy(messageWithLink);
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

    //Sets the selected active bid as the high bid (work it in to multiple items if needed)
    private void SetAsHighBid()
    {
        //** Implement later
    }

    private void UpdateDisplay()
    {
        _activeBidTracker.Updated = false;

        ActiveAuctions = new List<LiveAuctionInfo>(_activeBidTracker.ActiveAuctions);
        CompletedAuctions = new List<CompletedAuction>(_activeBidTracker.CompletedAuctions);
        CurrentBids = new List<LiveBidInfo>(_activeBidTracker.Bids.Where(x => x.ParentAuctionId == SelectedActiveAuction.Id).OrderByDescending(x => x.Timestamp));
        HighBids = new List<LiveBidInfo>(_activeBidTracker.GetHighBids(SelectedActiveAuction));

        StatusMarker marker = _activeBidTracker.GetStatusMarkerFromString(SelectedAuctionStatusMarker);
        AuctionStatusMessageToPaste = _activeBidTracker.GetStatusMessage(SelectedActiveAuction, marker);
        SpentMessagesToPaste = _activeBidTracker.GetSpentMessagesForCurrentHighBids(SelectedActiveAuction);
    }
}

public interface ILiveLogTrackingViewModel : IDialogViewModel
{
    ICollection<LiveAuctionInfo> ActiveAuctions { get; }

    ICollection<string> AuctionStatusMarkers { get; }

    string AuctionStatusMessageToPaste { get; }

    ICollection<CompletedAuction> CompletedAuctions { get; }

    DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    ICollection<LiveBidInfo> CurrentBids { get; }

    string FilePath { get; set; }

    ICollection<LiveBidInfo> HighBids { get; }

    DelegateCommand ReactivateCompletedAuctionCommand { get; }

    DelegateCommand RemoveBidCommand { get; }

    LiveAuctionInfo SelectedActiveAuction { get; set; }

    string SelectedAuctionStatusMarker { get; set; }

    LiveBidInfo SelectedBid { get; set; }

    CompletedAuction SelectedCompletedAuction { get; set; }

    LiveSpentCall SelectedSpentMessageToPaste { get; set; }

    DelegateCommand SelectFileToTailCommand { get; }

    DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    DelegateCommand SetAsHighBidCommand { get; }

    ICollection<LiveSpentCall> SpentMessagesToPaste { get; }
}
