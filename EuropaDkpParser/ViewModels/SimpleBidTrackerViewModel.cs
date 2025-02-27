// -----------------------------------------------------------------------
// SimpleBidTrackerViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows.Threading;
using DkpParser;
using DkpParser.LiveTracking;
using Gjeltema.Logging;
using Prism.Commands;

internal sealed class SimpleBidTrackerViewModel : WindowViewModelBase, ISimpleBidTrackerViewModel
{
    private const string LogPrefix = $"{nameof(SimpleBidTrackerViewModel)}";
    private readonly ActiveBidTracker _activeBidTracker;
    private readonly IDkpParserSettings _settings;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    private readonly DispatcherTimer _updateTimer;
    private ICollection<LiveAuctionDisplay> _activeAuctions;
    private ICollection<CompletedAuction> _completedAuctions;
    private int _fontSize;
    private string _fontSizeSetting;
    private bool _lowRollWins;
    private DateTime _nextForcedUpdate = DateTime.MinValue;
    private string _selectedLogFilePath;

    public SimpleBidTrackerViewModel(IWindowViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        _settings = settings;

        _activeBidTracker = new(settings, new TailFile());
        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);

        LogFileNames = [.. _settings.SelectedLogFiles];

        SetActiveAuctionsToCompletedCommand = new DelegateCommand(SetActiveAuctionsToCompleted);

        _fontSize = 12;
        _fontSizeSetting = "12";
    }

    public ICollection<LiveAuctionDisplay> ActiveAuctions
    {
        get => _activeAuctions;
        private set => SetProperty(ref _activeAuctions, value);
    }

    public ICollection<CompletedAuction> CompletedAuctions
    {
        get => _completedAuctions;
        private set => SetProperty(ref _completedAuctions, value);
    }

    public int FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    public string FontSizeSetting
    {
        get => _fontSizeSetting;
        set
        {
            if (int.TryParse(value, out int finalFontSize))
            {
                FontSize = finalFontSize;
                SetProperty(ref _fontSizeSetting, value);
            }
        }
    }

    public ICollection<string> LogFileNames { get; }

    public bool LowRollWins
    {
        get => _lowRollWins;
        set => SetProperty(ref _lowRollWins, value);
    }

    public ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; } = [];

    public string SelectedLogFilePath
    {
        get => _selectedLogFilePath;
        set
        {
            if (SetProperty(ref _selectedLogFilePath, value))
            {
                StartTailingFile(value);
                Log.Debug($"{LogPrefix} {nameof(SelectedLogFilePath)} being set to {value}.");
            }
        }
    }

    public DelegateCommand SetActiveAuctionsToCompletedCommand { get; }

    protected override sealed IWindowView CreateWindowView(IWindowViewFactory viewFactory)
        => viewFactory.CreateSimpleBidTrackerWindow(this);

    protected override sealed void HandleClosing()
    {
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
        if (SelectedActiveAuctions.Count == 0)
            return;

        ICollection<LiveAuctionDisplay> selectedAuctions = [.. SelectedActiveAuctions];

        foreach (LiveAuctionDisplay selectedAuction in selectedAuctions)
        {
            _activeBidTracker.SetAuctionToCompleted(selectedAuction.Auction);
        }

        SelectedActiveAuctions.Clear();
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

    int FontSize { get; set; }

    string FontSizeSetting { get; set; }

    ICollection<string> LogFileNames { get; }

    bool LowRollWins { get; set; }

    ICollection<LiveAuctionDisplay> SelectedActiveAuctions { get; set; }

    string SelectedLogFilePath { get; set; }

    DelegateCommand SetActiveAuctionsToCompletedCommand { get; }
}
