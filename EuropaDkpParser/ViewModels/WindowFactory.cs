// -----------------------------------------------------------------------
// WindowFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;

public sealed class WindowFactory : IWindowFactory
{
    private readonly IWindowViewFactory _viewFactory;

    internal WindowFactory(IWindowViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public ILiveLogTrackingViewModel CreateLiveLogTrackingViewModel(
        IDkpParserSettings settings,
        IDialogFactory dialogFactory,
        IOverlayFactory overlayFactory,
        IWindowFactory windowFactory)
        => new LiveLogTrackingViewModel(_viewFactory, settings, dialogFactory, overlayFactory, windowFactory);

    public ISimpleBidTrackerViewModel CreateSimpleBidTrackerViewModel(IDkpParserSettings settings)
        => new SimpleBidTrackerViewModel(_viewFactory, settings);
}

public interface IWindowFactory
{
    ILiveLogTrackingViewModel CreateLiveLogTrackingViewModel(
        IDkpParserSettings settings,
        IDialogFactory dialogFactory,
        IOverlayFactory overlayFactory,
        IWindowFactory windowFactory);

    ISimpleBidTrackerViewModel CreateSimpleBidTrackerViewModel(IDkpParserSettings settings);
}
