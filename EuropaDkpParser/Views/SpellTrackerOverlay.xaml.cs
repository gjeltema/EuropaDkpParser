// -----------------------------------------------------------------------
// SpellTrackerOverlay.xaml.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows;
using System.Windows.Controls;
using EuropaDkpParser.ViewModels;

public partial class SpellTrackerOverlay : UserControl
{
    public SpellTrackerOverlay()
    {
        InitializeComponent();
    }

    private void HandleDoubleClick(object sender, RoutedEventArgs e)
    {
        FrameworkElement fe = e?.Source as FrameworkElement;
        SpellTrackerItemViewModel vm = fe?.DataContext as SpellTrackerItemViewModel;
        vm?.CancelSpell();
    }
}
