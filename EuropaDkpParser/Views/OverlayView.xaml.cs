// -----------------------------------------------------------------------
// OverlayView.xaml.cs Copyright 2026 Craig Gjeltema
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
        SizeChanged += HandleSizeChanged;
    }

    public void DisableMove()
    {
        WindowBorder.BorderThickness = new Thickness(0, 0, 0, 0);
        _enableMove = false;
        Background.Opacity = 0;
        if (_overlayViewModel.AllowResizing)
            ResizeMode = ResizeMode.NoResize;
    }

    public void EnableMove()
    {
        WindowBorder.BorderThickness = new Thickness(1, 1, 1, 1);
        _enableMove = true;
        Background.Opacity = 0.2;
        if (_overlayViewModel.AllowResizing)
            ResizeMode = ResizeMode.CanResizeWithGrip;
    }

    private void HandleLocationChanged(object sender, EventArgs e)
    {
        _overlayViewModel.XPos = (int)Left;
        _overlayViewModel.YPos = (int)Top;
    }

    private void HandleSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _overlayViewModel.Height = (int)Height;
        _overlayViewModel.Width = (int)Width;
    }

    private void MouseDownHandler(object sender, MouseEventArgs e)
    {
        if (_enableMove && e.LeftButton.HasFlag(MouseButtonState.Pressed))
            DragMove();
    }
}
