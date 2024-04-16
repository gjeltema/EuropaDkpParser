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
    private ICollection<string> _attendancesAndJoins;
    private PlayerPossibleLinkdead _currentEntry;
    private string _nextButtonText;
    private string _playerAddedMessage;
    private string _playerName;
    private string _selectedAttendance;

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

    public ICollection<string> AttendancesAndJoins
    {
        get => _attendancesAndJoins;
        private set => SetProperty(ref _attendancesAndJoins, value);
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

    public string SelectedAttendance
    {
        get => _selectedAttendance;
        set => SetProperty(ref _selectedAttendance, value);
    }
    
    private void AddToAttendance()
    {
        _currentEntry.AttendanceMissingFrom.Players.Add(_currentEntry.Player);
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
        IOrderedEnumerable<string> entries = _raidEntries.AttendanceEntries.Select(x => x.ToDisplayString())
            .Union(
                _raidEntries.PlayerJoinCalls
                .Where(x => x.PlayerName == _currentEntry.Player.PlayerName)
                .Select(x => x.ToString())
            )
            .Order();

        AttendancesAndJoins = entries.ToList();
        SelectedAttendance = _currentEntry.AttendanceMissingFrom.ToDisplayString();
    }

    private void SetNextButtonText()
    {
        int numberOfErrors = _raidEntries.PossibleLinkdeads.Count(x => !x.Addressed);
        NextButtonText = numberOfErrors > 1 ? Strings.GetString("Next") : Strings.GetString("Finish");
    }
}

public interface IPossibleLinkdeadErrorDialogViewModel : IDialogViewModel
{
    DelegateCommand AddToAttendanceCommand { get; }

    string AttendanceMissingFrom { get; }

    ICollection<string> AttendancesAndJoins { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string NextButtonText { get; }

    string PlayerName { get; }

    string SelectedAttendance { get; set; }

    string PlayerAddedMessage { get; }
}
