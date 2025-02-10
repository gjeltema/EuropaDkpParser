// -----------------------------------------------------------------------
// IWindowViewFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IWindowViewFactory
{
    ILiveLogTrackingWindow CreateLiveLogTrackingWindow(ILiveLogTrackingViewModel liveLogTrackingViewModel);
}
