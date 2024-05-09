// -----------------------------------------------------------------------
// AttendanceEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

internal sealed class AttendanceEntryAnalyzer : IAttendanceEntryAnalyzer
{
    private static readonly TimeSpan thirtyMinutes = TimeSpan.FromMinutes(30);
    private readonly List<PlayerAttend> _playersAttending = [];
    private readonly IDkpParserSettings _settings;
    private readonly List<ZoneNameInfo> _zones = [];
    private RaidEntries _raidEntries;

    public AttendanceEntryAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public void AnalyzeAttendanceCalls(LogParseResults logParseResults, RaidEntries raidEntries)
    {
        _raidEntries = raidEntries;
        PopulatePlayerAttends(logParseResults);
        PopulateZoneNames(logParseResults);

        AnalyzeLogFilesAttendanceCalls(logParseResults);
        HandleCrashedEntries(logParseResults);
    }

    private void AddPlayersFromPlayersAttending(EqLogEntry logEntry, AttendanceEntry call)
    {
        foreach (PlayerAttend player in _playersAttending.Where(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp)))
        {
            call.AddOrMergeInPlayerCharacter(new PlayerCharacter { PlayerName = player.PlayerName });
            if (player.IsAfk)
            {
                PlayerCharacter afkPlayer = call.Players.FirstOrDefault(x => x.PlayerName == player.PlayerName);
                if (afkPlayer != null)
                {
                    call.AfkPlayers.Add(afkPlayer);
                }
            }
        }
    }

    private void AddRaidDumpMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        RaidDumpFile raidDump = logParseResults.RaidDumpFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (raidDump == null)
            return;

        foreach (PlayerCharacter player in raidDump.Characters)
        {
            call.AddOrMergeInPlayerCharacter(player);
        }
    }

    private void AddRaidListMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        RaidListFile raidList = logParseResults.RaidListFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (raidList == null)
            return;

        foreach (PlayerCharacter player in raidList.CharacterNames)
        {
            call.AddOrMergeInPlayerCharacter(player);
        }
    }

    private void AnalyzeLogFilesAttendanceCalls(LogParseResults logParseResults)
    {
        // [Sun Mar 17 22:15:31 2024] You tell your raid, ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'
        // [Sun Mar 17 23:18:28 2024] You tell your raid, ':::Raid Attendance Taken:::Sister of the Spire:::Kill:::'

        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry logEntry in log.LogEntries.Where(x => x.EntryType == LogEntryType.Attendance || x.EntryType == LogEntryType.Kill))
            {
                try
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

                    if (call.Players.Count > 1)
                        _raidEntries.AttendanceEntries.Add(call);
                }
                catch (Exception ex)
                {
                    EuropaDkpParserException eex = new("An unexpected error occurred when analyzing an attendance call.", logEntry.LogLine, ex);
                    throw eex;
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
        entry.Visited = true;

        string logLineNoTimestamp = entry.LogLine[(Constants.LogDateTimeLength + 1)..];
        int indexOfLastBracket = logLineNoTimestamp.LastIndexOf(']');
        if (indexOfLastBracket < 0)
        {
            // Should not reach here.
            Debug.Fail($"Reached a place in {nameof(ExtractAttendingPlayer)} that should not be reached. No ']'.  Logline: {entry.LogLine}");
            _raidEntries.AnalysisErrors.Add($"Unable to extract attending player. No ']'.: {entry.LogLine}");
            return new PlayerAttend { PlayerName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        int firstIndexOfEndMarker = logLineNoTimestamp.LastIndexOf('(');
        if (firstIndexOfEndMarker < 0)
        {
            firstIndexOfEndMarker = logLineNoTimestamp.LastIndexOf('<');
            if (firstIndexOfEndMarker < 0)
            {
                // Should not reach here.
                Debug.Fail($"Reached a place in {nameof(ExtractAttendingPlayer)} that should not be reached. No '(' or '<'.  Logline: {entry.LogLine}");
                _raidEntries.AnalysisErrors.Add($"Unable to extract attending player. No '(' or '<'.: {entry.LogLine}");
                return new PlayerAttend { PlayerName = "UNKNOWN", Timestamp = entry.Timestamp };
            }
        }

        string playerName = logLineNoTimestamp[(indexOfLastBracket + 1)..firstIndexOfEndMarker].Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get player name from 'who' entry: {entry.LogLine}");
            return new PlayerAttend { PlayerName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        PlayerCharacter character = new() { PlayerName = playerName };

        if (logLineNoTimestamp.Contains(Constants.AnonWithBrackets))
        {
            _raidEntries.AddOrMergeInPlayerCharacter(character);
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        int indexOfLeadingClassBracket = logLineNoTimestamp.LastIndexOf('[');
        if (indexOfLeadingClassBracket < 0)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to find leading '[' in 'who' entry: {entry.LogLine}");
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        string classLevelString = logLineNoTimestamp[(indexOfLeadingClassBracket + 1)..indexOfLastBracket];
        if (string.IsNullOrWhiteSpace(classLevelString))
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get class and level in 'who' entry: {entry.LogLine}");
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        string[] classAndLevel = classLevelString.Split(' ');
        if (classAndLevel.Length < 2)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get class and level in 'who' entry: {entry.LogLine}");
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        int indexOfLastParens = logLineNoTimestamp.LastIndexOf(')');
        if (indexOfLastParens < 0)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to find trailing ')' in 'who' entry: {entry.LogLine}");
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        string race = logLineNoTimestamp[(firstIndexOfEndMarker + 1)..indexOfLastParens];
        if (string.IsNullOrWhiteSpace(race))
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get race in 'who' entry: {entry.LogLine}");
            return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
        }

        character.ClassName = classAndLevel.Length > 2 ? string.Join(" ", classAndLevel[1], classAndLevel[2]) : classAndLevel[1];
        character.Level = int.Parse(classAndLevel[0]);
        character.Race = race;
        _raidEntries.AddOrMergeInPlayerCharacter(character);

        bool isAfk = logLineNoTimestamp.Contains(Constants.Afk);
        return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp, IsAfk = isAfk };
    }

    private PlayerCharacter ExtractCrashedPlayer(EqLogEntry logEntry)
    {
        // [Thu Mar 07 21:33:39 2024] Undertree tells the raid,  ':::CRASHED:::'

        if (logEntry.LogLine.Length < Constants.LogDateTimeLength + 2)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get player name in CRASHED entry: {logEntry.LogLine}");
            return null;
        }

        string linePastTimestamp = logEntry.LogLine[(Constants.LogDateTimeLength + 1)..];
        string[] parts = linePastTimestamp.Split(' ');
        if (parts.Length < 1)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get player name in CRASHED entry: {logEntry.LogLine}");
            return null;
        }

        string playerName = parts[0].Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            _raidEntries.AnalysisErrors.Add($"Unable get player name in CRASHED entry: {logEntry.LogLine}");
            return null;
        }

        PlayerCharacter character = _raidEntries.AllPlayersInRaid.FirstOrDefault(x => x.PlayerName == playerName);
        return character;
    }

    private ZoneNameInfo ExtractZoneName(EqLogEntry entry)
    {
        entry.Visited = true;

        // [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
        int indexOfPlayersIn = entry.LogLine.IndexOf(Constants.PlayersIn);
        if (indexOfPlayersIn < Constants.LogDateTimeLength + Constants.PlayersIn.Length)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get zone name: {entry.LogLine}");
            return null;
        }

        int endIndexOfPlayersIn = indexOfPlayersIn + Constants.PlayersIn.Length;
        if (endIndexOfPlayersIn + Constants.MinimumRaidNameLength > entry.LogLine.Length)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get zone name: {entry.LogLine}");
            return null;
        }

        string zoneName = entry.LogLine[endIndexOfPlayersIn..^1].Trim();
        if (string.IsNullOrEmpty(zoneName))
        {
            _raidEntries.AnalysisErrors.Add($"Unable get zone name: {entry.LogLine}");
            return null;
        }

        return new() { ZoneName = zoneName, Timestamp = entry.Timestamp };
    }

    private AttendanceEntry GetAssociatedLogEntry(AttendanceEntry call)
    {
        AttendanceEntry lastOneFound = null;
        IEnumerable<AttendanceEntry> matchingAttendanceEntries =
            _raidEntries.AttendanceEntries.Where(x => x.CallName == call.CallName && x.AttendanceCallType == call.AttendanceCallType);

        foreach (AttendanceEntry entry in matchingAttendanceEntries)
        {
            if (entry.Timestamp < call.Timestamp)
                lastOneFound = entry;
        }

        return lastOneFound;
    }

    private void HandleCrashedEntries(LogParseResults logParseResults)
    {
        foreach (EqLogFile logFile in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry logEntry in logFile.LogEntries.Where(x => x.EntryType == LogEntryType.Crashed))
            {
                try
                {
                    logEntry.Visited = true;

                    PlayerCharacter crashedPlayer = ExtractCrashedPlayer(logEntry);
                    if (crashedPlayer == null)
                        continue;

                    PlayerJoinRaidEntry previousLeaveEntry = _raidEntries.PlayerJoinCalls
                        .Where(x => x.EntryType == LogEntryType.LeftRaid && x.PlayerName == crashedPlayer.PlayerName && x.Timestamp < logEntry.Timestamp)
                        .MaxBy(x => x.Timestamp);

                    if (previousLeaveEntry == null)
                    {
                        _raidEntries.AnalysisErrors.Add($"Unable get find previous 'left raid' entry for CRASHED entry: {logEntry.LogLine}");
                        continue;
                    }

                    DateTime startTimestamp = previousLeaveEntry.Timestamp;
                    bool isMoreThan30Minutes = (logEntry.Timestamp - previousLeaveEntry.Timestamp) > thirtyMinutes;
                    if (isMoreThan30Minutes)
                    {
                        AttendanceEntry previousTimeAttendance = _raidEntries.AttendanceEntries
                            .Where(x => x.AttendanceCallType == AttendanceCallType.Time
                                            && x.Timestamp < logEntry.Timestamp
                                            && (logEntry.Timestamp - x.Timestamp) < thirtyMinutes)
                            .MaxBy(x => x.Timestamp);

                        if (previousLeaveEntry == null)
                        {
                            _raidEntries.AnalysisErrors.Add($"Unable get find previous time attendance for CRASHED entry: {logEntry.LogLine}");
                            continue;
                        }

                        startTimestamp = previousTimeAttendance.Timestamp;
                    }

                    IEnumerable<AttendanceEntry> missingFromAttendanceEnties = _raidEntries.AttendanceEntries
                        .Where(x => startTimestamp <= x.Timestamp && x.Timestamp < logEntry.Timestamp);

                    foreach (AttendanceEntry entry in missingFromAttendanceEnties)
                    {
                        entry.AddOrMergeInPlayerCharacter(crashedPlayer);
                    }
                }
                catch (Exception ex)
                {
                    EuropaDkpParserException eex = new("An unexpected error occurred when analyzing a CRASHED call.", logEntry.LogLine, ex);
                    throw eex;
                }
            }
        }
    }

    private bool IsRemoveCall(EqLogEntry logEntry, AttendanceEntry call, string logLine)
    {
        if (logLine.Contains(Constants.Undo) || logLine.Contains(Constants.Remove))
        {
            logEntry.Visited = true;

            AttendanceEntry toBeRemoved = GetAssociatedLogEntry(call);
            if (toBeRemoved != null)
            {
                _raidEntries.AttendanceEntries.Remove(toBeRemoved);
            }

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
                .Select(ExtractZoneName)
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ZoneName));

            _zones.AddRange(zones);
        }
    }

    private void SetAttendanceType(EqLogEntry logEntry, AttendanceEntry call, string logLine)
    {
        string[] splitEntry = logLine.Split(Constants.AttendanceDelimiter);
        if (logEntry.EntryType == LogEntryType.Attendance)
        {
            call.AttendanceCallType = AttendanceCallType.Time;

            if (splitEntry.Length < 4)
            {
                _raidEntries.AnalysisErrors.Add($"Unable get get raid name from Time attendance entry: {logEntry.LogLine}");
                return;
            }
            call.CallName = splitEntry[3].Trim();
            if (string.IsNullOrEmpty(call.CallName))
            {
                _raidEntries.AnalysisErrors.Add($"Unable get get raid name from Time attendance entry: {logEntry.LogLine}");
            }
        }
        else
        {
            call.AttendanceCallType = AttendanceCallType.Kill;

            if (splitEntry.Length < 3)
            {
                _raidEntries.AnalysisErrors.Add($"Unable get get raid name from Kill attendance entry: {logEntry.LogLine}");
                return;
            }
            call.CallName = splitEntry[2].Trim();
            if (string.IsNullOrEmpty(call.CallName))
            {
                _raidEntries.AnalysisErrors.Add($"Unable get get raid name from Kill attendance entry: {logEntry.LogLine}");
            }
        }
    }

    private void SetZoneName(EqLogEntry logEntry, AttendanceEntry call)
    {
        ZoneNameInfo zoneLogEntry = _zones.FirstOrDefault(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (zoneLogEntry != null)
        {
            call.ZoneName = zoneLogEntry.ZoneName;
            if (!_settings.RaidValue.AllValidRaidZoneNames.Contains(zoneLogEntry.ZoneName))
            {
                call.PossibleError = PossibleError.InvalidZoneName;
            }
        }
        else
        {
            call.PossibleError = PossibleError.NoZoneName;
            call.ZoneName = string.Empty;
            _raidEntries.AnalysisErrors.Add($"Unable get get zone name from attendance entry: {logEntry.LogLine}");
        }
    }

    private void UpdatePlayerInfo(AttendanceEntry call)
    {
        foreach (PlayerCharacter player in call.Players)
        {
            PlayerCharacter character = _raidEntries.AllPlayersInRaid.FirstOrDefault(x => x.PlayerName == player.PlayerName);
            if (character != null)
            {
                player.Merge(character);
            }
            else // Shouldnt reach here
            {
                Debug.Fail($"No PlayerCharacter found in {nameof(UpdatePlayerInfo)}. Playername: {player.PlayerName}");
                _raidEntries.AnalysisErrors.Add($"No player character found in AllPlayers collection to update player info: {player.PlayerName}");
            }
        }
    }

    [DebuggerDisplay("{DebugText}")]
    private sealed class PlayerAttend
    {
        public bool IsAfk { get; init; }

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
