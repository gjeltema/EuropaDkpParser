// -----------------------------------------------------------------------
// DialogView.xaml.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows;
using EuropaDkpParser.ViewModels;

public partial class DialogView : Window, IDialogView
{
    public DialogView(IDialogViewModel dialogViewModel)
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
        DataContext = dialogViewModel;
    }
}
