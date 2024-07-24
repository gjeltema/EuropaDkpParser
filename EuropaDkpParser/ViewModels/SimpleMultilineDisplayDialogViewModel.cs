// -----------------------------------------------------------------------
// SimpleMultilineDisplayDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using EuropaDkpParser.Resources;

internal sealed class SimpleMultilineDisplayDialogViewModel : DialogViewModelBase, ISimpleMultilineDisplayDialogViewModel
{
    public SimpleMultilineDisplayDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("DkpspentEntriesRemovedDialogTitleText");
    }

    public string DisplayLines { get; set; }
}

public interface ISimpleMultilineDisplayDialogViewModel : IDialogViewModel
{
    string DisplayLines { get; set; }
}
