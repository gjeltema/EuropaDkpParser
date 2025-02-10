// -----------------------------------------------------------------------
// IDialogView.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IDialogView
{
    bool? DialogResult { get; set; }

    double Height { get; set; }

    double Width { get; set; }

    bool? ShowDialog();
}

public interface IDialogViewFactory
{
    IDialogView CreateDialogView(IDialogViewModel dialogViewModel);
}
