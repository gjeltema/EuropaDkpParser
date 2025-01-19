// -----------------------------------------------------------------------
// OverlayPositioningViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;

internal sealed class OverlayPositioningViewModel : OverlayViewModelBase, IOverlayPositioningViewModel
{
    public OverlayPositioningViewModel(IOverlayViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        XPos = settings.OverlayLocationX;
        YPos = settings.OverlayLocationY;

        DisplayFontSize = settings.OverlayFontSize;
        DisplayColor = settings.OverlayFontColor;
    }

    public string DisplayColor { get; set; }

    public int DisplayFontSize { get; set; }

    public void ShowToMove()
    {
        CreateAndShowOverlay();
        EnableMove();
    }
}

public interface IOverlayPositioningViewModel : IOverlayViewModel
{
    string DisplayColor { get; set; }

    int DisplayFontSize { get; set; }

    void ShowToMove();
}
