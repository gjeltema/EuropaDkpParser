// -----------------------------------------------------------------------
// CompletedDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Diagnostics;
using System.IO;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
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
        CopyMezBreaksToClipboardCommand = new DelegateCommand(CopyMezBreaksToClipboard, () => ShowMezBreaksButton)
            .ObservesProperty(() => ShowMezBreaksButton);
    }

    public string CompletionMessage { get; }

    public DelegateCommand CopyMezBreaksToClipboardCommand { get; }

    public string LogFilePath { get; }

    public string MezBreaksSummary
    {
        get;
        set
        {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(ShowMezBreaksButton));
        }
    }

    public DelegateCommand OpenLogFileDirectoryCommand { get; }

    public bool ShowDkpSpentEntries
        => !string.IsNullOrWhiteSpace(SummaryDisplay);

    public bool ShowMezBreaksButton
        => !string.IsNullOrWhiteSpace(MezBreaksSummary);

    public string SummaryDisplay { get; set; }

    private void CopyMezBreaksToClipboard()
    {
        if (string.IsNullOrWhiteSpace(MezBreaksSummary))
            return;

        Clip.Copy(MezBreaksSummary);
    }

    private void OpenLogFileDirectory()
    {
        string directory = Path.GetDirectoryName(LogFilePath);
        Process.Start("explorer.exe", directory);
    }
}

public interface ICompletedDialogViewModel : IDialogViewModel
{
    string CompletionMessage { get; }

    DelegateCommand CopyMezBreaksToClipboardCommand { get; }

    string LogFilePath { get; }

    string MezBreaksSummary { get; set; }

    DelegateCommand OpenLogFileDirectoryCommand { get; }

    bool ShowDkpSpentEntries { get; }

    bool ShowMezBreaksButton { get; }

    string SummaryDisplay { get; set; }
}
