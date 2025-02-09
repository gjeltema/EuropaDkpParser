// -----------------------------------------------------------------------
// LiveLogTrackingView.xaml.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows;
using System.Windows.Controls;
using EuropaDkpParser.ViewModels;

public partial class LiveLogTrackingView : Window
{
    public LiveLogTrackingView(ILiveLogTrackingViewModel liveLogTrackingViewModel)
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
        DataContext = liveLogTrackingViewModel;

        ActiveAuctionListing.SelectionChanged += HandleSelectionChanged;
    }

    private void ClosedHandler(object sender, EventArgs e)
    {
        ILiveLogTrackingViewModel dc = DataContext as ILiveLogTrackingViewModel;
        dc?.HandleClosed();
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ILiveLogTrackingViewModel dc)
            return;

        dc.SelectedActiveAuctions.Clear();

        foreach (object selectedItem in ActiveAuctionListing.SelectedItems)
        {
            if (selectedItem is LiveAuctionDisplay selectedAuction)
                dc.SelectedActiveAuctions.Add(selectedAuction);
        }
    }
}
