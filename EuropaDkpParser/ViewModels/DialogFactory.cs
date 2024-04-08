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

    public IAttendanceErrorDisplayDialogViewModel CreateAttendanceErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries)
        => new AttendanceErrorDisplayDialogViewModel(_viewFactory, settings, raidEntries);

    public IAttendanceEntryModiferDialogViewModel CreateAttendanceModifierDialogViewModel(RaidEntries raidEntries)
        => new AttendanceEntryModiferDialogViewModel(_viewFactory, raidEntries);

    public ICompletedDialogViewModel CreateCompletedDialogViewModel(string logFilePath, RaidUploadResults uploadResults)
        => new CompletedDialogViewModel(_viewFactory, logFilePath, uploadResults);

    public IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries)
        => new DkpErrorDisplayDialogViewModel(_viewFactory, settings, raidEntries);

    public IFileArchiveDialogViewModel CreateFileArchiveDialogViewModel(IDkpParserSettings settings)
        => new FileArchiveDialogViewModel(_viewFactory, settings);

    public IFinalSummaryDialogViewModel CreateFinalSummaryDialogViewModel(IDialogFactory dialogFactory, RaidEntries raidEntries)
        => new FinalSummaryDialogViewModel(_viewFactory, dialogFactory, raidEntries);

    public ILogSelectionViewModel CreateSettingsViewDialogViewModel(IDkpParserSettings settings)
        => new LogSelectionViewModel(_viewFactory, settings);
}

public interface IDialogFactory
{
    IAttendanceErrorDisplayDialogViewModel CreateAttendanceErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    IAttendanceEntryModiferDialogViewModel CreateAttendanceModifierDialogViewModel(RaidEntries raidEntries);

    ICompletedDialogViewModel CreateCompletedDialogViewModel(string logFilePath, RaidUploadResults uploadResults);

    IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    IFileArchiveDialogViewModel CreateFileArchiveDialogViewModel(IDkpParserSettings settings);

    IFinalSummaryDialogViewModel CreateFinalSummaryDialogViewModel(IDialogFactory dialogFactory, RaidEntries raidEntries);

    ILogSelectionViewModel CreateSettingsViewDialogViewModel(IDkpParserSettings settings);
}
