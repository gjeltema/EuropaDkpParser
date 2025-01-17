// -----------------------------------------------------------------------
// OverlayView.xaml.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using System;
using System.Windows;
using System.Windows.Input;
using EuropaDkpParser.ViewModels;

public partial class OverlayView : Window, IOverlayView
{
    private readonly IOverlayViewModel _overlayViewModel;

    public OverlayView(IOverlayViewModel overlayViewModel)
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
        _overlayViewModel = overlayViewModel;
        DataContext = overlayViewModel;
        LocationChanged += HandleLocationChanged;
    }

    public void HideBorder()
    {
        WindowResizeChrome.ResizeBorderThickness = new Thickness(0);
        WindowBorder.BorderThickness = new Thickness(0, 0, 0, 0);
    }

    public void ShowBorder()
    {
        WindowResizeChrome.ResizeBorderThickness = new Thickness(8);
        WindowBorder.BorderThickness = new Thickness(1, 1, 1, 1);
    }

    private void HandleLocationChanged(object sender, EventArgs e)
    {
        _overlayViewModel.XPos = (int)Left;
        _overlayViewModel.YPos = (int)Top;
    }

    private void MouseEnterHandler(object sender, MouseEventArgs e)
    {
        ShowBorder();
    }

    private void MouseLeaveHandler(object sender, MouseEventArgs e)
    {
        HideBorder();
    }
}
