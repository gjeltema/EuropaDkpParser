// -----------------------------------------------------------------------
// OverlayViewModelBase.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal abstract class OverlayViewModelBase : EuropaViewModelBase, IOverlayViewModel
{
    protected IOverlayViewFactory _viewFactory;

    protected OverlayViewModelBase(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
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
        OverlayView = _viewFactory.CreateOverlayView(this);
        OverlayView.Top = YPos;
        OverlayView.Left = XPos;
        ShowOverlay();
    }

    public void DisableMove()
        => OverlayView?.DisableMove();

    public void EnableMove()
        => OverlayView?.EnableMove();

    public void HideOverlay()
    {
        OverlayView?.DisableMove();
        OverlayView?.Hide();
    }

    public void ShowOverlay()
        => OverlayView?.Show();
}

public interface IOverlayViewModel
{
    bool PositionChanged { get; }

    int XPos { get; set; }

    int YPos { get; set; }

    void Close();

    void CreateAndShowOverlay();

    void DisableMove();

    void EnableMove();

    void HideOverlay();

    void ShowOverlay();
}
