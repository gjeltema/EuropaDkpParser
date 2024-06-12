// -----------------------------------------------------------------------
// AttendanceEntryModifier.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

/// <summary>
/// Moves an attendance entry or adds a new one, using "has joined the raid" and "left the raid" entries to maintain a correct
/// attendee list.
/// </summary>
public sealed class AttendanceEntryModifier : IAttendanceEntryModifier
{
    private readonly RaidEntries _raidEntries;

    public AttendanceEntryModifier(RaidEntries raidEntries)
    {
        _raidEntries = raidEntries;
    }

    public AttendanceEntry CreateAttendanceEntry(AttendanceEntry baseline, DateTime newAttendanceTimestamp, string newRaidName, AttendanceCallType newCallType)
    {
        AttendanceEntry newEntry = new()
        {
            AttendanceCallType = newCallType,
            CallName = newRaidName,
            Players = new HashSet<PlayerCharacter>(baseline.Players),
            RawHeaderLogLine = "<Created Attendance Entry>",
            Timestamp = newAttendanceTimestamp,
            ZoneName = baseline.ZoneName
        };

        if (baseline.Timestamp <= newAttendanceTimestamp)
        {
            ModifyPlayersMovingForwards(baseline.Timestamp, newEntry, newAttendanceTimestamp);
        }
        else
        {
            ModifyPlayersMovingBackwards(baseline.Timestamp, newEntry, newAttendanceTimestamp);
        }

        return newEntry;
    }

    public void MoveAttendanceEntry(AttendanceEntry toBeMoved, DateTime newTimestamp)
    {
        DateTime baselineTimestamp = toBeMoved.Timestamp;
        toBeMoved.Timestamp = newTimestamp;
        if (baselineTimestamp <= newTimestamp)
        {
            ModifyPlayersMovingForwards(baselineTimestamp, toBeMoved, newTimestamp);
        }
        else
        {
            ModifyPlayersMovingBackwards(baselineTimestamp, toBeMoved, newTimestamp);
        }
    }

    private void AddPlayer(AttendanceEntry entry, PlayerJoinRaidEntry playerJoin)
    {
        if (entry.Players.FirstOrDefault(x => x.PlayerName == playerJoin.PlayerName) == null)
        {
            PlayerCharacter playerToAdd = _raidEntries.AllPlayersInRaid.FirstOrDefault(x => x.PlayerName == playerJoin.PlayerName);
            if (playerToAdd != null)
            {
                entry.Players.Add(playerToAdd);
            }
        }
    }

    private void ModifyPlayersMovingBackwards(DateTime baselineTimestamp, AttendanceEntry toBeModified, DateTime newAttendanceTimestamp)
    {
        IList<PlayerJoinRaidEntry> playerJoinedList =
                _raidEntries.PlayerJoinCalls.Where(x => newAttendanceTimestamp <= x.Timestamp && x.Timestamp < baselineTimestamp).Reverse().ToList();

        foreach (PlayerJoinRaidEntry playerJoin in playerJoinedList)
        {
            if (playerJoin.EntryType == LogEntryType.JoinedRaid)
            {
                RemovePlayer(toBeModified, playerJoin);
            }
            else if (playerJoin.EntryType == LogEntryType.LeftRaid)
            {
                AddPlayer(toBeModified, playerJoin);
            }
            else
            {
                // Is an error, should not reach here
                Debug.Fail($"Reached area in {nameof(ModifyPlayersMovingBackwards)} that should not be reached - LogEntryType is not a valid value: {playerJoin.EntryType}.");
            }
        }
    }

    private void ModifyPlayersMovingForwards(DateTime baselineTimestamp, AttendanceEntry toBeModified, DateTime newAttendanceTimestamp)
    {
        IEnumerable<PlayerJoinRaidEntry> playerJoinedEnumerable =
                _raidEntries.PlayerJoinCalls.Where(x => baselineTimestamp <= x.Timestamp && x.Timestamp < newAttendanceTimestamp);

        foreach (PlayerJoinRaidEntry playerJoin in playerJoinedEnumerable)
        {
            if (playerJoin.EntryType == LogEntryType.JoinedRaid)
            {
                AddPlayer(toBeModified, playerJoin);
            }
            else if (playerJoin.EntryType == LogEntryType.LeftRaid)
            {
                RemovePlayer(toBeModified, playerJoin);
            }
            else
            {
                // Is an error, should not reach here
                Debug.Fail($"Reached area in {nameof(ModifyPlayersMovingForwards)} that should not be reached - LogEntryType is not a valid value: {playerJoin.EntryType}.");
            }
        }
    }

    private void RemovePlayer(AttendanceEntry entry, PlayerJoinRaidEntry playerJoin)
    {
        PlayerCharacter playerToRemove = entry.Players.FirstOrDefault(x => x.PlayerName == playerJoin.PlayerName);
        if (playerToRemove != null)
        {
            entry.Players.Remove(playerToRemove);
        }
    }
}

/// <summary>
/// Moves an attendance entry or adds a new one, using "has joined the raid" and "left the raid" entries to maintain a correct
/// attendee list.
/// </summary>
public interface IAttendanceEntryModifier
{
    AttendanceEntry CreateAttendanceEntry(AttendanceEntry baseline, DateTime newEntryTimestamp, string newRaidName, AttendanceCallType newCallType);

    void MoveAttendanceEntry(AttendanceEntry toBeMoved, DateTime newTimestamp);
}
