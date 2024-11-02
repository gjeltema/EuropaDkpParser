// -----------------------------------------------------------------------
// LiveLogTrackingViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class LiveLogTrackingViewModel : DialogViewModelBase, ILiveLogTrackingViewModel
{
    private ICollection<string> _activeAuctions;
    private ICollection<string> _auctionStatusMarkers;
    private string _auctionStatusMessagesToPaste;
    private ICollection<string> _completedAuctions;
    private ICollection<string> _currentBids;
    private string _filePath;
    private ICollection<string> _highBids;
    private string _selectedActiveAuction;
    private string _selectedAuctionStatusMarker;
    private string _selectedBid;
    private string _selectedCompletedAuction;

    public LiveLogTrackingViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("LiveLogTrackingDialogTitleText");


    }

    public ICollection<string> ActiveAuctions
    {
        get => _activeAuctions;
        private set => SetProperty(ref _activeAuctions, value);
    }

    public ICollection<string> AuctionStatusMarkers
    {
        get => _auctionStatusMarkers;
        private set => SetProperty(ref _auctionStatusMarkers, value);
    }

    public string AuctionStatusMessagesToPaste
    {
        get => _auctionStatusMessagesToPaste;
        private set => SetProperty(ref _auctionStatusMessagesToPaste, value);
    }

    public ICollection<string> CompletedAuctions
    {
        get => _completedAuctions;
        private set => SetProperty(ref _completedAuctions, value);
    }

    public ICollection<string> CurrentBids
    {
        get => _currentBids;
        private set => SetProperty(ref _currentBids, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public ICollection<string> HighBids
    {
        get => _highBids;
        private set => SetProperty(ref _highBids, value);
    }

    public DelegateCommand ReactivateCompletedAuction { get; }

    public DelegateCommand RemoveBid { get; }

    public string SelectedActiveAuction
    {
        get => _selectedActiveAuction;
        set => SetProperty(ref _selectedActiveAuction, value);
    }

    public string SelectedAuctionStatusMarker
    {
        get => _selectedAuctionStatusMarker;
        set => SetProperty(ref _selectedAuctionStatusMarker, value);
    }

    public string SelectedBid
    {
        get => _selectedBid;
        set => SetProperty(ref _selectedBid, value);
    }

    public string SelectedCompletedAuction
    {
        get => _selectedCompletedAuction;
        set => SetProperty(ref _selectedCompletedAuction, value);
    }

    public DelegateCommand SelectFileToTail { get; }

    public DelegateCommand SetActiveAuctionToCompleted { get; }
}

public interface ILiveLogTrackingViewModel : IDialogViewModel
{
    ICollection<string> ActiveAuctions { get; }

    ICollection<string> AuctionStatusMarkers { get; }

    string AuctionStatusMessagesToPaste { get; }

    ICollection<string> CompletedAuctions { get; }

    ICollection<string> CurrentBids { get; }

    string FilePath { get; set; }

    ICollection<string> HighBids { get; }

    DelegateCommand ReactivateCompletedAuction { get; }

    DelegateCommand RemoveBid { get; }

    string SelectedActiveAuction { get; set; }

    string SelectedAuctionStatusMarker { get; set; }

    string SelectedBid { get; set; }

    string SelectedCompletedAuction { get; set; }

    DelegateCommand SelectFileToTail { get; }

    DelegateCommand SetActiveAuctionToCompleted { get; }
}
