// -----------------------------------------------------------------------
// SimpleMultilineDisplayDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal sealed class SimpleMultilineDisplayDialogViewModel : DialogViewModelBase, ISimpleMultilineDisplayDialogViewModel
{
    public SimpleMultilineDisplayDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    { }

    public int DisplayFontSize { get; set; } = 12;

    public string DisplayLines { get; set; }
}

public interface ISimpleMultilineDisplayDialogViewModel : IDialogViewModel
{
    int DisplayFontSize { get; set; }

    string DisplayLines { get; set; }
}
