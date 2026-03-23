// -----------------------------------------------------------------------
// AuctioneerOverlay.xaml.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows.Controls;
using EuropaDkpParser.ViewModels;

public partial class AuctioneerOverlay : UserControl
{
    public AuctioneerOverlay()
    {
        InitializeComponent();

        ActiveAuctionListing.SelectionChanged += HandleSelectionChanged;
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not IAuctioneerOverlayViewModel dc)
            return;

        dc.SelectedActiveAuctions.Clear();

        foreach (object selectedItem in ActiveAuctionListing.SelectedItems)
        {
            if (selectedItem is LiveAuctionDisplay selectedAuction)
                dc.SelectedActiveAuctions.Add(selectedAuction);
        }
    }
}
