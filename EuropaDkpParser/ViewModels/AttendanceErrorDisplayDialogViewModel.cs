// -----------------------------------------------------------------------
// AttendanceErrorDisplayDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using Prism.Commands;

internal sealed class AttendanceErrorDisplayDialogViewModel : DialogViewModelBase, IAttendanceErrorDisplayDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<AttendanceEntry> _allAttendances;
    private AttendanceEntry _currentEntry;
    private ICollection<AttendanceEntry> _errorAttendances;
    private string _errorLogEntry;
    private string _errorMessageText;
    private bool _isBossMobTypoError;
    private bool _isDuplicateError;
    private string _nextButtonText;
    private string _selectedBossName;
    private AttendanceEntry _selectedErrorEntry;

    internal AttendanceErrorDisplayDialogViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings, RaidEntries raidEntries)
        : base(viewFactory)
    {
        _settings = settings;
        _raidEntries = raidEntries;

        ApprovedBossNames = _settings.BossMobs;
        string firstBossName = ApprovedBossNames.FirstOrDefault();
        if (!string.IsNullOrEmpty(firstBossName))
        {
            SelectedBossName = firstBossName;
        }

        MoveToNextErrorCommand = new DelegateCommand(AdvanceToNextError);
        RemoveDuplicateErrorEntryCommand = new DelegateCommand(RemoveDuplicateErrorEntry, () => _currentEntry?.PossibleError == PossibleError.DuplicateRaidEntry);
        ChangeBossMobNameCommand = new DelegateCommand(ChangeBossMobName, () => _currentEntry?.PossibleError == PossibleError.BossMobNameTypo);

        AdvanceToNextError();
    }

    public ICollection<AttendanceEntry> AllAttendances
    {
        get => _allAttendances;
        set => SetProperty(ref _allAttendances, value);
    }

    public ICollection<string> ApprovedBossNames { get; }

    public DelegateCommand ChangeBossMobNameCommand { get; }

    public ICollection<AttendanceEntry> ErrorAttendances
    {
        get => _errorAttendances;
        set => SetProperty(ref _errorAttendances, value);
    }

    public string ErrorLogEntry
    {
        get => _errorLogEntry;
        set => SetProperty(ref _errorLogEntry, value);
    }

    public string ErrorMessageText
    {
        get => _errorMessageText;
        set => SetProperty(ref _errorMessageText, value);
    }

    public DelegateCommand FinishReviewingErrorsCommand { get; }

    public bool IsBossMobTypoError
    {
        get => _isBossMobTypoError;
        private set => SetProperty(ref _isBossMobTypoError, value);
    }

    public bool IsDuplicateError
    {
        get => _isDuplicateError;
        private set => SetProperty(ref _isDuplicateError, value);
    }

    public DelegateCommand MoveToNextErrorCommand { get; }

    public string NextButtonText
    {
        get => _nextButtonText;
        private set => SetProperty(ref _nextButtonText, value);
    }

    public DelegateCommand RemoveDuplicateErrorEntryCommand { get; }

    public string SelectedBossName
    {
        get => _selectedBossName;
        set => SetProperty(ref _selectedBossName, value);
    }

    public AttendanceEntry SelectedErrorEntry
    {
        get => _selectedErrorEntry;
        set => SetProperty(ref _selectedErrorEntry, value);
    }

    private void AdvanceToNextError()
    {
        if (ErrorAttendances?.Count > 0)
        {
            foreach (AttendanceEntry errorAttendance in ErrorAttendances)
                errorAttendance.PossibleError = PossibleError.None;
        }

        if (_currentEntry != null)
            _currentEntry.PossibleError = PossibleError.None;

        SetNextButtonText();

        _currentEntry = _raidEntries.AttendanceEntries.FirstOrDefault(x => x.PossibleError != PossibleError.None);
        if (_currentEntry == null)
        {
            CloseOk();
            return;
        }

        AllAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
        RemoveDuplicateErrorEntryCommand.RaiseCanExecuteChanged();
        ChangeBossMobNameCommand.RaiseCanExecuteChanged();

        if (_currentEntry.PossibleError == PossibleError.DuplicateRaidEntry)
        {
            IsBossMobTypoError = false;
            IsDuplicateError = true;

            ErrorMessageText = "Possible duplicate entries:";
            ErrorAttendances = _raidEntries.AttendanceEntries
                .Where(x => x.RaidName.Equals(_currentEntry.RaidName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else if (_currentEntry.PossibleError == PossibleError.BossMobNameTypo)
        {
            IsBossMobTypoError = true;
            IsDuplicateError = false;

            ErrorMessageText = "Possible boss name typo:";
            ErrorAttendances = [_currentEntry];

            for (int i = 8; i > 0; i--)
            {
                if (_currentEntry.RaidName.Length < i)
                    continue;

                string startOfBossName = _currentEntry.RaidName[..i];
                string approvedBossName = _settings.BossMobs.FirstOrDefault(x => x.StartsWith(startOfBossName));
                if (approvedBossName != null)
                {
                    SelectedBossName = approvedBossName;
                    break;
                }
            }
        }
    }

    private void ChangeBossMobName()
    {
        _currentEntry.RaidName = SelectedBossName;
        ErrorAttendances = [_currentEntry];
    }

    private void RemoveDuplicateErrorEntry()
    {
        _raidEntries.AttendanceEntries.Remove(SelectedErrorEntry);
        ErrorAttendances = _raidEntries.AttendanceEntries
            .Where(x => x.RaidName.Equals(_currentEntry.RaidName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        AllAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
        SetNextButtonText();
    }

    private void SetNextButtonText()
    {
        int numberOfErrors = _raidEntries.AttendanceEntries.Count(x => x.PossibleError != PossibleError.None);
        NextButtonText = numberOfErrors > 1 ? "Next" : "Finish";
    }
}

public interface IAttendanceErrorDisplayDialogViewModel : IDialogViewModel
{
    ICollection<AttendanceEntry> AllAttendances { get; }

    ICollection<string> ApprovedBossNames { get; }

    DelegateCommand ChangeBossMobNameCommand { get; }

    ICollection<AttendanceEntry> ErrorAttendances { get; }

    string ErrorLogEntry { get; }

    string ErrorMessageText { get; }

    DelegateCommand FinishReviewingErrorsCommand { get; }

    bool IsBossMobTypoError { get; }

    bool IsDuplicateError { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string NextButtonText { get; }

    DelegateCommand RemoveDuplicateErrorEntryCommand { get; }

    string SelectedBossName { get; set; }

    AttendanceEntry SelectedErrorEntry { get; set; }
}
