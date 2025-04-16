// -----------------------------------------------------------------------
// AttendanceErrorDisplayDialogViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Linq;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class AttendanceErrorDisplayDialogViewModel : DialogViewModelBase, IAttendanceErrorDisplayDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<AttendanceEntry> _allAttendances;
    private ICollection<string> _allCharactersInAccount;
    private AttendanceEntry _currentEntry;
    private ICollection<AttendanceEntry> _errorAttendances;
    private string _errorLogEntry;
    private string _errorMessageText;
    private string _firstMultipleCharacter;
    private bool _isBossMobTypoError;
    private bool _isDuplicateError;
    private bool _isMultipleCharsFromSameAccountError;
    private bool _isNoZoneNameError;
    private bool _isRaidNameTooShortError;
    private string _nextButtonText;
    private string _raidNameText;
    private string _secondMultipleCharacter;
    private string _selectedBossName;
    private AttendanceEntry _selectedErrorEntry;
    private string _selectedZoneName;
    private bool _showCharacterRemovedFromAttendanceMessage;

    internal AttendanceErrorDisplayDialogViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("AttendanceErrorDisplayDialogTitleText");
        Width = 780;

        _settings = settings;
        _raidEntries = raidEntries;

        ZoneNames = _settings.RaidValue.AllValidRaidZoneNames;

        ApprovedBossNames = _settings.RaidValue.AllBossMobNames.Order().ToList();
        string firstBossName = ApprovedBossNames.FirstOrDefault();
        if (!string.IsNullOrEmpty(firstBossName))
        {
            SelectedBossName = firstBossName;
        }

        MoveToNextErrorCommand = new DelegateCommand(AdvanceToNextError);
        RemoveDuplicateErrorEntryCommand = new DelegateCommand(RemoveDuplicateErrorEntry, () => _currentEntry?.PossibleError == PossibleError.DuplicateRaidEntry && SelectedErrorEntry != null)
            .ObservesProperty(() => SelectedErrorEntry);
        ChangeBossMobNameCommand = new DelegateCommand(ChangeBossMobName);
        UpdateZoneNameCommand = new DelegateCommand(UpdateZoneName, () => !string.IsNullOrWhiteSpace(SelectedZoneName) && SelectedZoneName != _currentEntry?.ZoneName)
            .ObservesProperty(() => SelectedZoneName);
        UpdateSelectedRaidNameCommand = new DelegateCommand(UpdateSelectedRaidName, () => !string.IsNullOrWhiteSpace(RaidNameText) && SelectedErrorEntry != null)
            .ObservesProperty(() => RaidNameText).ObservesProperty(() => SelectedErrorEntry);
        UpdateRaidNameCommand = new DelegateCommand(UpdateRaidName, () => !string.IsNullOrWhiteSpace(RaidNameText))
            .ObservesProperty(() => RaidNameText);
        RemoveFirstMultipleCharacterCommand = new DelegateCommand(() => RemoveMultipleCharacter(FirstMultipleCharacter));
        RemoveSecondMultipleCharacterCommand = new DelegateCommand(() => RemoveMultipleCharacter(SecondMultipleCharacter));

        AdvanceToNextError();
    }

    public ICollection<AttendanceEntry> AllAttendances
    {
        get => _allAttendances;
        set => SetProperty(ref _allAttendances, value);
    }

    public ICollection<string> AllCharactersInAccount
    {
        get => _allCharactersInAccount;
        set => SetProperty(ref _allCharactersInAccount, value);
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

    public string FirstMultipleCharacter
    {
        get => _firstMultipleCharacter;
        set => SetProperty(ref _firstMultipleCharacter, value);
    }

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

    public bool IsMultipleCharsFromSameAccountError
    {
        get => _isMultipleCharsFromSameAccountError;
        private set => SetProperty(ref _isMultipleCharsFromSameAccountError, value);
    }

    public bool IsNoZoneNameError
    {
        get => _isNoZoneNameError;
        private set => SetProperty(ref _isNoZoneNameError, value);
    }

    public bool IsRaidNameTooShortError
    {
        get => _isRaidNameTooShortError;
        private set => SetProperty(ref _isRaidNameTooShortError, value);
    }

    public DelegateCommand MoveToNextErrorCommand { get; }

    public string NextButtonText
    {
        get => _nextButtonText;
        private set => SetProperty(ref _nextButtonText, value);
    }

    public string RaidNameText
    {
        get => _raidNameText;
        set => SetProperty(ref _raidNameText, value);
    }

    public DelegateCommand RemoveDuplicateErrorEntryCommand { get; }

    public DelegateCommand RemoveFirstMultipleCharacterCommand { get; }

    public DelegateCommand RemoveSecondMultipleCharacterCommand { get; }

    public string SecondMultipleCharacter
    {
        get => _secondMultipleCharacter;
        set => SetProperty(ref _secondMultipleCharacter, value);
    }

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

    public string SelectedZoneName
    {
        get => _selectedZoneName;
        set => SetProperty(ref _selectedZoneName, value);
    }

    public bool ShowCharacterRemovedFromAttendanceMessage
    {
        get => _showCharacterRemovedFromAttendanceMessage;
        private set => SetProperty(ref _showCharacterRemovedFromAttendanceMessage, value);
    }

    public DelegateCommand UpdateRaidNameCommand { get; }

    public DelegateCommand UpdateSelectedRaidNameCommand { get; }

    public DelegateCommand UpdateZoneNameCommand { get; }

    public ICollection<string> ZoneNames { get; }

    private void AdvanceToNextError()
    {
        // Clean up previous error entries
        if (ErrorAttendances?.Count > 0)
        {
            foreach (AttendanceEntry errorAttendance in ErrorAttendances)
                errorAttendance.PossibleError = PossibleError.None;
        }

        if (_currentEntry != null)
            _currentEntry.PossibleError = PossibleError.None;

        ShowCharacterRemovedFromAttendanceMessage = false;
        IsBossMobTypoError = false;
        IsDuplicateError = false;
        IsNoZoneNameError = false;
        IsRaidNameTooShortError = false;
        IsMultipleCharsFromSameAccountError = false;

        SetNextButtonText();

        // Initialize next error
        _currentEntry = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).FirstOrDefault(x => x.PossibleError != PossibleError.None);
        if (_currentEntry == null)
        {
            MultipleCharsOnAttendanceError multipleCharsError = _raidEntries.MultipleCharsInAttendanceErrors
                .OrderBy(x => x.Attendance.Timestamp)
                .FirstOrDefault(x => !x.Reviewed);
            if (multipleCharsError == null)
            {
                CloseOk();
            }
            else
            {
                IsMultipleCharsFromSameAccountError = true;

                multipleCharsError.Reviewed = true;

                ErrorMessageText = Strings.GetString("MultipleCharsFromSameAccountError");
                FirstMultipleCharacter = multipleCharsError.MultipleCharsInAttendance.FirstCharacter.Name;
                SecondMultipleCharacter = multipleCharsError.MultipleCharsInAttendance.SecondCharacter.Name;
                ErrorAttendances = [multipleCharsError.Attendance];
                SelectedErrorEntry = multipleCharsError.Attendance;

                IEnumerable<DkpUserCharacter> charsInAccount =
                    _settings.CharactersOnDkpServer.GetAllRelatedCharacters(multipleCharsError.MultipleCharsInAttendance.FirstCharacter);
                AllCharactersInAccount = charsInAccount.Select(x => x.Name).ToList();
            }

            return;
        }

        UpdateAllAttendances();
        SelectedErrorEntry = null;
        RaidNameText = string.Empty;
        RemoveDuplicateErrorEntryCommand.RaiseCanExecuteChanged();
        ChangeBossMobNameCommand.RaiseCanExecuteChanged();
        UpdateZoneNameCommand.RaiseCanExecuteChanged();
        UpdateRaidNameCommand.RaiseCanExecuteChanged();

        if (ZoneNames.Contains(_currentEntry.ZoneName))
        {
            SelectedZoneName = _currentEntry.ZoneName;
        }

        if (_currentEntry.PossibleError == PossibleError.DuplicateRaidEntry)
        {
            IsDuplicateError = true;

            ErrorMessageText = Strings.GetString("PossibleDupEntries");
            ErrorAttendances = _raidEntries.AttendanceEntries
                .Where(x => x.CallName.Equals(_currentEntry.CallName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Timestamp)
                .ToList();
            SelectedErrorEntry = ErrorAttendances.Last();
        }
        else if (_currentEntry.PossibleError == PossibleError.BossMobNameTypo)
        {
            IsBossMobTypoError = true;

            ErrorMessageText = Strings.GetString("PossibleBossTypo");
            ErrorAttendances = [_currentEntry];

            SetSelectedBossName();
        }
        else if (_currentEntry.PossibleError == PossibleError.NoZoneName || _currentEntry.PossibleError == PossibleError.InvalidZoneName)
        {
            IsNoZoneNameError = true;

            ErrorMessageText = _currentEntry.PossibleError == PossibleError.NoZoneName ? Strings.GetString("NoZoneNameErrorText") : Strings.GetString("InvalidZoneNameErrorText");
            ErrorAttendances = [_currentEntry];
            SetSelectedZoneName();
        }
        else if (_currentEntry.PossibleError == PossibleError.RaidNameTooShort)
        {
            if (_currentEntry.AttendanceCallType == AttendanceCallType.Time)
            {
                IsRaidNameTooShortError = true;
            }
            else
            {
                IsBossMobTypoError = true;
                SetSelectedBossName();
            }

            ErrorMessageText = Strings.GetString("RaidNameTooShort");
            ErrorAttendances = [_currentEntry];
        }
    }

    private void ChangeBossMobName()
    {
        _currentEntry.CallName = SelectedBossName;
        ErrorAttendances = [_currentEntry];
        UpdateAllAttendances();
    }

    private void RemoveDuplicateErrorEntry()
    {
        _raidEntries.RemoveAttendance(SelectedErrorEntry);

        ErrorAttendances = _raidEntries.AttendanceEntries
            .Where(x => x.CallName.Equals(_currentEntry.CallName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Timestamp)
            .ToList();

        UpdateAllAttendances();
        SetNextButtonText();
    }

    private void RemoveMultipleCharacter(string characterToRemove)
    {
        PlayerCharacter attendanceCharToRemove =
            SelectedErrorEntry.Characters.FirstOrDefault(x => x.CharacterName.Equals(characterToRemove, StringComparison.OrdinalIgnoreCase));

        if (attendanceCharToRemove != null)
        {
            SelectedErrorEntry.Characters.Remove(attendanceCharToRemove);
            ShowCharacterRemovedFromAttendanceMessage = true;
        }
    }

    private void SetNextButtonText()
    {
        int numberOfErrors = _raidEntries.AttendanceEntries.Count(x => x.PossibleError != PossibleError.None);
        NextButtonText = numberOfErrors > 1 ? Strings.GetString("Next") : Strings.GetString("Finish");
    }

    private void SetSelectedBossName()
    {
        string callName = _currentEntry.CallName.Replace("<", string.Empty);
        for (int i = 8; i > 0; i--)
        {
            if (callName.Length < i)
                continue;

            string startOfBossName = callName[..i];
            string approvedBossName = ApprovedBossNames.FirstOrDefault(x => x.StartsWith(startOfBossName));
            if (approvedBossName != null)
            {
                SelectedBossName = approvedBossName;
                break;
            }

            string bossNameWithThe = "The " + startOfBossName;
            string approvedBossNameWithThe = ApprovedBossNames.FirstOrDefault(x => x.StartsWith(bossNameWithThe));
            if (approvedBossNameWithThe != null)
            {
                SelectedBossName = approvedBossNameWithThe;
                break;
            }
        }
    }

    private void SetSelectedZoneName()
    {
        string callZone = _currentEntry.ZoneName;
        if (string.IsNullOrWhiteSpace(callZone))
            return;

        for (int i = 8; i > 0; i--)
        {
            if (callZone.Length < i)
                continue;

            string startOfZoneName = callZone[..i];
            string approvedZone = ZoneNames.FirstOrDefault(x => x.StartsWith(startOfZoneName));
            if (approvedZone != null)
            {
                SelectedZoneName = approvedZone;
                break;
            }

            string zoneNameWithThe = "The " + startOfZoneName;
            string approvedBossNameWithThe = ZoneNames.FirstOrDefault(x => x.StartsWith(zoneNameWithThe));
            if (approvedBossNameWithThe != null)
            {
                SelectedZoneName = approvedBossNameWithThe;
                break;
            }
        }
    }

    private void UpdateAllAttendances()
        => AllAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();

    private void UpdateRaidName()
    {
        _currentEntry.CallName = RaidNameText;
        ErrorAttendances = [_currentEntry];
        UpdateAllAttendances();
    }

    private void UpdateSelectedRaidName()
    {
        ICollection<AttendanceEntry> attendances = ErrorAttendances;
        AttendanceEntry selectedAttendance = SelectedErrorEntry;
        selectedAttendance.CallName = RaidNameText;
        ErrorAttendances = [.. attendances];
        UpdateAllAttendances();
    }

    private void UpdateZoneName()
    {
        _currentEntry.ZoneName = SelectedZoneName;
        ErrorAttendances = [_currentEntry];
        UpdateAllAttendances();
    }
}

public interface IAttendanceErrorDisplayDialogViewModel : IDialogViewModel
{
    ICollection<AttendanceEntry> AllAttendances { get; }

    ICollection<string> AllCharactersInAccount { get; }

    ICollection<string> ApprovedBossNames { get; }

    DelegateCommand ChangeBossMobNameCommand { get; }

    ICollection<AttendanceEntry> ErrorAttendances { get; }

    string ErrorLogEntry { get; }

    string ErrorMessageText { get; }

    string FirstMultipleCharacter { get; set; }

    bool IsBossMobTypoError { get; }

    bool IsDuplicateError { get; }

    bool IsMultipleCharsFromSameAccountError { get; }

    bool IsNoZoneNameError { get; }

    bool IsRaidNameTooShortError { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string NextButtonText { get; }

    string RaidNameText { get; set; }

    DelegateCommand RemoveDuplicateErrorEntryCommand { get; }

    DelegateCommand RemoveFirstMultipleCharacterCommand { get; }

    DelegateCommand RemoveSecondMultipleCharacterCommand { get; }

    string SecondMultipleCharacter { get; set; }

    string SelectedBossName { get; set; }

    AttendanceEntry SelectedErrorEntry { get; set; }

    string SelectedZoneName { get; set; }

    bool ShowCharacterRemovedFromAttendanceMessage { get; }

    DelegateCommand UpdateRaidNameCommand { get; }

    DelegateCommand UpdateSelectedRaidNameCommand { get; }

    DelegateCommand UpdateZoneNameCommand { get; }

    ICollection<string> ZoneNames { get; }
}
