// -----------------------------------------------------------------------
// IOverlayView.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IOverlayView
{
    double Height { get; set; }

    double Left { get; set; }

    double Top { get; set; }

    double Width { get; set; }

    void Close();

    void DisableMove();

    void EnableMove();

    void Hide();

    void Show();
}

public interface IOverlayViewFactory
{
    IOverlayView CreateOverlayView(IOverlayViewModel overlayViewModel);
}
