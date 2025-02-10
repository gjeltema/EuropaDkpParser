// -----------------------------------------------------------------------
// IWindowView.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IWindowView
{
    event System.ComponentModel.CancelEventHandler Closing;

    void Show();
}

public interface ILiveLogTrackingWindow : IWindowView
{
}

public interface IWindowViewFactory
{
    ILiveLogTrackingWindow CreateLiveLogTrackingWindow(ILiveLogTrackingViewModel liveLogTrackingViewModel);
}
