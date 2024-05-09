// -----------------------------------------------------------------------
// PossibleLinkdeadErrorDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class PossibleLinkdeadErrorDialogViewModel : DialogViewModelBase, IPossibleLinkdeadErrorDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private string _attendanceMissingFrom;
    private ICollection<AttendanceJoinDisplay> _attendancesAndJoins;
    private ICollection<string> _attendees;
    private PlayerPossibleLinkdead _currentEntry;
    private string _nextButtonText;
    private string _playerAddedMessage;
    private string _playerName;
    private AttendanceJoinDisplay _selectedAttendance;

    public PossibleLinkdeadErrorDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("PossibleLinkdeadErrorDialogTitleText");

        _raidEntries = raidEntries;

        AddToAttendanceCommand = new DelegateCommand(AddToAttendance);
        MoveToNextErrorCommand = new DelegateCommand(MoveToNextError);

        MoveToNextError();
    }

    public DelegateCommand AddToAttendanceCommand { get; }

    public string AttendanceMissingFrom
    {
        get => _attendanceMissingFrom;
        private set => SetProperty(ref _attendanceMissingFrom, value);
    }

    public ICollection<AttendanceJoinDisplay> AttendancesAndJoins
    {
        get => _attendancesAndJoins;
        private set => SetProperty(ref _attendancesAndJoins, value);
    }

    public ICollection<string> Attendees
    {
        get => _attendees;
        private set => SetProperty(ref _attendees, value);
    }

    public DelegateCommand MoveToNextErrorCommand { get; }

    public string NextButtonText
    {
        get => _nextButtonText;
        private set => SetProperty(ref _nextButtonText, value);
    }

    public string PlayerAddedMessage
    {
        get => _playerAddedMessage;
        private set => SetProperty(ref _playerAddedMessage, value);
    }

    public string PlayerName
    {
        get => _playerName;
        private set => SetProperty(ref _playerName, value);
    }

    public AttendanceJoinDisplay SelectedAttendance
    {
        get => _selectedAttendance;
        set => SetProperty(ref _selectedAttendance, value);
    }

    private void AddToAttendance()
    {
        _currentEntry.AttendanceMissingFrom.Players.Add(_currentEntry.Player);
        Attendees = _currentEntry.AttendanceMissingFrom.Players.Select(x => x.PlayerName).Order().ToList();
        PlayerAddedMessage = Strings.GetString("PlayerAddedMessage");
    }

    private void MoveToNextError()
    {
        PlayerAddedMessage = string.Empty;

        if (_currentEntry != null)
            _currentEntry.Addressed = true;

        SetNextButtonText();

        // Initialize next error
        _currentEntry = _raidEntries.PossibleLinkdeads.FirstOrDefault(x => !x.Addressed);
        if (_currentEntry == null)
        {
            CloseOk();
            return;
        }

        AttendanceMissingFrom = _currentEntry.AttendanceMissingFrom.ToDisplayString();
        PlayerName = _currentEntry.Player.PlayerName;

        PopulateAttendancesAndJoins();
    }

    private void PopulateAttendancesAndJoins()
    {
        IOrderedEnumerable<AttendanceJoinDisplay> entries = _raidEntries.AttendanceEntries.Select(x => new AttendanceJoinDisplay(x))
            .Union(
                _raidEntries.PlayerJoinCalls
                .Where(x => x.PlayerName == _currentEntry.Player.PlayerName)
                .Select(x => new AttendanceJoinDisplay(x))
            )
            .OrderBy(x => x.Timestamp);

        AttendancesAndJoins = entries.ToList();
        SelectedAttendance = AttendancesAndJoins.FirstOrDefault(x => x.Timestamp == _currentEntry.AttendanceMissingFrom.Timestamp);

        Attendees = _currentEntry.AttendanceMissingFrom.Players.Select(x => x.PlayerName).Order().ToList();
    }

    private void SetNextButtonText()
    {
        int numberOfErrors = _raidEntries.PossibleLinkdeads.Count(x => !x.Addressed);
        NextButtonText = numberOfErrors > 1 ? Strings.GetString("Next") : Strings.GetString("Finish");
    }
}

public sealed class AttendanceJoinDisplay
{
    public AttendanceJoinDisplay(AttendanceEntry entry)
    {
        Timestamp = entry.Timestamp;
        DisplayString = $"{entry.CallName}\t{entry.ZoneName}";
    }

    public AttendanceJoinDisplay(PlayerJoinRaidEntry entry)
    {
        Timestamp = entry.Timestamp;
        DisplayString = $"**** {entry.PlayerName} has {(entry.EntryType == LogEntryType.JoinedRaid ? "JOINED" : "LEFT")} the raid ****";
    }

    public string DisplayString { get; set; }

    public DateTime Timestamp { get; set; }

    public sealed override string ToString()
        => $"{Timestamp:HH:mm:ss}  {DisplayString}";
}

public interface IPossibleLinkdeadErrorDialogViewModel : IDialogViewModel
{
    DelegateCommand AddToAttendanceCommand { get; }

    string AttendanceMissingFrom { get; }

    ICollection<AttendanceJoinDisplay> AttendancesAndJoins { get; }

    ICollection<string> Attendees { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string NextButtonText { get; }

    string PlayerAddedMessage { get; }

    string PlayerName { get; }

    AttendanceJoinDisplay SelectedAttendance { get; set; }
}
