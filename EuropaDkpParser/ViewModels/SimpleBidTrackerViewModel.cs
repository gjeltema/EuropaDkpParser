// -----------------------------------------------------------------------
// SimpleBidTrackerViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows.Threading;
using DkpParser;
using DkpParser.LiveTracking;
using Gjeltema.Logging;
using Prism.Commands;

internal sealed class SimpleBidTrackerViewModel : WindowViewModelBase, ISimpleBidTrackerViewModel
{
    private const string LogPrefix = $"[{nameof(SimpleBidTrackerViewModel)}]";
    private readonly ActiveBidTracker _activeBidTracker;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    private readonly DispatcherTimer _updateTimer;
    private DateTime _nextForcedUpdate = DateTime.MinValue;

    public SimpleBidTrackerViewModel(IWindowViewFactory viewFactory, IDkpParserSettings settings, IEqLogTailFile eqLogTailFile)
        : base(viewFactory)
    {
        _settings = settings;

        _activeBidTracker = new(settings, eqLogTailFile);
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);

        LogFileNames = [.. _settings.SelectedLogFiles];

        SetActiveAuctionsToCompletedCommand = new DelegateCommand(SetActiveAuctionsToCompleted);

        SelectedFontSize = 12;

        _activeBidTracker.StartTracking();
    }

    public ICollection<LiveAuctionDisplay> ActiveAuctions { get; private set => SetProperty(ref field, value); }

    public ICollection<CompletedAuction> CompletedAuctions { get; private set => SetProperty(ref field, value); }

    public ICollection<int> FontSizeValues { get; } = [10, 12, 14, 16, 18, 20, 24, 28, 32];

    public ICollection<string> LogFileNames { get; }

    public bool LowRollWins { get; set => SetProperty(ref field, value); }

    public ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; } = [];

    public int SelectedFontSize { get; set => SetProperty(ref field, value); }

    public string SelectedLogFilePath
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Log.Info($"{LogPrefix} {nameof(SelectedLogFilePath)} being set to {value}.");
                _activeBidTracker.StartTracking(value);
            }
        }
    }

    public DelegateCommand SetActiveAuctionsToCompletedCommand { get; }

    protected override sealed IWindowView CreateWindowView(IWindowViewFactory viewFactory)
        => viewFactory.CreateSimpleBidTrackerWindow(this);

    protected override sealed void HandleClosing()
    {
        Log.Info($"{LogPrefix} {nameof(HandleClosing)} called.");
        _updateTimer.Stop();
        _activeBidTracker.StopTracking();
    }

    private DateTime GetSortingTimestamp(CompletedAuction completed)
    {
        if (completed.SpentCalls.Count > 0)
            return completed.SpentCalls.Max(x => x.Timestamp);
        else
            return completed.AuctionStart.Timestamp;
    }

    private void HandleUpdate(object sender, EventArgs e)
    {
        if (!_activeBidTracker.Updated && DateTime.Now < _nextForcedUpdate)
            return;

        UpdateDisplay();
    }

    private LiveAuctionDisplay InitializeLiveAuctionDisplay(LiveAuctionInfo auctionInfo)
    {
        ICollection<LiveBidInfo> highBidders = _activeBidTracker.GetHighBids(auctionInfo, LowRollWins);
        string highBiddersDisplay = "";
        if (highBidders.Count > 0)
            highBiddersDisplay = $"({string.Join(", ", highBidders.Select(x => $"{x.CharacterBeingBidFor} {x.BidAmount} DKP"))})";
        string fullInfo = auctionInfo.IsRoll
            ? $"{auctionInfo.Timestamp:HH:mm} {auctionInfo.ItemName}{(auctionInfo.TotalNumberOfItems > 1 ? " x" + auctionInfo.TotalNumberOfItems.ToString() : "")} roll:{auctionInfo.RandValue} {highBiddersDisplay}"
            : $"{auctionInfo.Timestamp:HH:mm} {auctionInfo.ItemName}{(auctionInfo.TotalNumberOfItems > 1 ? " x" + auctionInfo.TotalNumberOfItems.ToString() : "")} {highBiddersDisplay}";
        LiveAuctionDisplay display = new(auctionInfo) { FullInfo = fullInfo };
        return display;
    }

    private void SetActiveAuctionsToCompleted()
    {
        if (SelectedActiveAuctions == null || SelectedActiveAuctions.Count == 0)
            return;

        ICollection<LiveAuctionDisplay> selectedAuctions = [.. SelectedActiveAuctions];

        foreach (LiveAuctionDisplay selectedAuction in selectedAuctions)
        {
            _activeBidTracker.SetAuctionToCompleted(selectedAuction.Auction);
        }

        SelectedActiveAuctions.Clear();
    }

    private void UpdateDisplay()
    {
        _activeBidTracker.Updated = false;

        ActiveAuctions = _activeBidTracker.ActiveAuctions
            .OrderByDescending(x => x.Timestamp)
            .Select(InitializeLiveAuctionDisplay)
            .ToList();

        CompletedAuctions = [.. _activeBidTracker.CompletedAuctions.OrderByDescending(GetSortingTimestamp)];

        _nextForcedUpdate = DateTime.Now.AddSeconds(10);
    }
}

public interface ISimpleBidTrackerViewModel : IWindowViewModel
{
    ICollection<LiveAuctionDisplay> ActiveAuctions { get; }

    ICollection<CompletedAuction> CompletedAuctions { get; }

    ICollection<int> FontSizeValues { get; }

    ICollection<string> LogFileNames { get; }

    bool LowRollWins { get; set; }

    ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; }

    int SelectedFontSize { get; set; }

    string SelectedLogFilePath { get; set; }

    DelegateCommand SetActiveAuctionsToCompletedCommand { get; }
}
