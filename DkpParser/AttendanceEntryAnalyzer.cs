// -----------------------------------------------------------------------
// AttendanceEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

internal sealed class AttendanceEntryAnalyzer : IAttendanceEntryAnalyzer
{
    private readonly List<PlayerAttend> _playersAttending = [];
    private readonly List<ZoneNameInfo> _zones = [];
    private RaidEntries _raidEntries;

    public void AnalyzeAttendanceCalls(LogParseResults logParseResults, RaidEntries raidEntries)
    {
        _raidEntries = raidEntries;
        PopulatePlayerAttends(logParseResults);
        PopulateZoneNames(logParseResults);

        AnalyzeLogFilesAttendanceCalls(logParseResults);
    }

    private void AddPlayersFromPlayersAttending(EqLogEntry logEntry, AttendanceEntry call)
    {
        foreach (PlayerAttend player in _playersAttending.Where(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp)))
        {
            call.AddOrMergeInPlayerCharacter(new PlayerCharacter { PlayerName = player.PlayerName });
        }
    }

    private void AddRaidDumpMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        RaidDumpFile raidDump = logParseResults.RaidDumpFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (raidDump != null)
        {
            foreach (PlayerCharacter player in raidDump.Characters)
            {
                call.AddOrMergeInPlayerCharacter(player);
            }
        }
    }

    private void AddRaidListMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        RaidListFile raidList = logParseResults.RaidListFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (raidList != null)
        {
            foreach (PlayerCharacter player in raidList.CharacterNames)
            {
                call.AddOrMergeInPlayerCharacter(player);
            }
        }
    }

    private void AnalyzeLogFilesAttendanceCalls(LogParseResults logParseResults)
    {
        // [Sun Mar 17 22:15:31 2024] You tell your raid, ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'
        // [Sun Mar 17 23:18:28 2024] You tell your raid, ':::Raid Attendance Taken:::Sister of the Spire:::Kill:::'

        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry logEntry in log.LogEntries)
            {
                if (logEntry.EntryType == LogEntryType.Attendance || logEntry.EntryType == LogEntryType.Kill)
                {
                    string correctedLogLine = CorrectDelimiter(logEntry.LogLine);
                    AttendanceEntry call = new() { Timestamp = logEntry.Timestamp, RawHeaderLogLine = logEntry.LogLine };

                    SetAttendanceType(logEntry, call, correctedLogLine);

                    if (IsRemoveCall(logEntry, call, correctedLogLine))
                        continue;

                    AddRaidDumpMembers(logParseResults, logEntry, call);

                    AddRaidListMembers(logParseResults, logEntry, call);

                    AddPlayersFromPlayersAttending(logEntry, call);

                    UpdatePlayerInfo(call);

                    SetZoneName(logEntry, call);

                    logEntry.Visited = true;
                    _raidEntries.AttendanceEntries.Add(call);
                }
            }
        }
    }

    private string CorrectDelimiter(string logLine)
    {
        logLine = logLine.Replace(Constants.PossibleErrorDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(Constants.TooLongDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(';', ':');
        return logLine;
    }

    private PlayerAttend ExtractAttendingPlayer(EqLogEntry entry)
    {
        // [Sun Mar 17 21:27:54 2024]  AFK [50 Bard] Grindcore (Half Elf) <Europa>
        // [Tue Mar 19 20:30:56 2024]  AFK [ANONYMOUS] Ecliptor  <Europa>
        // [Tue Mar 19 23:46:05 2024]  <LINKDEAD>[ANONYMOUS] Luwena  <Europa>
        // [Sun Mar 17 21:27:54 2024] [50 Monk] Pullz (Human) <Europa>
        // [Sun Mar 17 21:27:54 2024] [ANONYMOUS] Mendrik <Europa>
        // [Sat Mar 09 20:23:41 2024]  <LINKDEAD>[50 Rogue] Noggen (Dwarf) <Europa>

        string logLineNoTimestamp = entry.LogLine[(Constants.LogDateTimeLength + 1)..];
        int indexOfLastBracket = logLineNoTimestamp.LastIndexOf(']');
        if (indexOfLastBracket == -1)
        {
            // Should not reach here.
            Debug.Fail($"Reached a place in {nameof(ExtractAttendingPlayer)} that should not be reached. No ']'.  Logline: {entry.LogLine}");
            return new PlayerAttend { PlayerName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        int firstIndexOfEndMarker = logLineNoTimestamp.LastIndexOf('(');
        if (firstIndexOfEndMarker == -1)
        {
            firstIndexOfEndMarker = logLineNoTimestamp.LastIndexOf('<');
            if (firstIndexOfEndMarker == -1)
            {
                // Should not reach here.
                Debug.Fail($"Reached a place in {nameof(ExtractAttendingPlayer)} that should not be reached. No '(' or '<'.  Logline: {entry.LogLine}");
                return new PlayerAttend { PlayerName = "UNKNOWN", Timestamp = entry.Timestamp };
            }
        }

        string playerName = logLineNoTimestamp[(indexOfLastBracket + 1)..firstIndexOfEndMarker].Trim();
        entry.Visited = true;

        PlayerCharacter character = new() { PlayerName = playerName };

        if (logLineNoTimestamp.Contains(Constants.Anonymous))
        {
            _raidEntries.AddOrMergeInPlayerCharacter(character);
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        int indexOfLeadingClassBracket = logLineNoTimestamp.LastIndexOf('[');
        string classLevelString = logLineNoTimestamp[(indexOfLeadingClassBracket + 1)..indexOfLastBracket];
        string[] classAndLevel = classLevelString.Split(' ');

        int indexOfLastParens = logLineNoTimestamp.LastIndexOf(')');
        string race = logLineNoTimestamp[(firstIndexOfEndMarker + 1)..indexOfLastParens];

        character.ClassName = classAndLevel.Length > 2 ? string.Join(" ", classAndLevel[1], classAndLevel[2]) : classAndLevel[1];
        character.Level = int.Parse(classAndLevel[0]);
        character.Race = race;
        _raidEntries.AddOrMergeInPlayerCharacter(character);

        return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
    }

    private ZoneNameInfo ExtractZoneName(EqLogEntry entry)
    {
        // [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
        int indexOfPlayersIn = entry.LogLine.IndexOf(Constants.PlayersIn);
        int endIndexOfPlayersIn = indexOfPlayersIn + Constants.PlayersIn.Length;
        string zoneName = entry.LogLine[endIndexOfPlayersIn..^1].Trim();

        entry.Visited = true;

        return new() { ZoneName = zoneName, Timestamp = entry.Timestamp };
    }

    private AttendanceEntry GetAssociatedLogEntry(AttendanceEntry call)
    {
        AttendanceEntry lastOneFound = null;
        IEnumerable<AttendanceEntry> matchingAttendanceEntries =
            _raidEntries.AttendanceEntries.Where(x => x.RaidName == call.RaidName && x.AttendanceCallType == call.AttendanceCallType);

        foreach (AttendanceEntry entry in matchingAttendanceEntries)
        {
            if (entry.Timestamp < call.Timestamp)
                lastOneFound = entry;
        }

        return lastOneFound;
    }

    private bool IsRemoveCall(EqLogEntry logEntry, AttendanceEntry call, string logLine)
    {
        if (logLine.Contains(Constants.Undo) || logLine.Contains(Constants.Remove))
        {
            AttendanceEntry toBeRemoved = GetAssociatedLogEntry(call);
            if (toBeRemoved != null)
            {
                _raidEntries.AttendanceEntries.Remove(toBeRemoved);
            }
            logEntry.Visited = true;
            return true;
        }

        return false;
    }

    private void PopulatePlayerAttends(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<PlayerAttend> players = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.PlayerName)
                .Select(ExtractAttendingPlayer);

            _playersAttending.AddRange(players);
        }
    }

    private void PopulateZoneNames(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<ZoneNameInfo> zones = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.WhoZoneName)
                .Select(ExtractZoneName);

            _zones.AddRange(zones);
        }
    }

    private void SetAttendanceType(EqLogEntry logEntry, AttendanceEntry call, string logLine)
    {
        string[] splitEntry = logLine.Split(Constants.AttendanceDelimiter);
        if (logEntry.EntryType == LogEntryType.Attendance)
        {
            call.RaidName = splitEntry[3].Trim();
            call.AttendanceCallType = AttendanceCallType.Time;
        }
        else
        {
            call.RaidName = splitEntry[2].Trim();
            call.AttendanceCallType = AttendanceCallType.Kill;
        }
    }

    private void SetZoneName(EqLogEntry logEntry, AttendanceEntry call)
    {
        ZoneNameInfo zoneLogEntry = _zones.FirstOrDefault(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (zoneLogEntry != null)
        {
            call.ZoneName = zoneLogEntry.ZoneName;
        }
        else
        {
            //** Heuristic to find nearby zone name
            call.PossibleError = PossibleError.NoZoneName;
        }
    }

    private void UpdatePlayerInfo(AttendanceEntry call)
    {
        foreach (PlayerCharacter player in call.Players)
        {
            PlayerCharacter character = _raidEntries.AllPlayersInRaid.FirstOrDefault(x => x.PlayerName == player.PlayerName);
            if (character != null)
                player.Merge(character);
            else // Shouldnt reach here
                Debug.Fail($"No PlayerCharacter found in {nameof(UpdatePlayerInfo)}. Playername: {player.PlayerName}");
        }
    }

    [DebuggerDisplay("{DebugText}")]
    private sealed class PlayerAttend
    {
        public string PlayerName { get; init; }

        public DateTime Timestamp { get; init; }

        private string DebugText
            => $"{PlayerName} {Timestamp:HHmmss}";
    }

    [DebuggerDisplay("{DebugText}")]
    private sealed class ZoneNameInfo
    {
        public DateTime Timestamp { get; init; }

        public string ZoneName { get; init; }

        private string DebugText
            => $"{ZoneName} {Timestamp:HHmmss}";
    }
}

public interface IAttendanceEntryAnalyzer
{
    void AnalyzeAttendanceCalls(LogParseResults logParseResults, RaidEntries raidEntries);
}
