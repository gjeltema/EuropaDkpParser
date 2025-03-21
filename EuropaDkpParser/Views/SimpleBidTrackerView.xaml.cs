﻿// -----------------------------------------------------------------------
// SimpleBidTrackerView.xaml.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows;
using System.Windows.Controls;
using EuropaDkpParser.ViewModels;

public partial class SimpleBidTrackerView : Window, ISimpleBidTrackerWindow
{
    public SimpleBidTrackerView(ISimpleBidTrackerViewModel simpleBidTrackerViewModel)
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
        DataContext = simpleBidTrackerViewModel;

        ActiveAuctionListing.SelectionChanged += HandleSelectionChanged;
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ISimpleBidTrackerViewModel dc)
            return;

        dc.SelectedActiveAuctions.Clear();

        foreach (object selectedItem in ActiveAuctionListing.SelectedItems)
        {
            if (selectedItem is LiveAuctionDisplay selectedAuction)
                dc.SelectedActiveAuctions.Add(selectedAuction);
        }
    }
}
