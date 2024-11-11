// -----------------------------------------------------------------------
// LiveLogTrackingView.xaml.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views
{
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
    }
}
