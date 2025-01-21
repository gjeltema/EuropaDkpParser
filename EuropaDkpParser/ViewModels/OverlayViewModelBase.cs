// -----------------------------------------------------------------------
// OverlayViewModelBase.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal abstract class OverlayViewModelBase : EuropaViewModelBase, IOverlayViewModel
{
    private const double OverlayControlOpacity = 0.7;
    protected IOverlayViewFactory _viewFactory;
    private double _controlOpacity;
    private Action _hideAction;
    private bool _windowHasBeenShown;

    protected OverlayViewModelBase(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public double ControlOpacity
    {
        get => _controlOpacity;
        set => SetProperty(ref _controlOpacity, value);
    }

    public bool PositionChanged { get; private set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    protected IOverlayView OverlayView { get; private set; }

    public void Close()
    {
        OverlayView?.Close();
        OverlayView = null;
    }

    public void CreateAndShowOverlay()
    {
        OverlayView ??= _viewFactory.CreateOverlayView(this);

        OverlayView.Top = YPos;
        OverlayView.Left = XPos;
        ControlOpacity = OverlayControlOpacity;
        ShowOverlay();
    }

    public void DisableMove()
        => OverlayView?.DisableMove();

    public void EnableMove()
        => OverlayView?.EnableMove();

    public void HideOverlay()
    {
        if (OverlayView != null)
        {
            OverlayView.DisableMove();
            //OverlayView?.Hide();
            ControlOpacity = 0.0;
        }

        _hideAction?.Invoke();
    }

    public void SetHideHandler(Action hideAction)
        => _hideAction = hideAction;

    public void ShowOverlay()
    {
        if (!_windowHasBeenShown)
        {
            _windowHasBeenShown = true;
            OverlayView?.Show();
        }
    }
}

public interface IOverlayViewModel
{
    public double ControlOpacity { get; set; }

    bool PositionChanged { get; }

    int XPos { get; set; }

    int YPos { get; set; }

    void Close();

    void CreateAndShowOverlay();

    void DisableMove();

    void EnableMove();

    void HideOverlay();

    void SetHideHandler(Action hideAction);

    void ShowOverlay();
}
