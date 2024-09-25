// -----------------------------------------------------------------------
// AfkCheckerDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Diagnostics;
using System.Linq;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class AfkCheckerDialogViewModel : DialogViewModelBase, IAfkCheckerDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private List<PlayerAttendanceEntry> _attendances;
    private PlayerCharacter _currentPlayer;
    private string _playerName;
    private List<PlayerCharacter> _playersReviewed = [];
    private string _removedPlayerText;
    private PlayerAttendanceEntry _selectedPlayerAttendanceEntry;

    public AfkCheckerDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("AfkCheckerDialogTitleText");

        _raidEntries = raidEntries;

        Attendances = new List<PlayerAttendanceEntry>();

        MoveToNextPlayerCommand = new DelegateCommand(MoveToNextAfkPlayer);
        RemovePlayerFromAttendanceCommand = new DelegateCommand(RemovePlayer, () => SelectedPlayerAttendanceEntry != null)
            .ObservesProperty(() => SelectedPlayerAttendanceEntry);

        SetNextAfkPlayer();
    }

    public List<PlayerAttendanceEntry> Attendances
    {
        get => _attendances;
        private set => SetProperty(ref _attendances, value);
    }

    public DelegateCommand MoveToNextPlayerCommand { get; }

    public string PlayerName
    {
        get => _playerName;
        private set => SetProperty(ref _playerName, value);
    }

    public string RemovedPlayerText
    {
        get => _removedPlayerText;
        private set => SetProperty(ref _removedPlayerText, value);
    }

    public DelegateCommand RemovePlayerFromAttendanceCommand { get; }

    public PlayerAttendanceEntry SelectedPlayerAttendanceEntry
    {
        get => _selectedPlayerAttendanceEntry;
        set => SetProperty(ref _selectedPlayerAttendanceEntry, value);
    }

    public bool SetNextAfkPlayer()
    {
        if (_currentPlayer != null)
            _playersReviewed.Add(_currentPlayer);

        RemovedPlayerText = string.Empty;

        SelectedPlayerAttendanceEntry = null;

        bool reviewPlayer = false;

        while (!reviewPlayer)
        {
            _currentPlayer = _raidEntries.AllCharactersInRaid.FirstOrDefault(x => !_playersReviewed.Contains(x));
            if (_currentPlayer == null)
            {
                CloseOk();
                return false;
            }

            _playersReviewed.Add(_currentPlayer);

            List<PlayerAttendanceEntry> attendances = [];

            foreach (AttendanceEntry attendance in _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp))
            {
                PlayerCharacter player = attendance.Characters.FirstOrDefault(x => x.CharacterName == _currentPlayer.CharacterName);
                if (player != null)
                {
                    PlayerAttendanceEntry playerAttendance = new()
                    {
                        Attendance = attendance,
                        PlayerName = _currentPlayer.CharacterName,
                        Timestamp = attendance.Timestamp,
                        IsAfk = attendance.AfkPlayers.Any(x => x.CharacterName == _currentPlayer.CharacterName)
                    };

                    attendances.Add(playerAttendance);
                }
            }

            if (attendances.Count(x => x.IsAfk) > (_raidEntries.AttendanceEntries.Count / 3.0))
            {
                reviewPlayer = true;
                PlayerName = _currentPlayer.CharacterName;
                Attendances.Clear();
                Attendances.AddRange(attendances);
            }
        }

        return true;
    }

    private void MoveToNextAfkPlayer()
        => SetNextAfkPlayer();

    private void RemovePlayer()
    {
        PlayerCharacter player = SelectedPlayerAttendanceEntry.Attendance.Characters.FirstOrDefault(x => x.CharacterName == SelectedPlayerAttendanceEntry.PlayerName);
        if (player == null)
            return;

        SelectedPlayerAttendanceEntry.Attendance.Characters.Remove(player);
        RemovedPlayerText = $"Removed {player.CharacterName} from {SelectedPlayerAttendanceEntry.Attendance.CallName}";
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class PlayerAttendanceEntry
{
    public AttendanceEntry Attendance { get; init; }

    public string DebugText
        => $"{PlayerName} {(IsAfk ? "AFK" : "")} {Attendance.CallName}";

    public bool IsAfk { get; init; }

    public string PlayerName { get; init; }

    public DateTime Timestamp { get; init; }

    public override string ToString()
        => $"{(IsAfk ? "AFK" : "    ")}  {Attendance.CallName}";
}

public interface IAfkCheckerDialogViewModel : IDialogViewModel
{
    List<PlayerAttendanceEntry> Attendances { get; }

    DelegateCommand MoveToNextPlayerCommand { get; }

    string PlayerName { get; }

    string RemovedPlayerText { get; }

    DelegateCommand RemovePlayerFromAttendanceCommand { get; }

    PlayerAttendanceEntry SelectedPlayerAttendanceEntry { get; set; }

    bool SetNextAfkPlayer();
}
