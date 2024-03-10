// -----------------------------------------------------------------------
// IDialogView.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IDialogView
{
    bool? DialogResult { get; set; }

    bool? ShowDialog();
}
