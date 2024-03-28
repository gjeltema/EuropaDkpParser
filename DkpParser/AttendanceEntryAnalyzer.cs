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

        // [Sun Mar 17 22:15:31 2024] You tell your raid, ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'
        // [Sun Mar 17 23:18:28 2024] You tell your raid, ':::Raid Attendance Taken:::Sister of the Spire:::Kill:::'
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry logEntry in log.LogEntries)
            {
                if (logEntry.EntryType == LogEntryType.Attendance || logEntry.EntryType == LogEntryType.Kill)
                {
                    string logLine = CorrectDelimiter(logEntry.LogLine);
                    string[] splitEntry = logLine.Split(Constants.AttendanceDelimiter);

                    AttendanceEntry call = new() { Timestamp = logEntry.Timestamp };
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

                    if (logLine.Contains(Constants.Undo) || logLine.Contains(Constants.Remove))
                    {
                        AttendanceEntry toBeRemoved = GetAssociatedLogEntry(call);
                        if (toBeRemoved != null)
                        {
                            _raidEntries.AttendanceEntries.Remove(toBeRemoved);
                        }
                        logEntry.Visited = true;
                        continue;
                    }

                    RaidDumpFile raidDump = logParseResults.RaidDumpFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
                    if (raidDump != null)
                    {
                        foreach (PlayerCharacter player in raidDump.Characters)
                        {
                            call.AddOrMergeInPlayerCharacter(player);
                        }
                    }

                    RaidListFile raidList = logParseResults.RaidListFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
                    if (raidList != null)
                    {
                        foreach (PlayerCharacter player in raidList.CharacterNames)
                        {
                            call.AddOrMergeInPlayerCharacter(player);
                        }
                    }

                    foreach (PlayerAttend player in _playersAttending.Where(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp)))
                    {
                        call.AddOrMergeInPlayerCharacter(new PlayerCharacter { PlayerName = player.PlayerName });
                    }

                    foreach (PlayerCharacter player in call.Players)
                    {
                        PlayerCharacter character = _raidEntries.AllPlayersInRaid.FirstOrDefault(x => x.PlayerName == player.PlayerName);
                        player.Merge(character);
                    }

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

                    logEntry.Visited = true;
                    call.RawHeaderLogLine = logEntry.LogLine;
                    _raidEntries.AttendanceEntries.Add(call);
                }
            }
        }
    }

    private string CorrectDelimiter(string logLine)
    {
        logLine = logLine.Replace(Constants.PossibleErrorDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(Constants.TooLongDelimiter, Constants.AttendanceDelimiter);
        return logLine;
    }

    private PlayerAttend ExtractAttendingPlayerName(EqLogEntry entry)
    {
        // [Sun Mar 17 21:27:54 2024]  AFK [50 Bard] Grindcore (Half Elf) <Europa>
        // [Tue Mar 19 20:30:56 2024]  AFK [ANONYMOUS] Ecliptor  <Europa>
        // [Tue Mar 19 23:46:05 2024]  <LINKDEAD>[ANONYMOUS] Luwena  <Europa>
        // [Sun Mar 17 21:27:54 2024] [50 Monk] Pullz (Human) <Europa>
        // [Sun Mar 17 21:27:54 2024] [ANONYMOUS] Mendrik <Europa>
        // [Sat Mar 09 20:23:41 2024]  <LINKDEAD>[50 Rogue] Noggen (Dwarf) <Europa>

        string logLine = entry.LogLine;
        int indexOfLastBracket = logLine.LastIndexOf(']');
        if (indexOfLastBracket == -1)
        {
            // Should not reach here.
            Debug.Fail($"Reached a place in {nameof(ExtractAttendingPlayerName)} that should not be reached. No ']'.  Logline: {logLine}");
            return new PlayerAttend { PlayerName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        int firstIndexOfEndMarker = logLine.LastIndexOf('(');
        if (firstIndexOfEndMarker == -1)
        {
            firstIndexOfEndMarker = logLine.LastIndexOf('<');
            if (firstIndexOfEndMarker == -1)
            {
                // Should not reach here.
                Debug.Fail($"Reached a place in {nameof(ExtractAttendingPlayerName)} that should not be reached. No '(' or '<'.  Logline: {logLine}");
                return new PlayerAttend { PlayerName = "UNKNOWN", Timestamp = entry.Timestamp };
            }
        }

        string playerName = logLine[(indexOfLastBracket + 1)..firstIndexOfEndMarker].Trim();
        entry.Visited = true;

        PlayerCharacter character = new() { PlayerName = playerName };

        if (logLine.Contains(Constants.Anonymous))
        {
            _raidEntries.AddOrMergeInPlayerCharacter(character);
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        int indexOfLeadingClassBracket = logLine.LastIndexOf('[');
        string classLevelString = logLine[(indexOfLeadingClassBracket + 1)..indexOfLastBracket];
        string[] classAndLevel = classLevelString.Split(' ');

        int indexOfLastParens = logLine.LastIndexOf(')');
        string race = logLine[(firstIndexOfEndMarker + 1)..indexOfLastParens];

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
        string zoneName = entry.LogLine[endIndexOfPlayersIn..^1];

        entry.Visited = true;

        return new() { ZoneName = zoneName, Timestamp = entry.Timestamp };
    }

    private AttendanceEntry GetAssociatedLogEntry(AttendanceEntry call)
    {
        AttendanceEntry lastOneFound = null;
        foreach (AttendanceEntry entry in _raidEntries.AttendanceEntries.Where(x => x.RaidName == call.RaidName && x.AttendanceCallType == call.AttendanceCallType))
        {
            if (entry.Timestamp < call.Timestamp)
                lastOneFound = entry;
        }

        return lastOneFound;
    }

    private void PopulatePlayerAttends(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<PlayerAttend> players = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.PlayerName)
                .Select(ExtractAttendingPlayerName);

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

    [DebuggerDisplay("{DebugText}")]
    private sealed class PlayerAttend
    {
        public string PlayerName { get; init; }

        public DateTime Timestamp { get; init; }

        private string DebugText
            => $"{PlayerName} {Timestamp:HHmmss}";
    }

    [DebuggerDisplay("{ZoneName,nq}, {Timestamp,nq}")]
    private sealed class ZoneNameInfo
    {
        public DateTime Timestamp { get; init; }

        public string ZoneName { get; init; }
    }
}

public interface IAttendanceEntryAnalyzer
{
    void AnalyzeAttendanceCalls(LogParseResults logParseResults, RaidEntries raidEntries);
}
