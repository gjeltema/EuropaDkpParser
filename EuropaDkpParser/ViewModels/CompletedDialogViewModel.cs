// -----------------------------------------------------------------------
// CompletedDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using Prism.Commands;

internal sealed class CompletedDialogViewModel : DialogViewModelBase, ICompletedDialogViewModel
{
    internal CompletedDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
    }

    public string LogFilePath { get; }
    public string CompletionMessage { get; }
    public DelegateCommand OpenLogFileDirectoryCommand { get; }
}

public interface ICompletedDialogViewModel : IDialogViewModel
{
    string LogFilePath { get; }

    string CompletionMessage { get; }

    DelegateCommand OpenLogFileDirectoryCommand { get; }
}
