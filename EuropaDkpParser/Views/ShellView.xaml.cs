// -----------------------------------------------------------------------
// ShellView.xaml.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System.Windows;
using EuropaDkpParser.ViewModels;

public partial class ShellView : Window
{
    public ShellView(IShellViewModel shellVM)
    {
        DataContext = shellVM;

        InitializeComponent();
    }
}
