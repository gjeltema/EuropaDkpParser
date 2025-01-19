// -----------------------------------------------------------------------
// IOverlayView.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IOverlayView
{
    double Left { get; set; }

    double Top { get; set; }

    void Close();

    void DisableMove();

    void EnableMove();

    void Hide();

    void Show();
}
