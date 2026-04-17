// -----------------------------------------------------------------------
// OverlayFactory.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using DkpParser.LiveTracking;

internal sealed class OverlayFactory : IOverlayFactory
{
    private readonly IOverlayViewFactory _viewFactory;

    internal OverlayFactory(IOverlayViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public IAttendanceOverlayViewModel CreateAttendanceOverlayViewModel(IDkpParserSettings settings, IAttendanceSnapshot attendanceSnapshot)
        => new AttendanceOverlayViewModel(_viewFactory, settings, attendanceSnapshot);

    public IAuctioneerOverlayViewModel CreateAuctioneerOverlayViewModel(IDkpParserSettings settings, IEqLogTailFile eqLogTailFile, IRaidAttendanceCalc raidAttendance)
        => new AuctioneerOverlayViewModel(_viewFactory, settings, eqLogTailFile, raidAttendance);

    public IReadyCheckOverlayViewModel CreateReadyCheckOverlayViewModel(IDkpParserSettings settings)
        => new ReadyCheckOverlayViewModel(_viewFactory, settings);

    public ISpellTrackerOverlayViewModel CreateSpellTrackerkOverlayViewModel(IDkpParserSettings settings)
        => new SpellTrackerOverlayViewModel(_viewFactory, settings);
}

public interface IOverlayFactory
{
    IAttendanceOverlayViewModel CreateAttendanceOverlayViewModel(IDkpParserSettings settings, IAttendanceSnapshot attendanceSnapshot);

    IAuctioneerOverlayViewModel CreateAuctioneerOverlayViewModel(IDkpParserSettings settings, IEqLogTailFile eqLogTailFile, IRaidAttendanceCalc raidAttendance);

    IReadyCheckOverlayViewModel CreateReadyCheckOverlayViewModel(IDkpParserSettings settings);

    ISpellTrackerOverlayViewModel CreateSpellTrackerkOverlayViewModel(IDkpParserSettings settings);
}
