// -----------------------------------------------------------------------
// OverlayViewModelBase.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal abstract class OverlayViewModelBase : EuropaViewModelBase, IOverlayViewModel
{
    protected IOverlayViewFactory _viewFactory;
    private Action _hideAction;
    private bool _moveIsEnabled = false;
    private bool _windowHasBeenShown;

    protected OverlayViewModelBase(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public bool AllowResizing { get; set; }

    public bool ContentIsVisible { get; set => SetProperty(ref field, value); }

    public bool DisplayControls
        => _moveIsEnabled || ShouldDisplayControls();

    public int Height { get; set; }

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
        ContentIsVisible = true;
        ShowOverlay();
    }

    public void DisableMove()
    {
        _moveIsEnabled = false;
        OverlayView?.DisableMove();
        RefreshDisplayControls();
        SaveLocation();
    }

    public void EnableMove()
    {
        _moveIsEnabled = true;
        OverlayView?.EnableMove();
        RefreshDisplayControls();
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
        // Only call Show once.  After that, the visibility is controlled by the ContentIsVisible property.
        // This helps to prevent the Overlay from grabbing focus.
        if (!_windowHasBeenShown)
        {
            _windowHasBeenShown = true;
            OverlayView?.Show();
        }
    }

    protected virtual void HandleClose() { }

    protected void RefreshDisplayControls()
        => RaisePropertyChanged(nameof(DisplayControls));

    protected virtual void SaveLocation() { }

    protected virtual bool ShouldDisplayControls()
        => true;
}

public interface IOverlayViewModel
{
    bool AllowResizing { get; }

    bool ContentIsVisible { get; set; }

    bool DisplayControls { get; }

    int Height { get; set; }

    int Width { get; set; }

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
