// -----------------------------------------------------------------------
// LiveLogTrackingView.xaml.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows;
using EuropaDkpParser.ViewModels;

public partial class LiveLogTrackingView : Window
{
    public LiveLogTrackingView(ILiveLogTrackingViewModel liveLogTrackingViewModel)
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
        DataContext = liveLogTrackingViewModel;
    }

    private void ClosedHandler(object sender, EventArgs e)
    {
        ILiveLogTrackingViewModel dc = DataContext as ILiveLogTrackingViewModel;
        dc?.HandleClosed();
    }
}
