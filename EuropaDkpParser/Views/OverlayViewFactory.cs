// -----------------------------------------------------------------------
// OverlayViewFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using EuropaDkpParser.ViewModels;

public sealed class OverlayViewFactory : IOverlayViewFactory
{
    public IOverlayView CreateOverlayView(IOverlayViewModel overlayViewModel)
        => new OverlayView(overlayViewModel);
}
