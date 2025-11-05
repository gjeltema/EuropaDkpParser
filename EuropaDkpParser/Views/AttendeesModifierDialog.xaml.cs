// -----------------------------------------------------------------------
// AttendeesModifierDialog.xaml.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows.Controls;
using DkpParser;
using EuropaDkpParser.ViewModels;

public partial class AttendeesModifierDialog : UserControl
{
    public AttendeesModifierDialog()
    {
        InitializeComponent();

        AllAttendancesListing.SelectionChanged += HandleSelectionChanged;
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not IAttendeesModifierDialogViewModel dc)
            return;

        dc.SelectedAttendances.Clear();

        foreach (object selectedItem in AllAttendancesListing.SelectedItems)
        {
            if (selectedItem is AttendanceEntry selectedAuction)
            {
                dc.SelectedAttendances.Add(selectedAuction);
                dc.SignalSelectedAttendancesChanged();
            }
        }
    }
}
