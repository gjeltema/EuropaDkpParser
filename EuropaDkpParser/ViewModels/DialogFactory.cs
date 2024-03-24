// -----------------------------------------------------------------------
// DialogFactory.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;

internal sealed class DialogFactory : IDialogFactory
{
    private readonly IDialogViewFactory _viewFactory;

    internal DialogFactory(IDialogViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public IAttendanceErrorDisplayDialogViewModel CreateAttendanceErrorDisplayDialog(IDkpParserSettings settings, RaidEntries raidEntries)
        => new AttendanceErrorDisplayDialogViewModel(_viewFactory, settings, raidEntries);

    public ICompletedDialogViewModel CreateCompletedDialog(string logFilePath, string completionMessage)
        => new CompletedDialogViewModel(_viewFactory, logFilePath, completionMessage);

    public IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries)
        => new DkpErrorDisplayDialogViewModel(_viewFactory, settings, raidEntries);

    public IFinalSummaryDialogViewModel CreateFinalSummaryDialog(RaidEntries raidEntries)
        => new FinalSummaryDialogViewModel(_viewFactory, raidEntries);

    public ILogSelectionViewModel CreateSettingsViewDialog(IDkpParserSettings settings)
        => new LogSelectionViewModel(_viewFactory, settings);
}

public interface IDialogFactory
{
    IAttendanceErrorDisplayDialogViewModel CreateAttendanceErrorDisplayDialog(IDkpParserSettings settings, RaidEntries raidEntries);

    ICompletedDialogViewModel CreateCompletedDialog(string logFilePath, string completionMessage);

    IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    IFinalSummaryDialogViewModel CreateFinalSummaryDialog(RaidEntries raidEntries);

    ILogSelectionViewModel CreateSettingsViewDialog(IDkpParserSettings settings);
}
