// -----------------------------------------------------------------------
// WindowViewFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views;

using EuropaDkpParser.ViewModels;

public sealed class WindowViewFactory : IWindowViewFactory
{
    public ILiveLogTrackingWindow CreateLiveLogTrackingWindow(ILiveLogTrackingViewModel liveLogTrackingViewModel)
        => new LiveLogTrackingView(liveLogTrackingViewModel);

    public ISimpleBidTrackerWindow CreateSimpleBidTrackerWindow(ISimpleBidTrackerViewModel simpleBidTrackerViewModel)
        => new SimpleBidTrackerView(simpleBidTrackerViewModel);
}
