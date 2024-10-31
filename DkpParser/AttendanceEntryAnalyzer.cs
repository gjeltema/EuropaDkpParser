// -----------------------------------------------------------------------
// AttendanceEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

internal sealed class AttendanceEntryAnalyzer : IAttendanceEntryAnalyzer
{
    private static readonly TimeSpan fifteenMinutes = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan tenMinutes = TimeSpan.FromMinutes(10);
    private readonly List<CharacterAttend> _charactersAttending = [];
    private readonly DelimiterStringSanitizer _sanitizer = new();
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
        PopulateCharacterAttends(logParseResults);
        PopulateZoneNames(logParseResults);

        AnalyzeLogFilesAttendanceCalls(logParseResults);
        HandleCrashedEntries(logParseResults);
        HandleAfkTags(logParseResults);
    }

    private void AddCharactersFromCharactersAttending(EqLogEntry logEntry, AttendanceEntry call)
    {
        foreach (CharacterAttend character in _charactersAttending.Where(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp)))
        {
            call.AddOrMergeInPlayerCharacter(new PlayerCharacter { CharacterName = character.CharacterName });
            if (character.IsAfk)
            {
                PlayerCharacter afkCharacter = call.Characters.FirstOrDefault(x => x.CharacterName == character.CharacterName);
                if (afkCharacter != null)
                {
                    call.AfkPlayers.Add(afkCharacter);
                }
            }
        }
    }

    private void AddRaidDumpMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        RaidDumpFile raidDump = logParseResults.RaidDumpFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (raidDump == null)
            return;

        foreach (PlayerCharacter character in raidDump.Characters)
        {
            call.AddOrMergeInPlayerCharacter(character);
        }
    }

    private void AddRaidListMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        RaidListFile raidList = logParseResults.RaidListFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
        if (raidList == null)
            return;

        foreach (PlayerCharacter character in raidList.CharacterNames)
        {
            call.AddOrMergeInPlayerCharacter(character);
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
                logEntry.Visited = true;

                try
                {
                    string correctedLogLine = _sanitizer.SanitizeDelimiterString(logEntry.LogLine);
                    AttendanceEntry call = new() { Timestamp = logEntry.Timestamp, RawHeaderLogLine = logEntry.LogLine };

                    SetAttendanceType(logEntry, call, correctedLogLine);

                    if (IsRemoveCall(logEntry, call, correctedLogLine))
                        continue;

                    AddRaidDumpMembers(logParseResults, logEntry, call);

                    AddRaidListMembers(logParseResults, logEntry, call);

                    AddCharactersFromCharactersAttending(logEntry, call);

                    UpdateCharacterInfo(call);

                    SetZoneName(logEntry, call);

                    if (call.Characters.Count > 1)
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

    private void CreateAfkEntries(List<AfkEntryInfo> afkCharacterEntries)
    {
        foreach (AfkEntryInfo afkStartEntry in afkCharacterEntries.Where(x => x.EntryType == LogEntryType.AfkStart))
        {
            AfkEntryInfo endEntry = afkCharacterEntries
                .Where(x => x.EntryType == LogEntryType.AfkEnd)
                .Where(x => afkStartEntry.Timestamp < x.Timestamp)
                .Where(x => x.Character.CharacterName == afkStartEntry.Character.CharacterName)
                .FirstOrDefault();

            DateTime endTime = endEntry?.Timestamp ?? DateTime.MaxValue;

            AfkEntry afk = new()
            {
                Character = afkStartEntry.Character,
                StartTime = afkStartEntry.Timestamp,
                EndTime = endTime,
                LogLine = afkStartEntry.LogLine
            };

            _raidEntries.AfkEntries.Add(afk);
        }
    }

    private PlayerCharacter ExtractAfkStartOrEndCharacter(EqLogEntry logEntry)
    {
        // [Thu Mar 07 21:33:39 2024] Undertree tells the raid,  ':::AFK:::'
        // [Thu Mar 07 21:33:39 2024] Undertree tells the raid,  ':::AFKEND:::'

        if (logEntry.LogLine.Length < Constants.LogDateTimeLength + Constants.AfkStart.Length + Constants.RaidOther.Length + 5)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get character name in {Constants.AfkStart} or {Constants.AfkEnd} entry: {logEntry.LogLine}");
            return null;
        }

        string linePastTimestamp = logEntry.LogLine[(Constants.LogDateTimeLength + 1)..];
        string[] parts = linePastTimestamp.Split(' ');
        if (parts.Length < 4)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get character name in {Constants.AfkStart} or {Constants.AfkEnd} entry: {logEntry.LogLine}");
            return null;
        }

        string characterName = parts[0].Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            _raidEntries.AnalysisErrors.Add($"Unable get character name in {Constants.AfkStart} or {Constants.AfkEnd} entry: {logEntry.LogLine}");
            return null;
        }

        PlayerCharacter character = _raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == characterName);
        return character;
    }

    private CharacterAttend ExtractAttendingCharacter(EqLogEntry entry)
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
            Debug.Fail($"Reached a place in {nameof(ExtractAttendingCharacter)} that should not be reached. No ']'.  Logline: {entry.LogLine}");
            _raidEntries.AnalysisErrors.Add($"Unable to extract attending character. No ']'.: {entry.LogLine}");
            return new CharacterAttend { CharacterName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        int firstIndexOfEndMarker = logLineNoTimestamp.LastIndexOf('(');
        if (firstIndexOfEndMarker < 0)
        {
            firstIndexOfEndMarker = logLineNoTimestamp.LastIndexOf('<');
            if (firstIndexOfEndMarker < 0)
            {
                // Should not reach here.
                Debug.Fail($"Reached a place in {nameof(ExtractAttendingCharacter)} that should not be reached. No '(' or '<'.  Logline: {entry.LogLine}");
                _raidEntries.AnalysisErrors.Add($"Unable to extract attending character. No '(' or '<'.: {entry.LogLine}");
                return new CharacterAttend { CharacterName = "UNKNOWN", Timestamp = entry.Timestamp };
            }
        }

        string characterName = logLineNoTimestamp[(indexOfLastBracket + 1)..firstIndexOfEndMarker].Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get character name from 'who' entry: {entry.LogLine}");
            return new CharacterAttend { CharacterName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        PlayerCharacter character = new() { CharacterName = characterName };

        if (logLineNoTimestamp.Contains(Constants.AnonWithBrackets))
        {
            _raidEntries.AddOrMergeInPlayerCharacter(character);
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        int indexOfLeadingClassBracket = logLineNoTimestamp.LastIndexOf('[');
        if (indexOfLeadingClassBracket < 0)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to find leading '[' in 'who' entry: {entry.LogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        string classLevelString = logLineNoTimestamp[(indexOfLeadingClassBracket + 1)..indexOfLastBracket];
        if (string.IsNullOrWhiteSpace(classLevelString))
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get class and level in 'who' entry: {entry.LogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        string[] classAndLevel = classLevelString.Split(' ');
        if (classAndLevel.Length < 2)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get class and level in 'who' entry: {entry.LogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        int indexOfLastParens = logLineNoTimestamp.LastIndexOf(')');
        if (indexOfLastParens < 0)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to find trailing ')' in 'who' entry: {entry.LogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        string race = logLineNoTimestamp[(firstIndexOfEndMarker + 1)..indexOfLastParens];
        if (string.IsNullOrWhiteSpace(race))
        {
            _raidEntries.AnalysisErrors.Add($"Unable to get race in 'who' entry: {entry.LogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        character.ClassName = classAndLevel.Length > 2 ? string.Join(" ", classAndLevel[1], classAndLevel[2]) : classAndLevel[1];
        character.Level = int.Parse(classAndLevel[0]);
        character.Race = race;
        _raidEntries.AddOrMergeInPlayerCharacter(character);

        bool isAfk = logLineNoTimestamp.Contains(Constants.Afk);
        return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp, IsAfk = isAfk };
    }

    private PlayerCharacter ExtractCrashedCharacter(EqLogEntry logEntry)
    {
        // [Thu Mar 07 21:33:39 2024] Undertree tells the raid,  ':::CRASHED:::'

        if (logEntry.LogLine.Length < Constants.LogDateTimeLength + Constants.Crashed.Length + Constants.RaidOther.Length + 5)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get character name in CRASHED entry: {logEntry.LogLine}");
            return null;
        }

        string linePastTimestamp = logEntry.LogLine[(Constants.LogDateTimeLength + 1)..];
        string[] parts = linePastTimestamp.Split(' ');
        if (parts.Length < 4)
        {
            _raidEntries.AnalysisErrors.Add($"Unable get character name in CRASHED entry: {logEntry.LogLine}");
            return null;
        }

        string characterName = parts[0].Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            _raidEntries.AnalysisErrors.Add($"Unable get character name in CRASHED entry: {logEntry.LogLine}");
            return null;
        }

        PlayerCharacter character = _raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == characterName);
        return character;
    }

    private ZoneNameInfo ExtractZoneName(EqLogEntry entry)
    {
        entry.Visited = true;

        bool isMultiplePlayers = true;

        // [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
        int indexOfPlayersIn = entry.LogLine.IndexOf(Constants.PlayersIn);
        if (indexOfPlayersIn < Constants.LogDateTimeLength + Constants.PlayersIn.Length)
        {
            indexOfPlayersIn = entry.LogLine.IndexOf(Constants.PlayerIn);
            if (indexOfPlayersIn < Constants.LogDateTimeLength + Constants.PlayerIn.Length)
            {
                _raidEntries.AnalysisErrors.Add($"Unable get zone name: {entry.LogLine}");
                return null;
            }

            isMultiplePlayers = false;
        }

        int playersInLength = isMultiplePlayers ? Constants.PlayersIn.Length : Constants.PlayerIn.Length;
        int endIndexOfPlayersIn = indexOfPlayersIn + playersInLength;
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

    private List<AfkEntryInfo> GetAfkDeclarationEntries(LogParseResults logParseResults)
    {
        List<AfkEntryInfo> afkCharacterEntries = [];

        foreach (EqLogFile logFile in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry logEntry in logFile.LogEntries.Where(x => x.EntryType == LogEntryType.AfkStart || x.EntryType == LogEntryType.AfkEnd))
            {
                try
                {
                    logEntry.Visited = true;

                    PlayerCharacter afkCharacter = ExtractAfkStartOrEndCharacter(logEntry);
                    if (afkCharacter == null)
                        continue;

                    AfkEntryInfo characterAfkEntry = new() { Character = afkCharacter, EntryType = logEntry.EntryType, Timestamp = logEntry.Timestamp, LogLine = logEntry.LogLine };

                    afkCharacterEntries.Add(characterAfkEntry);
                }
                catch (Exception ex)
                {
                    _raidEntries.AnalysisErrors.Add(
                        $"An unexpected error occurred when analyzing an {Constants.AfkStart} or {Constants.AfkEnd} call: {logEntry.LogLine}{Environment.NewLine}    {ex}");
                }
            }
        }

        afkCharacterEntries = afkCharacterEntries.OrderBy(x => x.Timestamp).ToList();
        return afkCharacterEntries;
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

    private void HandleAfkTags(LogParseResults logParseResults)
    {
        List<AfkEntryInfo> afkCharacterEntries = GetAfkDeclarationEntries(logParseResults);

        CreateAfkEntries(afkCharacterEntries);

        foreach (AfkEntry afk in _raidEntries.AfkEntries)
        {
            IEnumerable<AttendanceEntry> relatedAttendances = _raidEntries.AttendanceEntries.Where(x => afk.StartTime <= x.Timestamp && x.Timestamp <= afk.EndTime);
            foreach (AttendanceEntry attendance in relatedAttendances)
            {
                attendance.Characters.Remove(afk.Character);
            }
        }
    }

    private void HandleCrashedEntries(LogParseResults logParseResults)
    {
        foreach (EqLogFile logFile in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry crashedLogEntry in logFile.LogEntries.Where(x => x.EntryType == LogEntryType.Crashed))
            {
                try
                {
                    // Add character to any log entries within 10 minutes prior to the :::CRASHED::: message
                    // (or up to 15 minutes prior if a "Player has left the raid" is found in that time period)
                    crashedLogEntry.Visited = true;

                    PlayerCharacter crashedCharacter = ExtractCrashedCharacter(crashedLogEntry);
                    if (crashedCharacter == null)
                    {
                        _raidEntries.AnalysisErrors.Add($"Unable to extract character name when analyzing a CRASHED call: {crashedLogEntry.LogLine}");
                        continue;
                    }

                    DateTime startTimestamp = crashedLogEntry.Timestamp - tenMinutes;

                    // Get previous "Player has left the raid" entry
                    CharacterJoinRaidEntry previousLeaveEntry = _raidEntries.CharacterJoinCalls
                        .Where(x => x.EntryType == LogEntryType.LeftRaid && x.CharacterName == crashedCharacter.CharacterName && x.Timestamp < crashedLogEntry.Timestamp)
                        .MaxBy(x => x.Timestamp);

                    if (previousLeaveEntry != null)
                    {
                        // If the last "Player has left the raid" entry is less than 15 minutes in the past, use that timestamp
                        bool isLessThan15Minutes = (crashedLogEntry.Timestamp - previousLeaveEntry.Timestamp) < fifteenMinutes;
                        if (isLessThan15Minutes)
                        {
                            startTimestamp = previousLeaveEntry.Timestamp;
                        }
                    }

                    IEnumerable<AttendanceEntry> missingFromAttendanceEnties = _raidEntries.AttendanceEntries
                        .Where(x => startTimestamp <= x.Timestamp && x.Timestamp < crashedLogEntry.Timestamp);

                    foreach (AttendanceEntry entry in missingFromAttendanceEnties)
                    {
                        entry.AddOrMergeInPlayerCharacter(crashedCharacter);
                    }
                }
                catch (Exception ex)
                {
                    _raidEntries.AnalysisErrors.Add($"An unexpected error occurred when analyzing a CRASHED call: {crashedLogEntry.LogLine}{Environment.NewLine}    {ex}");
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

    private void PopulateCharacterAttends(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<CharacterAttend> characters = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.CharacterName)
                .Select(ExtractAttendingCharacter);

            _charactersAttending.AddRange(characters);
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

    private void UpdateCharacterInfo(AttendanceEntry call)
    {
        foreach (PlayerCharacter characterInCall in call.Characters)
        {
            PlayerCharacter characterInRaid = _raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == characterInCall.CharacterName);
            if (characterInRaid != null)
            {
                characterInCall.Merge(characterInRaid);
            }
            else // Shouldnt reach here
            {
                Debug.Fail($"No PlayerCharacter found in {nameof(UpdateCharacterInfo)}. Character Name: {characterInCall.CharacterName}");
                _raidEntries.AnalysisErrors.Add($"No character found in AllCharacters collection to update character info: {characterInCall.CharacterName}");
            }
        }
    }

    [DebuggerDisplay("{DebugDisplay}")]
    private sealed class AfkEntryInfo
    {
        public PlayerCharacter Character { get; init; }

        public LogEntryType EntryType { get; init; }

        public string LogLine { get; init; }

        public DateTime Timestamp { get; init; }

        private string DebugText
        => $"{Character.CharacterName} {EntryType} {Timestamp:HH:mm:ss}";
    }

    [DebuggerDisplay("{DebugText}")]
    private sealed class CharacterAttend
    {
        public string CharacterName { get; init; }

        public bool IsAfk { get; init; }

        public DateTime Timestamp { get; init; }

        private string DebugText
            => $"{CharacterName} {Timestamp:HHmmss}";
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
