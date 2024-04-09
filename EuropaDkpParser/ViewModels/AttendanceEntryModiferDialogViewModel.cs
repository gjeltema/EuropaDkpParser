﻿// -----------------------------------------------------------------------
// AttendanceEntryModiferDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class AttendanceEntryModiferDialogViewModel : DialogViewModelBase, IAttendanceEntryModiferDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private ICollection<AttendanceEntry> _allAttendances;
    private string _newRaidName;
    private string _newTimeText;
    private AttendanceEntry _selectedAttendanceEntry;

    public AttendanceEntryModiferDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("AttendanceEntryModifierDialogTitleText");

        _raidEntries = raidEntries;

        AllAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();

        AddAttendanceCallCommand = new DelegateCommand(AddAttendanceCall, () => SelectedAttendanceEntry != null && !string.IsNullOrWhiteSpace(NewRaidName) && !string.IsNullOrWhiteSpace(NewTimeText))
            .ObservesProperty(() => SelectedAttendanceEntry).ObservesProperty(() => NewRaidName).ObservesProperty(() => NewTimeText);
        MoveAttendanceCallCommand = new DelegateCommand(MoveAttendanceCall, () => SelectedAttendanceEntry != null && !string.IsNullOrWhiteSpace(NewTimeText))
            .ObservesProperty(() => SelectedAttendanceEntry).ObservesProperty(() => NewTimeText);
    }

    public DelegateCommand AddAttendanceCallCommand { get; }

    public ICollection<AttendanceEntry> AllAttendances
    {
        get => _allAttendances;
        private set => SetProperty(ref _allAttendances, value);
    }

    public DelegateCommand MoveAttendanceCallCommand { get; }

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

    public AttendanceEntry SelectedAttendanceEntry
    {
        get => _selectedAttendanceEntry;
        set
        {
            SetProperty(ref _selectedAttendanceEntry, value);
            NewTimeText = value.Timestamp.ToString(Constants.StandardDateTimeDisplayFormat);
        }
    }

    private void AddAttendanceCall()
    {
        if (!ValidateInputItems(out DateTime newTimestamp, true))
        {
            return;
        }

        IAttendanceEntryModifier modifier = new AttendanceEntryModifier(_raidEntries);
        AttendanceEntry newEntry = modifier.CreateAttendanceEntry(SelectedAttendanceEntry, newTimestamp, NewRaidName);
        if (newEntry == null)
        {
            return;
        }

        _raidEntries.AttendanceEntries.Add(newEntry);
        AllAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
    }

    private void MoveAttendanceCall()
    {
        if (!ValidateInputItems(out DateTime newTimestamp, false))
        {
            return;
        }

        IAttendanceEntryModifier modifier = new AttendanceEntryModifier(_raidEntries);
        modifier.MoveAttendanceEntry(SelectedAttendanceEntry, newTimestamp);

        AllAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
    }

    private bool ValidateInputItems(out DateTime newTimestamp, bool isAddAttendanceCommand)
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

        if (!DateTime.TryParse(NewTimeText, out newTimestamp))
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

    ICollection<AttendanceEntry> AllAttendances { get; }

    DelegateCommand MoveAttendanceCallCommand { get; }

    string NewRaidName { get; set; }

    string NewTimeText { get; set; }

    AttendanceEntry SelectedAttendanceEntry { get; set; }
}