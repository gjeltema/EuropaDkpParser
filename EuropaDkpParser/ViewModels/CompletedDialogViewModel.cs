﻿// -----------------------------------------------------------------------
// CompletedDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Diagnostics;
using System.IO;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class CompletedDialogViewModel : DialogViewModelBase, ICompletedDialogViewModel
{
    internal CompletedDialogViewModel(IDialogViewFactory viewFactory, string logFilePath, RaidUploadResults uploadResults)
        : base(viewFactory)
    {
        Title = Strings.GetString("CompletedDialogTitleText");

        string completionMessage = Strings.GetString("SuccessfulCompleteMessage"); //** Need failure message too

        LogFilePath = logFilePath;
        CompletionMessage = completionMessage;

        OpenLogFileDirectoryCommand = new DelegateCommand(OpenLogFileDirectory);
    }

    public string CompletionMessage { get; }

    public string LogFilePath { get; }

    public DelegateCommand OpenLogFileDirectoryCommand { get; }

    private void OpenLogFileDirectory()
    {
        string directory = Path.GetDirectoryName(LogFilePath);
        Process.Start("explorer.exe", directory);
    }
}

public interface ICompletedDialogViewModel : IDialogViewModel
{
    string CompletionMessage { get; }

    string LogFilePath { get; }

    DelegateCommand OpenLogFileDirectoryCommand { get; }
}
