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

    public IAfkCheckerDialogViewModel CreateAfkCheckerDialogViewModel(RaidEntries raidEntries)
        => new AfkCheckerDialogViewModel(_viewFactory, raidEntries);

    public IAttendanceErrorDisplayDialogViewModel CreateAttendanceErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries)
        => new AttendanceErrorDisplayDialogViewModel(_viewFactory, settings, raidEntries);

    public IAttendanceEntryModiferDialogViewModel CreateAttendanceModifierDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries)
        => new AttendanceEntryModiferDialogViewModel(_viewFactory, settings, raidEntries);

    public ICompletedDialogViewModel CreateCompletedDialogViewModel(string logFilePath)
        => new CompletedDialogViewModel(_viewFactory, logFilePath);

    public IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries)
        => new DkpErrorDisplayDialogViewModel(_viewFactory, settings, raidEntries);

    public IDkpParseDialogViewModel CreateDkpParseDialogViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
        => new DkpParseDialogViewModel(settings, dialogFactory, _viewFactory);

    public IFileArchiveDialogViewModel CreateFileArchiveDialogViewModel(IDkpParserSettings settings)
        => new FileArchiveDialogViewModel(_viewFactory, settings);

    public IFinalSummaryDialogViewModel CreateFinalSummaryDialogViewModel(IDialogFactory dialogFactory, IDkpParserSettings settings, RaidEntries raidEntries, bool canUploadToServer)
        => new FinalSummaryDialogViewModel(_viewFactory, dialogFactory, settings, raidEntries, canUploadToServer);

    public IGeneralEqLogParserDialogViewModel CreateGeneralEqParserDialogViewModel(IDialogFactory dialogFactory, IDkpParserSettings settings)
        => new GeneralEqLogParserDialogViewModel(_viewFactory, dialogFactory, settings);

    public IParserDialogViewModel CreateParserDialogViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
        => new ParserDialogViewModel(settings, dialogFactory, _viewFactory);

    public IPossibleLinkdeadErrorDialogViewModel CreatePossibleLinkdeadErrorDialogViewModel(RaidEntries raidEntries)
        => new PossibleLinkdeadErrorDialogViewModel(_viewFactory, raidEntries);

    public IRaidUploadDialogViewModel CreateRaidUploadDialogViewModel(RaidEntries raidEntries, IDkpParserSettings settings)
        => new RaidUploadDialogViewModel(_viewFactory, raidEntries, settings);

    public ILogSelectionViewModel CreateSettingsViewDialogViewModel(IDkpParserSettings settings)
        => new LogSelectionViewModel(_viewFactory, settings);
}

public interface IDialogFactory
{
    IAfkCheckerDialogViewModel CreateAfkCheckerDialogViewModel(RaidEntries raidEntries);

    IAttendanceErrorDisplayDialogViewModel CreateAttendanceErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    IAttendanceEntryModiferDialogViewModel CreateAttendanceModifierDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    ICompletedDialogViewModel CreateCompletedDialogViewModel(string logFilePath);

    IDkpErrorDisplayDialogViewModel CreateDkpErrorDisplayDialogViewModel(IDkpParserSettings settings, RaidEntries raidEntries);

    IDkpParseDialogViewModel CreateDkpParseDialogViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory);

    IFileArchiveDialogViewModel CreateFileArchiveDialogViewModel(IDkpParserSettings settings);

    IFinalSummaryDialogViewModel CreateFinalSummaryDialogViewModel(IDialogFactory dialogFactory, IDkpParserSettings settings, RaidEntries raidEntries, bool canUploadToServer);

    IGeneralEqLogParserDialogViewModel CreateGeneralEqParserDialogViewModel(IDialogFactory dialogFactory, IDkpParserSettings settings);

    IParserDialogViewModel CreateParserDialogViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory);

    IPossibleLinkdeadErrorDialogViewModel CreatePossibleLinkdeadErrorDialogViewModel(RaidEntries raidEntries);

    IRaidUploadDialogViewModel CreateRaidUploadDialogViewModel(RaidEntries raidEntries, IDkpParserSettings settings);

    ILogSelectionViewModel CreateSettingsViewDialogViewModel(IDkpParserSettings settings);
}
