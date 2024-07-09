// -----------------------------------------------------------------------
// CompletedDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Diagnostics;
using System.IO;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class CompletedDialogViewModel : DialogViewModelBase, ICompletedDialogViewModel
{
    internal CompletedDialogViewModel(IDialogViewFactory viewFactory, string logFilePath)
        : base(viewFactory)
    {
        Title = Strings.GetString("CompletedDialogTitleText");

        CompletionMessage = Strings.GetString("SuccessfulCompleteMessage");

        LogFilePath = logFilePath;
        if (!File.Exists(logFilePath))
            CompletionMessage = "No File Generated";

        OpenLogFileDirectoryCommand = new DelegateCommand(OpenLogFileDirectory);
    }

    public string CompletionMessage { get; }

    public string DkpSpentEntries { get; set; }

    public string LogFilePath { get; }

    public DelegateCommand OpenLogFileDirectoryCommand { get; }

    public bool ShowDkpSpentEntries
        => !string.IsNullOrWhiteSpace(DkpSpentEntries);

    private void OpenLogFileDirectory()
    {
        string directory = Path.GetDirectoryName(LogFilePath);
        Process.Start("explorer.exe", directory);
    }
}

public interface ICompletedDialogViewModel : IDialogViewModel
{
    string CompletionMessage { get; }

    string DkpSpentEntries { get; set; }

    string LogFilePath { get; }

    DelegateCommand OpenLogFileDirectoryCommand { get; }

    bool ShowDkpSpentEntries { get; }
}
