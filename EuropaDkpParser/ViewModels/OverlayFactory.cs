// -----------------------------------------------------------------------
// OverlayFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;

internal sealed class OverlayFactory : IOverlayFactory
{
    private readonly IOverlayViewFactory _viewFactory;

    internal OverlayFactory(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public IAttendanceOverlayViewModel CreateAttendanceOverlayViewModel(IDkpParserSettings settings)
        => new AttendanceOverlayViewModel(_viewFactory, settings);

    public IOverlayPositioningViewModel CreateOverlayPositioningViewModel(IDkpParserSettings settings)
        => new OverlayPositioningViewModel(_viewFactory, settings);
}

public interface IOverlayFactory
{
    IAttendanceOverlayViewModel CreateAttendanceOverlayViewModel(IDkpParserSettings settings);

    IOverlayPositioningViewModel CreateOverlayPositioningViewModel(IDkpParserSettings settings);
}
