// -----------------------------------------------------------------------
// OverlayViewModelBase.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal abstract class OverlayViewModelBase : EuropaViewModelBase, IOverlayViewModel
{
    protected IOverlayViewFactory _viewFactory;
    private int _xPos;
    private int _yPos;

    protected OverlayViewModelBase(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public bool PositionChanged { get; private set; }

    public int XPos
    {
        get => _xPos;
        set
        {
            if (_xPos != value)
            {
                _xPos = value;
                PositionChanged = true;
            }
        }
    }

    public int YPos
    {
        get => _yPos;
        set
        {
            if (_yPos != value)
            {
                _yPos = value;
                PositionChanged = true;
            }
        }
    }

    protected IOverlayView OverlayView { get; private set; }

    public void CreateAndShowOverlay()
    {
        OverlayView = _viewFactory.CreateOverlayView(this);
        OverlayView.Top = YPos;
        OverlayView.Left = XPos;
        Show();
    }

    public void DisableMove()
        => OverlayView?.HideBorder();

    public void EnableMove()
        => OverlayView?.ShowBorder();

    public void HideOverlay()
        => OverlayView?.Hide();

    public void Show()
        => OverlayView?.Show();
}

public interface IOverlayViewModel
{
    bool PositionChanged { get; }

    int XPos { get; set; }

    int YPos { get; set; }

    void CreateAndShowOverlay();

    void DisableMove();

    void EnableMove();

    void HideOverlay();

    void Show();
}
