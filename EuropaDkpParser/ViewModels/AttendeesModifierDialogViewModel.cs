// -----------------------------------------------------------------------
// AttendeesModifierDialogViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using Prism.Commands;

internal sealed class AttendeesModifierDialogViewModel : DialogViewModelBase, IAttendeesModifierDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private ICollection<string> _attendees;
    private string _commandResultsMessage;
    private bool _commandSuccess;
    private bool _displayResultsMessage;
    private string _selectedAttendee;

    public AttendeesModifierDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries)
        : base(viewFactory)
    {
        _raidEntries = raidEntries;

        AllAttendances = [.. _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp)];
        AllAttendees = [.. _raidEntries.AllCharactersInRaid.Select(x => x.CharacterName).Order()];
        SetAttendees();

        AddCharacterToSelectedAttendancesCommand = new DelegateCommand(AddToAttendances, () => SelectedAttendee != null)
            .ObservesProperty(() => SelectedAttendee);
        RemoveCharacterFromSelectedAttendancesCommand = new DelegateCommand(RemoveFromAttendances, () => SelectedAttendee != null)
            .ObservesProperty(() => SelectedAttendee);
    }

    public DelegateCommand AddCharacterToSelectedAttendancesCommand { get; }

    public ICollection<AttendanceEntry> AllAttendances { get; }

    public ICollection<string> AllAttendees { get; }

    public ICollection<string> Attendees
    {
        get => _attendees;
        private set => SetProperty(ref _attendees, value);
    }

    public string CommandResultsMessage
    {
        get => _commandResultsMessage;
        private set => SetProperty(ref _commandResultsMessage, value);
    }

    public bool CommandSuccess
    {
        get => _commandSuccess;
        private set => SetProperty(ref _commandSuccess, value);
    }

    public bool DisplayResultsMessage
    {
        get => _displayResultsMessage;
        set => SetProperty(ref _displayResultsMessage, value);
    }

    public DelegateCommand RemoveCharacterFromSelectedAttendancesCommand { get; }

    public ICollection<AttendanceEntry> SelectedAttendances { get; set; } = [];

    public string SelectedAttendee
    {
        get => _selectedAttendee;
        set => SetProperty(ref _selectedAttendee, value);
    }

    public void SignalSelectedAttendancesChanged()
        => SetAttendees();

    private void AddToAttendances()
    {
        ClearResults();

        PlayerCharacter characterToAdd = GetSelectedCharacter();
        if (characterToAdd == null)
        {
            SetResults(false, $"Error, unable to find {SelectedAttendee} in the attendees listing.");
            return;
        }

        ICollection<AttendanceEntry> attendancesToAddTo = [.. SelectedAttendances];

        foreach (AttendanceEntry attendance in attendancesToAddTo)
        {
            attendance.Characters.Add(characterToAdd);
        }

        SetAttendees(characterToAdd);

        SetResults(true, $"Successfully added {characterToAdd.CharacterName} to the selected attendances.");
    }

    private void ClearResults()
    {
        DisplayResultsMessage = false;
        CommandSuccess = false;
        CommandResultsMessage = string.Empty;
    }

    private PlayerCharacter GetSelectedCharacter()
    {
        string selectedCharacterName = SelectedAttendee;
        PlayerCharacter characterToAdd = _raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == selectedCharacterName);
        return characterToAdd;
    }

    private void RemoveFromAttendances()
    {
        ClearResults();

        PlayerCharacter characterToAdd = GetSelectedCharacter();
        if (characterToAdd == null)
        {
            SetResults(false, $"Error, unable to find {SelectedAttendee} in the attendees listing.");
            return;
        }

        ICollection<AttendanceEntry> attendancesToAddTo = [.. SelectedAttendances];

        foreach (AttendanceEntry attendance in attendancesToAddTo)
        {
            attendance.Characters.Remove(characterToAdd);
        }

        SetAttendees();

        SetResults(true, $"Successfully removed {characterToAdd.CharacterName} from the selected attendances.");
    }

    private void SetAttendees(PlayerCharacter previouslySelected = null)
    {
        SelectedAttendee = null;

        Attendees = [.. (from att in SelectedAttendances
                            from chars in att.Characters
                            select chars.CharacterName)
                            .Distinct()
                            .Order()];

        if (previouslySelected != null && Attendees.Contains(previouslySelected.CharacterName))
            SelectedAttendee = previouslySelected.CharacterName;
    }

    private void SetResults(bool success, string message)
    {
        DisplayResultsMessage = true;
        CommandSuccess = success;
        CommandResultsMessage = message;
    }
}

public interface IAttendeesModifierDialogViewModel : IDialogViewModel
{
    DelegateCommand AddCharacterToSelectedAttendancesCommand { get; }

    ICollection<AttendanceEntry> AllAttendances { get; }

    ICollection<string> AllAttendees { get; }

    ICollection<string> Attendees { get; }

    string CommandResultsMessage { get; }

    bool CommandSuccess { get; }

    bool DisplayResultsMessage { get; }

    DelegateCommand RemoveCharacterFromSelectedAttendancesCommand { get; }

    ICollection<AttendanceEntry> SelectedAttendances { get; set; }

    string SelectedAttendee { get; set; }

    void SignalSelectedAttendancesChanged();
}
