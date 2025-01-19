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
    private bool _enableMove = false;

    public OverlayView(IOverlayViewModel overlayViewModel)
    {
        InitializeComponent();

        DataContext = overlayViewModel;

        Owner = Application.Current.MainWindow;
        _overlayViewModel = overlayViewModel;

        LocationChanged += HandleLocationChanged;
    }

    public void DisableMove()
    {
        //WindowResizeChrome.ResizeBorderThickness = new Thickness(0);
        WindowBorder.BorderThickness = new Thickness(0, 0, 0, 0);
        _enableMove = false;
        Background.Opacity = 0;
    }

    public void EnableMove()
    {
        //WindowResizeChrome.ResizeBorderThickness = new Thickness(8);
        WindowBorder.BorderThickness = new Thickness(1, 1, 1, 1);
        _enableMove = true;
        Background.Opacity = 0.2;
    }

    private void HandleLocationChanged(object sender, EventArgs e)
    {
        _overlayViewModel.XPos = (int)Left;
        _overlayViewModel.YPos = (int)Top;
    }

    private void MouseDownHandler(object sender, MouseEventArgs e)
    {
        if (_enableMove && e.LeftButton.HasFlag(MouseButtonState.Pressed))
            DragMove();
    }
}
