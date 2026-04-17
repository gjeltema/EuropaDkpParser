// -----------------------------------------------------------------------
// WindowFactory.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using DkpParser.LiveTracking;

public sealed class WindowFactory : IWindowFactory
{
    private readonly IWindowViewFactory _viewFactory;

    internal WindowFactory(IWindowViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public ILiveLogTrackingViewModel CreateLiveLogTrackingViewModel(
        IDkpParserSettings settings,
        IEqLogTailFile eqLogTailFile,
        IRaidAttendanceCalc raidAttendance,
        IDialogFactory dialogFactory,
        IOverlayFactory overlayFactory,
        IWindowFactory windowFactory)
        => new LiveLogTrackingViewModel(_viewFactory, settings, eqLogTailFile, raidAttendance, dialogFactory, overlayFactory, windowFactory);

    public ISimpleBidTrackerViewModel CreateSimpleBidTrackerViewModel(IDkpParserSettings settings, IEqLogTailFile eqLogTailFile, IRaidAttendanceCalc raidAttendance)
        => new SimpleBidTrackerViewModel(_viewFactory, settings, eqLogTailFile, raidAttendance);
}

public interface IWindowFactory
{
    ILiveLogTrackingViewModel CreateLiveLogTrackingViewModel(
        IDkpParserSettings settings,
        IEqLogTailFile eqLogTailFile,
        IRaidAttendanceCalc raidAttendance,
        IDialogFactory dialogFactory,
        IOverlayFactory overlayFactory,
        IWindowFactory windowFactory);

    ISimpleBidTrackerViewModel CreateSimpleBidTrackerViewModel(IDkpParserSettings settings, IEqLogTailFile eqLogTailFile, IRaidAttendanceCalc raidAttendance);
}
