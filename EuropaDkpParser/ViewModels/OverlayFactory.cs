// -----------------------------------------------------------------------
// OverlayFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal sealed class OverlayFactory : IOverlayFactory
{
    private readonly IOverlayViewFactory _viewFactory;

    internal OverlayFactory(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }
}

public interface IOverlayFactory
{
}
