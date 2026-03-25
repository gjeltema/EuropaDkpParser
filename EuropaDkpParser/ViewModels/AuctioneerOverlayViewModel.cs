// -----------------------------------------------------------------------
// AuctioneerOverlayViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows.Threading;
using DkpParser;
using DkpParser.LiveTracking;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal sealed class AuctioneerOverlayViewModel : OverlayViewModelBase, IAuctioneerOverlayViewModel
{
    private const string LogPrefix = $"[{nameof(AuctioneerOverlayViewModel)}]";
    private readonly ActiveBidTracker _activeBidTracker;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    private readonly DispatcherTimer _updateTimer;
    private DateTime _nextForcedUpdate = DateTime.MinValue;

    public AuctioneerOverlayViewModel(IOverlayViewFactory viewFactory, IDkpParserSettings settings, IEqLogTailFile eqLogTailFile)
        : base(viewFactory)
    {
        _settings = settings;

        AllowResizing = true;
        ActiveAuctions = [];

        XPos = _settings.AuctionOverlayXLoc;
        YPos = _settings.AuctionOverlayYLoc;
        Width = _settings.AuctionOverlayWidth;
        Height = _settings.AuctionOverlayHeight;

        _activeBidTracker = new(settings, eqLogTailFile);
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);

        CopySelectedSpentCallToClipboardCommand = new DelegateCommand(CopySelectedSpentCallToClipboard, () => SelectedSpentMessageToPaste != null)
            .ObservesProperty(() => SelectedSpentMessageToPaste);
        CopySelectedStatusMessageToClipboardCommand = new DelegateCommand(CopySelectedStatusMessageToClipboard,
            () => SelectedActiveAuction != null && _activeBidTracker.Bids.Any(x => x.ParentAuctionId == SelectedActiveAuction?.Id))
            .ObservesProperty(() => SelectedActiveAuction);
        CycleToNextStatusMarkerCommand = new DelegateCommand(CycleToNextStatusMarker);
        SetActiveAuctionToCompletedCommand = new DelegateCommand(SetActiveAuctionToCompleted, () => SelectedActiveAuction != null)
            .ObservesProperty(() => SelectedActiveAuction);

        CurrentStatusMarker = _activeBidTracker.GetNextStatusMarkerForSelection("");

        _activeBidTracker.StartTracking();
    }

    public ICollection<LiveAuctionDisplay> ActiveAuctions
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                RefreshDisplayControls();
        }
    }

    public DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    public DelegateCommand CopySelectedStatusMessageToClipboardCommand { get; }

    public string CurrentStatusMarker { get; private set => SetProperty(ref field, value); }

    public DelegateCommand CycleToNextStatusMarkerCommand { get; }

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

    public SuggestedSpentCall SelectedSpentMessageToPaste { get; set => SetProperty(ref field, value); }

    public DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    public ICollection<SuggestedSpentCall> SpentMessagesToPaste { get; private set => SetProperty(ref field, value); }

    protected override void HandleClose()
    {
        _updateTimer.Stop();
        _activeBidTracker.StopTracking();
    }

    protected override void SaveLocation()
    {
        _settings.AuctionOverlayXLoc = XPos;
        _settings.AuctionOverlayYLoc = YPos;
        _settings.AuctionOverlayWidth = Width;
        _settings.AuctionOverlayHeight = Height;
        _settings.SaveSettings();
    }

    private void CopySelectedSpentCallToClipboard()
    {
        SuggestedSpentCall selectedSpentCall = SelectedSpentMessageToPaste;
        if (selectedSpentCall == null)
            return;

        string spentCallWithLink = _activeBidTracker.GetSpentMessageWithLink(selectedSpentCall);
        Clip.Copy(spentCallWithLink);

        SelectedActiveAuction.HasNewBidsAdded = false;
    }

    private void CopySelectedStatusMessageToClipboard()
    {
        StatusMarker marker = _activeBidTracker.GetStatusMarkerFromSelectionString(CurrentStatusMarker);
        string selectedStatsuMessage = _activeBidTracker.GetStatusMessage(SelectedActiveAuction?.Auction, marker, true);
        if (selectedStatsuMessage == null)
            return;

        Clip.Copy(selectedStatsuMessage);

        SelectedActiveAuction.HasNewBidsAdded = false;
    }

    private void CycleToNextStatusMarker()
    {
        CurrentStatusMarker = _activeBidTracker.GetNextStatusMarkerForSelection(CurrentStatusMarker);
    }

    private void HandleUpdate(object sender, EventArgs e)
    {
        if (!_activeBidTracker.Updated && DateTime.Now < _nextForcedUpdate)
            return;

        UpdateDisplay();
    }

    private void RefreshDisplayControls()
    {
        if (IsInMoveMode)
            return;

        if (ActiveAuctions.Count == 0)
        {
            if (ContentIsVisible)
                HideOverlay();
        }
        else
        {
            if (!ContentIsVisible)
                ShowOverlay();
        }
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

    private void UpdateActiveAuctionSelected()
        => UpdateActiveAuctionSelected(SelectedActiveAuction);

    private void UpdateActiveAuctionSelected(LiveAuctionDisplay selectedAuction)
    {
        if (selectedAuction != null)
        {
            SpentMessagesToPaste = _activeBidTracker.GetSpentInfoForCurrentHighBids(selectedAuction.Auction, true);
        }
        else
        {
            SpentMessagesToPaste = [];
        }
    }

    private void UpdateDisplay()
    {
        _activeBidTracker.Updated = false;

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

        UpdateActiveAuctionSelected(selectedAuction);

        _nextForcedUpdate = DateTime.Now.AddSeconds(10);
    }
}

public interface IAuctioneerOverlayViewModel : IOverlayViewModel
{
    ICollection<LiveAuctionDisplay> ActiveAuctions { get; }

    DelegateCommand CopySelectedSpentCallToClipboardCommand { get; }

    DelegateCommand CopySelectedStatusMessageToClipboardCommand { get; }

    string CurrentStatusMarker { get; }

    DelegateCommand CycleToNextStatusMarkerCommand { get; }

    LiveAuctionDisplay SelectedActiveAuction { get; set; }

    ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; }

    SuggestedSpentCall SelectedSpentMessageToPaste { get; set; }

    DelegateCommand SetActiveAuctionToCompletedCommand { get; }

    ICollection<SuggestedSpentCall> SpentMessagesToPaste { get; }
}
