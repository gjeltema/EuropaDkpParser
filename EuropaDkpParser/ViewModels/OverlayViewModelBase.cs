// -----------------------------------------------------------------------
// OverlayViewModelBase.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal abstract class OverlayViewModelBase : EuropaViewModelBase, IOverlayViewModel
{
    protected IOverlayViewFactory _viewFactory;
    private bool _contentIsVisible;
    private Action _hideAction;
    private bool _windowHasBeenShown;

    protected OverlayViewModelBase(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public bool ContentIsVisible
    {
        get => _contentIsVisible;
        set => SetProperty(ref _contentIsVisible, value);
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
        ContentIsVisible = true;
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
            ContentIsVisible = false;
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
    bool ContentIsVisible { get; set; }

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
