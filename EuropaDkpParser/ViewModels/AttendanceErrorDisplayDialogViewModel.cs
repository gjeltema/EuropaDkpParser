// -----------------------------------------------------------------------
// AttendanceErrorDisplayDialogViewModel.cs Copyright 2024 Craig Gjeltema
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
    private AttendanceEntry _currentEntry;
    private ICollection<AttendanceEntry> _errorAttendances;
    private string _errorLogEntry;
    private string _errorMessageText;
    private bool _isBossMobTypoError;
    private bool _isDuplicateError;
    private bool _isNoZoneNameError;
    private bool _isRaidNameTooShortError;
    private string _nextButtonText;
    private string _raidNameText;
    private string _selectedBossName;
    private AttendanceEntry _selectedErrorEntry;
    private string _selectedZoneName;

    internal AttendanceErrorDisplayDialogViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("AttendanceErrorDisplayDialogTitleText");

        _settings = settings;
        _raidEntries = raidEntries;

        ZoneNames = _settings.RaidValue.AllValidRaidZoneNames;

        ApprovedBossNames = _settings.RaidValue.AllBossMobNames;
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

        SetNextButtonText();

        // Initialize next error
        _currentEntry = _raidEntries.AttendanceEntries.FirstOrDefault(x => x.PossibleError != PossibleError.None);
        if (_currentEntry == null)
        {
            CloseOk();
            return;
        }

        AllAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
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
            IsBossMobTypoError = false;
            IsDuplicateError = true;
            IsNoZoneNameError = false;
            IsRaidNameTooShortError = false;

            ErrorMessageText = Strings.GetString("PossibleDupEntries");
            ErrorAttendances = _raidEntries.AttendanceEntries
                .Where(x => x.RaidName.Equals(_currentEntry.RaidName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else if (_currentEntry.PossibleError == PossibleError.BossMobNameTypo)
        {
            IsBossMobTypoError = true;
            IsDuplicateError = false;
            IsNoZoneNameError = false;
            IsRaidNameTooShortError = false;

            ErrorMessageText = Strings.GetString("PossibleBossTypo");
            ErrorAttendances = [_currentEntry];

            SetSelectedBossName();
        }
        else if (_currentEntry.PossibleError == PossibleError.NoZoneName || _currentEntry.PossibleError == PossibleError.InvalidZoneName)
        {
            IsBossMobTypoError = false;
            IsDuplicateError = false;
            IsNoZoneNameError = true;
            IsRaidNameTooShortError = false;

            ErrorMessageText = _currentEntry.PossibleError == PossibleError.NoZoneName ? Strings.GetString("NoZoneNameErrorText") : Strings.GetString("InvalidZoneNameErrorText");
            ErrorAttendances = [_currentEntry];
        }
        else if (_currentEntry.PossibleError == PossibleError.RaidNameTooShort)
        {
            IsDuplicateError = false;
            IsNoZoneNameError = false;

            if (_currentEntry.AttendanceCallType == AttendanceCallType.Time)
            {
                IsRaidNameTooShortError = true;
                IsBossMobTypoError = false;
            }
            else
            {
                IsRaidNameTooShortError = false;
                IsBossMobTypoError = true;
                SetSelectedBossName();
            }

            ErrorMessageText = Strings.GetString("RaidNameTooShort");
            ErrorAttendances = [_currentEntry];
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
        NextButtonText = numberOfErrors > 1 ? Strings.GetString("Next") : Strings.GetString("Finish");
    }

    private void SetSelectedBossName()
    {
        for (int i = 8; i > 0; i--)
        {
            if (_currentEntry.RaidName.Length < i)
                continue;

            string startOfBossName = _currentEntry.RaidName[..i];
            string approvedBossName = ApprovedBossNames.FirstOrDefault(x => x.StartsWith(startOfBossName));
            if (approvedBossName != null)
            {
                SelectedBossName = approvedBossName;
                break;
            }
        }
    }

    private void UpdateRaidName()
    {
        _currentEntry.RaidName = RaidNameText;
        ErrorAttendances = [_currentEntry];
    }

    private void UpdateSelectedRaidName()
    {
        ICollection<AttendanceEntry> attendances = ErrorAttendances;
        AttendanceEntry selectedAttendance = SelectedErrorEntry;
        selectedAttendance.RaidName = RaidNameText;
        ErrorAttendances = [.. attendances];
    }

    private void UpdateZoneName()
    {
        _currentEntry.ZoneName = SelectedZoneName;
        string zoneAlias = _settings.RaidValue.GetZoneRaidAlias(_currentEntry.ZoneName);

        RaidInfo raidInfo = _raidEntries.Raids.FirstOrDefault(x => x.RaidZone == zoneAlias);
        if (raidInfo == null)
        {
            RaidInfo newRaidInfo = new() { RaidZone = _currentEntry.ZoneName };
            _raidEntries.Raids.Add(newRaidInfo);
        }

        _raidEntries.UpdateRaids(_settings.RaidValue.GetZoneRaidAlias);

        ErrorAttendances = [_currentEntry];
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

    bool IsBossMobTypoError { get; }

    bool IsDuplicateError { get; }

    bool IsNoZoneNameError { get; }

    bool IsRaidNameTooShortError { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string NextButtonText { get; }

    string RaidNameText { get; set; }

    DelegateCommand RemoveDuplicateErrorEntryCommand { get; }

    string SelectedBossName { get; set; }

    AttendanceEntry SelectedErrorEntry { get; set; }

    string SelectedZoneName { get; set; }

    DelegateCommand UpdateRaidNameCommand { get; }

    DelegateCommand UpdateSelectedRaidNameCommand { get; }

    DelegateCommand UpdateZoneNameCommand { get; }

    ICollection<string> ZoneNames { get; }
}
