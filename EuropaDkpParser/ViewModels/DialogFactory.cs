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

    public ICompletedDialogViewModel CreateCompletedDialogViewModel(string logFilePath)
        => new CompletedDialogViewModel(_viewFactory, logFilePath);

    public IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries)
        => new DkpErrorDisplayDialogViewModel(_viewFactory, settings, raidEntries);

    public IFileArchiveDialogViewModel CreateFileArchiveDialogViewModel(IDkpParserSettings settings)
        => new FileArchiveDialogViewModel(_viewFactory, settings);

    public IFinalSummaryDialogViewModel CreateFinalSummaryDialogViewModel(IDialogFactory dialogFactory, RaidEntries raidEntries, bool canUploadToServer)
        => new FinalSummaryDialogViewModel(_viewFactory, dialogFactory, raidEntries, canUploadToServer);

    public IGeneralEqLogParserDialogViewModel CreateGeneralEqParserDialogViewModel() 
        => new GeneralEqLogParserDialogViewModel(_viewFactory);

    public IPossibleLinkdeadErrorDialogViewModel CreatePossibleLinkdeadErrorDialogViewModel(RaidEntries raidEntries)
        => new PossibleLinkdeadErrorDialogViewModel(_viewFactory, raidEntries);

    public IRaidUploadDialogViewModel CreateRaidUploadDialogViewModel(RaidEntries raidEntries, IDkpParserSettings settings)
        => new RaidUploadDialogViewModel(_viewFactory, raidEntries, settings);

    public ILogSelectionViewModel CreateSettingsViewDialogViewModel(IDkpParserSettings settings)
        => new LogSelectionViewModel(_viewFactory, settings);
}

public interface IDialogFactory
{
    IAttendanceErrorDisplayDialogViewModel CreateAttendanceErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    IAttendanceEntryModiferDialogViewModel CreateAttendanceModifierDialogViewModel(RaidEntries raidEntries);

    ICompletedDialogViewModel CreateCompletedDialogViewModel(string logFilePath);

    IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    IFileArchiveDialogViewModel CreateFileArchiveDialogViewModel(IDkpParserSettings settings);

    IFinalSummaryDialogViewModel CreateFinalSummaryDialogViewModel(IDialogFactory dialogFactory, RaidEntries raidEntries, bool canUploadToServer);

    IGeneralEqLogParserDialogViewModel CreateGeneralEqParserDialogViewModel();

    IPossibleLinkdeadErrorDialogViewModel CreatePossibleLinkdeadErrorDialogViewModel(RaidEntries raidEntries);

    IRaidUploadDialogViewModel CreateRaidUploadDialogViewModel(RaidEntries raidEntries, IDkpParserSettings settings);

    ILogSelectionViewModel CreateSettingsViewDialogViewModel(IDkpParserSettings settings);
}
