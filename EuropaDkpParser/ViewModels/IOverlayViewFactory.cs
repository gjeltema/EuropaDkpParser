// -----------------------------------------------------------------------
// IOverlayViewFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IOverlayViewFactory
{
    IOverlayView CreateOverlayView(IOverlayViewModel overlayViewModel);
}
