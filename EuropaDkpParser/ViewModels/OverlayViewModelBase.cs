// -----------------------------------------------------------------------
// OverlayViewModelBase.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal abstract class OverlayViewModelBase : EuropaViewModelBase, IOverlayViewModel
{
    protected IOverlayViewFactory _viewFactory;
    private bool _contentWasVisible;
    private Action _hideAction;
    private bool _windowHasBeenShown;

    protected OverlayViewModelBase(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public bool AllowResizing { get; set; }

    public bool ContentIsVisible { get; private set => SetProperty(ref field, value); }

    public int Height { get; set; }

    public bool IsInMoveMode { get; private set => SetProperty(ref field, value); }

    public int Width { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    protected IOverlayView OverlayView { get; private set; }

    public void Close()
    {
        HandleClose();
        OverlayView?.Close();
        OverlayView = null;
    }

    public void CreateAndShowOverlay()
    {
        OverlayView ??= _viewFactory.CreateOverlayView(this);

        OverlayView.Top = YPos;
        OverlayView.Left = XPos;
        OverlayView.Width = Width;
        OverlayView.Height = Height;
        ShowOverlay();
    }

    public void CreateShowAndHideOverlay()
    {
        CreateAndShowOverlay();
        ContentIsVisible = false;
    }

    public void DisableMove()
    {
        OverlayView?.DisableMove();
        IsInMoveMode = false;
        SaveLocation();
        ContentIsVisible = _contentWasVisible;
    }

    public void EnableMove()
    {
        _contentWasVisible = ContentIsVisible;
        ContentIsVisible = false;
        OverlayView?.EnableMove();
        IsInMoveMode = true;
    }

    public void HideOverlay()
    {
        if (OverlayView != null)
        {
            OverlayView.DisableMove();
            ContentIsVisible = false;
        }

        _hideAction?.Invoke();
    }

    public void SetHideHandler(Action hideAction)
        => _hideAction = hideAction;

    public void ShowOverlay()
    {
        ContentIsVisible = true;

        // Only call Show once.  After that, the visibility is controlled by the ContentIsVisible property.
        // This helps to prevent the Overlay from grabbing focus.
        if (!_windowHasBeenShown)
        {
            _windowHasBeenShown = true;
            OverlayView?.Show();
        }
    }

    protected virtual void HandleClose() { }

    protected virtual void SaveLocation() { }
}

public interface IOverlayViewModel
{
    bool AllowResizing { get; }

    bool ContentIsVisible { get; }

    int Height { get; set; }

    bool IsInMoveMode { get; }

    int Width { get; set; }

    int XPos { get; set; }

    int YPos { get; set; }

    void Close();

    void CreateAndShowOverlay();

    void CreateShowAndHideOverlay();

    void DisableMove();

    void EnableMove();

    void HideOverlay();

    void SetHideHandler(Action hideAction);

    void ShowOverlay();
}
