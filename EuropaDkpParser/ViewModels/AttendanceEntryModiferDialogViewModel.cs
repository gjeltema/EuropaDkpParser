// -----------------------------------------------------------------------
// AttendanceEntryModiferDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Collections.ObjectModel;
using System.Windows;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class AttendanceEntryModiferDialogViewModel : DialogViewModelBase, IAttendanceEntryModiferDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ObservableCollection<AttendanceEntry> _allAttendances;
    private string _moveTimeText;
    private AttendanceCallType _newAttendanceCallType;
    private string _newRaidName;
    private string _newTimeText;
    private string _raidNameText;
    private AttendanceEntry _selectedAttendanceEntry;
    private string _selectedZoneName;

    public AttendanceEntryModiferDialogViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("AttendanceEntryModifierDialogTitleText");
        Height = 620;

        _settings = settings;
        _raidEntries = raidEntries;

        AllAttendances = new ObservableCollection<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));

        AddAttendanceCallCommand = new DelegateCommand(AddAttendanceCall, () => SelectedAttendanceEntry != null && !string.IsNullOrWhiteSpace(NewRaidName) && !string.IsNullOrWhiteSpace(NewTimeText))
            .ObservesProperty(() => SelectedAttendanceEntry).ObservesProperty(() => NewRaidName).ObservesProperty(() => NewTimeText);
        MoveAttendanceCallCommand = new DelegateCommand(MoveAttendanceCall, () => SelectedAttendanceEntry != null && !string.IsNullOrWhiteSpace(MoveTimeText))
            .ObservesProperty(() => SelectedAttendanceEntry).ObservesProperty(() => MoveTimeText);
        RemoveAttendanceEntryCommand = new DelegateCommand(RemoveAttendance, () => SelectedAttendanceEntry != null)
            .ObservesProperty(() => SelectedAttendanceEntry);
        UpdateZoneNameCommand = new DelegateCommand(UpdateZoneName, () => SelectedAttendanceEntry != null && !string.IsNullOrWhiteSpace(SelectedZoneName) && SelectedZoneName != SelectedAttendanceEntry.ZoneName)
            .ObservesProperty(() => SelectedZoneName).ObservesProperty(() => SelectedAttendanceEntry);
        UpdateRaidNameCommand = new DelegateCommand(UpdateRaidName, () => SelectedAttendanceEntry != null && !string.IsNullOrWhiteSpace(RaidNameText) && RaidNameText != SelectedAttendanceEntry.CallName)
            .ObservesProperty(() => RaidNameText).ObservesProperty(() => SelectedAttendanceEntry);

        ZoneNames = _settings.RaidValue.AllValidRaidZoneNames;

        AttendanceCallTypes = [AttendanceCallType.Time, AttendanceCallType.Kill];
        NewAttendanceCallType = AttendanceCallType.Time;
    }

    public DelegateCommand AddAttendanceCallCommand { get; }

    public ObservableCollection<AttendanceEntry> AllAttendances
    {
        get => _allAttendances;
        private set => SetProperty(ref _allAttendances, value);
    }

    public ICollection<AttendanceCallType> AttendanceCallTypes { get; }

    public DelegateCommand MoveAttendanceCallCommand { get; }

    public string MoveTimeText
    {
        get => _moveTimeText;
        set => SetProperty(ref _moveTimeText, value);
    }

    public AttendanceCallType NewAttendanceCallType
    {
        get => _newAttendanceCallType;
        set => SetProperty(ref _newAttendanceCallType, value);
    }

    public string NewRaidName
    {
        get => _newRaidName;
        set => SetProperty(ref _newRaidName, value);
    }

    public string NewTimeText
    {
        get => _newTimeText;
        set => SetProperty(ref _newTimeText, value);
    }

    public string RaidNameText
    {
        get => _raidNameText;
        set => SetProperty(ref _raidNameText, value);
    }

    public DelegateCommand RemoveAttendanceEntryCommand { get; }

    public AttendanceEntry SelectedAttendanceEntry
    {
        get => _selectedAttendanceEntry;
        set
        {
            SetProperty(ref _selectedAttendanceEntry, value);
            if (value != null)
            {
                string timeText = value.Timestamp.ToString(Constants.StandardDateTimeDisplayFormat);
                NewTimeText = timeText;
                MoveTimeText = timeText;

                if (ZoneNames.Contains(value.ZoneName))
                {
                    SelectedZoneName = value.ZoneName;
                }

                RaidNameText = value.CallName;
            }
        }
    }

    public string SelectedZoneName
    {
        get => _selectedZoneName;
        set => SetProperty(ref _selectedZoneName, value);
    }

    public DelegateCommand UpdateRaidNameCommand { get; }

    public DelegateCommand UpdateZoneNameCommand { get; }

    public ICollection<string> ZoneNames { get; }

    private void AddAttendanceCall()
    {
        if (!ValidateInputItems(NewTimeText, out DateTime newTimestamp, true))
        {
            return;
        }

        IAttendanceEntryModifier modifier = new AttendanceEntryModifier(_raidEntries);
        AttendanceEntry newEntry = modifier.CreateAttendanceEntry(SelectedAttendanceEntry, newTimestamp, NewRaidName, NewAttendanceCallType);
        if (newEntry == null)
        {
            return;
        }

        _raidEntries.AttendanceEntries.Add(newEntry);
        AttendanceEntry nextEntry = AllAttendances.Where(x => x.Timestamp > newEntry.Timestamp).MinBy(x => x.Timestamp);
        int indexOfNextEntry = AllAttendances.IndexOf(nextEntry);
        if (indexOfNextEntry < 0)
            AllAttendances.Add(newEntry);
        else
            AllAttendances.Insert(indexOfNextEntry, newEntry);
    }

    private void MoveAttendanceCall()
    {
        if (!ValidateInputItems(MoveTimeText, out DateTime newTimestamp, false))
        {
            return;
        }

        AttendanceEntry selectedEntry = SelectedAttendanceEntry;
        SelectedAttendanceEntry = null;
        IAttendanceEntryModifier modifier = new AttendanceEntryModifier(_raidEntries);
        modifier.MoveAttendanceEntry(selectedEntry, newTimestamp);

        AllAttendances.Remove(selectedEntry);
        AttendanceEntry nextEntry = AllAttendances.Where(x => x.Timestamp > selectedEntry.Timestamp).MinBy(x => x.Timestamp);
        int indexOfNextEntry = AllAttendances.IndexOf(nextEntry);
        if (indexOfNextEntry < 0)
            AllAttendances.Add(selectedEntry);
        else
            AllAttendances.Insert(indexOfNextEntry, selectedEntry);
    }

    private void RemoveAttendance()
    {
        AttendanceEntry selected = SelectedAttendanceEntry;
        if (selected == null)
            return;

        SelectedAttendanceEntry = null;
        _raidEntries.RemoveAttendance(selected);
        AllAttendances.Remove(selected);
    }

    private void UpdateEntryDisplay(AttendanceEntry updatedEntry)
    {
        int indexOfEntry = AllAttendances.IndexOf(updatedEntry);
        if (indexOfEntry == -1)
            return;

        AllAttendances.RemoveAt(indexOfEntry);
        AllAttendances.Insert(indexOfEntry, updatedEntry);
    }

    private void UpdateRaidName()
    {
        AttendanceEntry entryToUpdate = SelectedAttendanceEntry;
        SelectedAttendanceEntry = null;

        entryToUpdate.CallName = RaidNameText;

        UpdateEntryDisplay(entryToUpdate);
        SelectedAttendanceEntry = entryToUpdate;
    }

    private void UpdateZoneName()
    {
        AttendanceEntry entryToUpdate = SelectedAttendanceEntry;
        SelectedAttendanceEntry = null;
        entryToUpdate.ZoneName = SelectedZoneName;

        UpdateEntryDisplay(entryToUpdate);
        SelectedAttendanceEntry = entryToUpdate;
    }

    private bool ValidateInputItems(string newTimeText, out DateTime newTimestamp, bool isAddAttendanceCommand)
    {
        newTimestamp = DateTime.MinValue;
        if (SelectedAttendanceEntry == null)
        {
            MessageBox.Show(Strings.GetString("NoSelectedAttendanceEntryErrorMessage"), Strings.GetString("NoSelectedAttendanceEntryError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (isAddAttendanceCommand && string.IsNullOrWhiteSpace(NewRaidName))
        {
            MessageBox.Show(Strings.GetString("NoRaidNameEnteredErrorMessage"), Strings.GetString("NoRaidNameEnteredError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (!DateTime.TryParse(newTimeText, out newTimestamp))
        {
            MessageBox.Show(Strings.GetString("NoModifyEntryTimestampErrorMessage"), Strings.GetString("NoModifyEntryTimestampError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }
}

public interface IAttendanceEntryModiferDialogViewModel : IDialogViewModel
{
    DelegateCommand AddAttendanceCallCommand { get; }

    ObservableCollection<AttendanceEntry> AllAttendances { get; }

    ICollection<AttendanceCallType> AttendanceCallTypes { get; }

    DelegateCommand MoveAttendanceCallCommand { get; }

    string MoveTimeText { get; set; }

    AttendanceCallType NewAttendanceCallType { get; set; }

    string NewRaidName { get; set; }

    string NewTimeText { get; set; }

    string RaidNameText { get; set; }

    DelegateCommand RemoveAttendanceEntryCommand { get; }

    AttendanceEntry SelectedAttendanceEntry { get; set; }

    string SelectedZoneName { get; set; }

    DelegateCommand UpdateRaidNameCommand { get; }

    DelegateCommand UpdateZoneNameCommand { get; }

    ICollection<string> ZoneNames { get; }
}
