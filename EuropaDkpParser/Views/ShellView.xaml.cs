// -----------------------------------------------------------------------
// ShellView.xaml.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows;
using EuropaDkpParser.ViewModels;

public partial class ShellView : Window
{
    public ShellView(IShellViewModel shellVM)
    {
        DataContext = shellVM;

        Closed += HandleClosed;

        InitializeComponent();
    }

    private void HandleClosed(object sender, EventArgs e)
    {
        if (DataContext is ShellViewModel shellVM)
        {
            shellVM.HandleClosed((int)Left, (int)Top);
        }
    }
}
