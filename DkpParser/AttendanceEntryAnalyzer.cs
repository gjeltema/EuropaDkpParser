// -----------------------------------------------------------------------
// AttendanceEntryAnalyzer.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gjeltema.Logging;

internal sealed class AttendanceEntryAnalyzer : IAttendanceEntryAnalyzer
{
    private const string AfkFlag = " AFK ";
    private const string LogPrefix = $"[{nameof(AttendanceEntryAnalyzer)}]";
    private static readonly TimeSpan crashedLeftRaidThresholdInMinutes = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan defaultCrashedThresholdInMinutes = TimeSpan.FromMinutes(10);
    private readonly List<CharacterAttend> _charactersAttending = [];
    private readonly DelimiterStringSanitizer _sanitizer = new();
    private readonly IDkpParserSettings _settings;
    private readonly List<ZoneNameInfo> _zonePerAttendance = [];
    private RaidEntries _raidEntries;

    public AttendanceEntryAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public void AnalyzeAttendanceCalls(LogParseResults logParseResults, RaidEntries raidEntries)
    {
        Log.Debug($"{LogPrefix} Starting {nameof(AnalyzeAttendanceCalls)}");

        _raidEntries = raidEntries;
        PopulateCharacterAttends(logParseResults);
        PopulateZoneNames(logParseResults);

        AnalyzeLogFilesAttendanceCalls(logParseResults);
        HandleCrashedEntries(logParseResults);
        HandleAfkTags(logParseResults);
        HandleTransfers(logParseResults);
        IdentifyMultipleCharactersOnOneAccount();
    }

    private void AddCharactersFromCharactersAttending(DateTime timestamp, AttendanceEntry call)
    {
        foreach (CharacterAttend character in _charactersAttending.Where(x => x.Timestamp.IsWithinDurationOfPopulationThreshold(timestamp)))
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
        RaidDumpFile raidDump = logParseResults.RaidDumpFiles.FirstOrDefault(x => x.FileDateTime.IsWithinDurationOfPopulationThreshold(logEntry.Timestamp));
        if (raidDump == null)
            return;

        foreach (PlayerCharacter character in raidDump.Characters)
        {
            call.AddOrMergeInPlayerCharacter(character);
        }
    }

    private void AddRaidListMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        RaidListFile raidList = logParseResults.RaidListFiles.FirstOrDefault(x => x.FileDateTime.IsWithinDurationOfPopulationThreshold(logEntry.Timestamp));
        if (raidList == null)
            return;

        foreach (PlayerCharacter character in raidList.CharacterNames)
        {
            call.AddOrMergeInPlayerCharacter(character);
        }
    }

    private void AddZealRaidMembers(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        ZealRaidAttendanceFile zealAttendance = logParseResults.ZealRaidAttendanceFiles.FirstOrDefault(x => x.RaidName == call.CallName);
        if (zealAttendance == null)
            return;

        foreach (PlayerCharacter character in zealAttendance.CharacterNames)
        {
            call.AddOrMergeInPlayerCharacter(character);
        }
    }

    private void AnalyzeLogFilesAttendanceCalls(LogParseResults logParseResults)
    {
        // [Sun Mar 17 22:15:31 2024] You tell your raid, ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'
        // [Sun Mar 17 23:18:28 2024] You tell your raid, ':::Raid Attendance Taken:::Sister of the Spire:::Kill:::'

        List<AttendanceEntry> logCalls = GetLogBasedAttendanceCalls(logParseResults);
        IEnumerable<AttendanceEntry> zealCalls = GetZealAttendanceCalls(logParseResults, logCalls);

        IEnumerable<AttendanceEntry> calls = logCalls.Concat(zealCalls);
        foreach (AttendanceEntry attendance in calls.OrderBy(x => x.Timestamp))
        {
            _raidEntries.AttendanceEntries.Add(attendance);
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
                .OrderBy(x => x.Timestamp)
                .FirstOrDefault();

            if (endEntry == null)
                Log.Warning($"{LogPrefix} Did not find AFKEND entry for: {afkStartEntry.LogLine}");

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

        string logLine = logEntry.LogLine;
        if (logLine.Length < Constants.AfkAlternateDelimiter.Length + Constants.RaidOther.Length + 5)
        {
            Log.Info($"{LogPrefix} Unable get character name in {Constants.AfkWithDelimiter} or {Constants.AfkEndWithDelimiter} entry: {logEntry.FullLogLine}");
            return null;
        }

        string[] parts = logLine.Split(' ');
        if (parts.Length < 4)
        {
            Log.Info($"{LogPrefix} Unable get character name in {Constants.AfkWithDelimiter} or {Constants.AfkEndWithDelimiter} entry: {logEntry.FullLogLine}");
            return null;
        }

        string characterName = parts[0].Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            Log.Info($"{LogPrefix} Unable get character name in {Constants.AfkWithDelimiter} or {Constants.AfkEndWithDelimiter} entry: {logEntry.FullLogLine}");
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

        string logLine = entry.LogLine;
        int indexOfLastBracket = logLine.LastIndexOf(']');
        if (indexOfLastBracket < 0)
        {
            // Should not reach here.
            Debug.Fail($"Reached a place in {nameof(ExtractAttendingCharacter)} that should not be reached. No ']'.  Logline: {entry.FullLogLine}");
            Log.Warning($"{LogPrefix} Unable to extract attending character. No ']'.: {entry.FullLogLine}");
            return new CharacterAttend { CharacterName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        int firstIndexOfEndMarker = logLine.LastIndexOf('(');
        if (firstIndexOfEndMarker < 0)
        {
            firstIndexOfEndMarker = logLine.LastIndexOf('<');
            if (firstIndexOfEndMarker < 0)
            {
                // Should not reach here.
                Debug.Fail($"Reached a place in {nameof(ExtractAttendingCharacter)} that should not be reached. No '(' or '<'.  Logline: {entry.FullLogLine}");
                Log.Warning($"{LogPrefix} Unable to extract attending character. No '(' or '<'.: {entry.FullLogLine}");
                return new CharacterAttend { CharacterName = "UNKNOWN", Timestamp = entry.Timestamp };
            }
        }

        string characterName = logLine[(indexOfLastBracket + 1)..firstIndexOfEndMarker].Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            Log.Warning($"{LogPrefix} Unable to get character name from 'who' entry: {entry.FullLogLine}");
            return new CharacterAttend { CharacterName = "UNKNOWN", Timestamp = entry.Timestamp };
        }

        PlayerCharacter character = new() { CharacterName = characterName };

        if (logLine.Contains(Constants.AnonWithBrackets))
        {
            _raidEntries.AddOrMergeInPlayerCharacter(character);
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        int indexOfLeadingClassBracket = logLine.LastIndexOf('[');
        if (indexOfLeadingClassBracket < 0)
        {
            Log.Warning($"{LogPrefix} Unable to find leading '[' in 'who' entry: {entry.FullLogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        string classLevelString = logLine[(indexOfLeadingClassBracket + 1)..indexOfLastBracket];
        if (string.IsNullOrWhiteSpace(classLevelString))
        {
            Log.Warning($"{LogPrefix} Unable to get class and level in 'who' entry: {entry.FullLogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        string[] classAndLevel = classLevelString.Split(' ');
        if (classAndLevel.Length < 2)
        {
            Log.Warning($"{LogPrefix} Unable to get class and level in 'who' entry: {entry.FullLogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        int indexOfLastParens = logLine.LastIndexOf(')');
        if (indexOfLastParens < 0)
        {
            Log.Warning($"{LogPrefix} Unable to find trailing ')' in 'who' entry: {entry.FullLogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        string race = logLine[(firstIndexOfEndMarker + 1)..indexOfLastParens];
        if (string.IsNullOrWhiteSpace(race))
        {
            Log.Warning($"{LogPrefix} Unable to get race in 'who' entry: {entry.FullLogLine}");
            return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp };
        }

        character.ClassName = classAndLevel.Length > 2 ? string.Join(" ", classAndLevel[1], classAndLevel[2]) : classAndLevel[1];
        character.Level = int.Parse(classAndLevel[0]);
        character.Race = race;
        _raidEntries.AddOrMergeInPlayerCharacter(character);

        bool isAfk = logLine.Contains(AfkFlag);
        return new CharacterAttend { CharacterName = characterName, Timestamp = entry.Timestamp, IsAfk = isAfk };
    }

    private PlayerCharacter ExtractCrashedCharacter(EqLogEntry logEntry)
    {
        // [Thu Mar 07 21:33:39 2024] Undertree tells the raid,  ':::CRASHED:::'

        string logLine = logEntry.LogLine;
        if (logLine.Length < Constants.CrashedAlternateDelimiter.Length + Constants.RaidOther.Length + 5)
        {
            Log.Warning($"{LogPrefix} Unable get character name in CRASHED entry: {logEntry.FullLogLine}");
            return null;
        }

        string[] parts = logLine.Split(' ');
        if (parts.Length < 4)
        {
            Log.Warning($"{LogPrefix} Unable get character name in CRASHED entry: {logEntry.FullLogLine}");
            return null;
        }

        string characterName = parts[0].Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            Log.Warning($"{LogPrefix} Unable get character name in CRASHED entry: {logEntry.FullLogLine}");
            return null;
        }

        PlayerCharacter character = _raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == characterName);
        return character;
    }

    private DkpTransfer ExtractTransfer(EqLogEntry logEntry)
    {
        logEntry.Visited = true;

        // [Fri Nov 01 23:13:39 2024] You tell your raid, ':::Tookky:::Genos:::TRANSFER'
        string correctedLogLine = _sanitizer.SanitizeDelimiterString(logEntry.LogLine);
        string[] parts = correctedLogLine.Split(Constants.AttendanceDelimiter);
        if (parts.Length < 4)
        {
            Log.Warning($"{LogPrefix} TRANSFER message when split has too few parts: {logEntry.FullLogLine}");
            return null;
        }

        string fromCharacter = parts[1];
        string toCharacter = parts[2];

        if (string.IsNullOrWhiteSpace(fromCharacter))
        {
            Log.Warning($"{LogPrefix} TRANSFER message unable to extract FROM character in log line: {logEntry.FullLogLine}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(toCharacter))
        {
            Log.Warning($"{LogPrefix} TRANSFER message unable to extract TO character in log line: {logEntry.FullLogLine}");
            return null;
        }

        fromCharacter = fromCharacter.NormalizeName();
        PlayerCharacter fromPlayerCharacter = _raidEntries.AllCharactersInRaid
            .FirstOrDefault(x => x.CharacterName.Equals(fromCharacter, StringComparison.OrdinalIgnoreCase));
        if (fromPlayerCharacter == null)
        {
            Log.Warning($"{LogPrefix} TRANSFER message unable to find FROM character in character listing: {logEntry.FullLogLine}");
            return null;
        }

        return new DkpTransfer
        {
            FromCharacter = fromPlayerCharacter,
            ToCharacterName = toCharacter.NormalizeName(),
            Timestamp = logEntry.Timestamp,
            LogLine = correctedLogLine,
        };
    }

    private ZoneNameInfo ExtractZoneName(EqLogEntry entry)
    {
        entry.Visited = true;

        bool isMultiplePlayers = true;

        // [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
        int indexOfPlayersIn = entry.LogLine.IndexOf(Constants.PlayersIn);
        if (indexOfPlayersIn < 0)
        {
            indexOfPlayersIn = entry.LogLine.IndexOf(Constants.PlayerIn);
            if (indexOfPlayersIn < 0)
            {
                Log.Warning($"{LogPrefix} Unable get zone name: {entry.FullLogLine}");
                return null;
            }

            isMultiplePlayers = false;
        }

        int playersInLength = isMultiplePlayers ? Constants.PlayersIn.Length : Constants.PlayerIn.Length;
        int endIndexOfPlayersIn = indexOfPlayersIn + playersInLength;
        if (endIndexOfPlayersIn + 5 > entry.LogLine.Length)
        {
            Log.Warning($"{LogPrefix} Unable get zone name: {entry.FullLogLine}");
            return null;
        }

        string zoneName = entry.LogLine[endIndexOfPlayersIn..^1].Trim();
        if (string.IsNullOrEmpty(zoneName))
        {
            Log.Warning($"{LogPrefix} Unable get zone name: {entry.FullLogLine}");
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

                    AfkEntryInfo characterAfkEntry = new() { Character = afkCharacter, EntryType = logEntry.EntryType, Timestamp = logEntry.Timestamp, LogLine = logEntry.FullLogLine };

                    afkCharacterEntries.Add(characterAfkEntry);
                }
                catch (Exception ex)
                {
                    Log.Error($"{LogPrefix} An unexpected error occurred when analyzing an {Constants.AfkWithDelimiter} or {Constants.AfkEndWithDelimiter} call: {logEntry.FullLogLine}{Environment.NewLine}{ex.ToLogMessage()}");
                }
            }
        }

        afkCharacterEntries = afkCharacterEntries.OrderBy(x => x.Timestamp).ToList();
        return afkCharacterEntries;
    }

    private AttendanceEntry GetAssociatedLogEntry(AttendanceEntry call)
        => _raidEntries.AttendanceEntries
            .Where(x => x.Timestamp < call.Timestamp && x.CallName == call.CallName && x.AttendanceCallType == call.AttendanceCallType)
            .OrderBy(x => x.Timestamp)
            .MaxBy(x => x.Timestamp);

    private List<AttendanceEntry> GetLogBasedAttendanceCalls(LogParseResults logParseResults)
    {
        List<AttendanceEntry> calls = [];
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry logEntry in log.LogEntries.Where(x => x.EntryType == LogEntryType.Attendance || x.EntryType == LogEntryType.Kill))
            {
                logEntry.Visited = true;
                AttendanceEntry call;

                try
                {
                    string correctedLogLine = _sanitizer.SanitizeDelimiterString(logEntry.LogLine);
                    call = new() { Timestamp = logEntry.Timestamp, RawHeaderLogLine = logEntry.FullLogLine };

                    SetAttendanceType(logEntry, call, correctedLogLine);

                    if (IsRemoveCall(logEntry, call, correctedLogLine))
                        continue;

                    AddRaidListMembers(logParseResults, logEntry, call);

                    AddZealRaidMembers(logParseResults, logEntry, call);

                    AddRaidDumpMembers(logParseResults, logEntry, call);

                    AddCharactersFromCharactersAttending(logEntry.Timestamp, call);

                    UpdateCharacterInfo(call);

                    SetZoneName(logParseResults, logEntry, call);

                    if (call.Characters.Count > 1)
                        calls.Add(call);
                }
                catch (Exception ex)
                {
                    EuropaDkpParserException eex = new("An unexpected error occurred when analyzing an attendance call.", logEntry.FullLogLine, ex);
                    throw eex;
                }
            }
        }

        return calls;
    }

    private IEnumerable<AttendanceEntry> GetZealAttendanceCalls(LogParseResults logParseResults, IEnumerable<AttendanceEntry> logCalls)
    {
        foreach (ZealRaidAttendanceFile zealAttendance in logParseResults.ZealRaidAttendanceFiles)
        {
            if (logCalls.Any(x => x.CallName == zealAttendance.RaidName))
                continue;

            AttendanceEntry attendance = new()
            {
                AttendanceCallType = zealAttendance.CallType,
                CallName = zealAttendance.RaidName,
                Timestamp = zealAttendance.FileDateTime,
                ZoneName = zealAttendance.ZoneName,
                Characters = zealAttendance.CharacterNames,
                RawHeaderLogLine = string.Empty
            };

            AddCharactersFromCharactersAttending(zealAttendance.FileDateTime, attendance);

            if (!_settings.RaidValue.AllValidRaidZoneNames.Contains(attendance.ZoneName))
            {
                attendance.PossibleError = PossibleError.InvalidZoneName;
            }

            yield return attendance;
        }
    }

    private void HandleAfkTags(LogParseResults logParseResults)
    {
        List<AfkEntryInfo> afkCharacterEntries = GetAfkDeclarationEntries(logParseResults);

        CreateAfkEntries(afkCharacterEntries);

        foreach (AfkEntry afk in _raidEntries.AfkEntries)
        {
            IEnumerable<AttendanceEntry> relatedAttendances = _raidEntries.AttendanceEntries.Where(x => afk.StartTime < x.Timestamp && x.Timestamp < afk.EndTime);
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
                        Log.Warning($"{LogPrefix} Unable to extract character name when analyzing a CRASHED call: {crashedLogEntry.FullLogLine}");
                        continue;
                    }

                    DateTime startTimestamp = crashedLogEntry.Timestamp - defaultCrashedThresholdInMinutes;

                    // Get previous "Player has left the raid" entry
                    CharacterJoinRaidEntry previousLeaveEntry = _raidEntries.CharacterJoinCalls
                        .Where(x => x.EntryType == LogEntryType.LeftRaid && x.CharacterName == crashedCharacter.CharacterName && x.Timestamp < crashedLogEntry.Timestamp)
                        .MaxBy(x => x.Timestamp);

                    if (previousLeaveEntry != null)
                    {
                        // If the last "Player has left the raid" entry is less than 15 minutes in the past, use that timestamp
                        bool isLessThan15Minutes = (crashedLogEntry.Timestamp - previousLeaveEntry.Timestamp) < crashedLeftRaidThresholdInMinutes;
                        if (isLessThan15Minutes)
                        {
                            startTimestamp = previousLeaveEntry.Timestamp;
                        }
                    }

                    IEnumerable<AttendanceEntry> missingFromAttendanceEnties = _raidEntries.AttendanceEntries
                        .Where(x => startTimestamp <= x.Timestamp && x.Timestamp < crashedLogEntry.Timestamp);

                    foreach (AttendanceEntry missingAttendance in missingFromAttendanceEnties)
                    {
                        if (!_settings.CharactersOnDkpServer.IsRelatedCharacterInCollection(crashedCharacter, missingAttendance.Characters))
                            missingAttendance.AddOrMergeInPlayerCharacter(crashedCharacter);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{LogPrefix} An unexpected error occurred when analyzing a CRASHED call: {crashedLogEntry.FullLogLine}{Environment.NewLine}{ex.ToLogMessage()}");
                }
            }
        }
    }

    private void HandleTransfers(LogParseResults logParseResults)
    {
        List<DkpTransfer> transfers = [];
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<DkpTransfer> transfersToAdd = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.Transfer)
                .Select(ExtractTransfer)
                .Where(x => x != null);

            transfers.AddRange(transfersToAdd);
        }

        _raidEntries.Transfers = transfers;
    }

    private void IdentifyMultipleCharactersOnOneAccount()
    {
        List<string> tranferFromCharacters = _raidEntries.Transfers.Select(x => x.FromCharacter.CharacterName).ToList();
        List<PlayerCharacter> transferToCharacters = _raidEntries.Transfers
            .Select(x => x.ToCharacterName)
            .Select(x => new PlayerCharacter { CharacterName = x }).ToList();

        List<MultipleCharsOnAttendanceError> multipleChars = [];
        foreach (AttendanceEntry attendance in _raidEntries.AttendanceEntries)
        {
            IEnumerable<PlayerCharacter> charactersInAttendanceAndTransferChars = attendance.Characters.Union(transferToCharacters);
            IEnumerable<MutipleCharactersOnAccount> multipleCharacters = _settings.CharactersOnDkpServer.GetMultipleCharactersOnAccount(charactersInAttendanceAndTransferChars);
            IEnumerable<MutipleCharactersOnAccount> multipleCharactersNotTransfer = multipleCharacters
                .Where(x => !tranferFromCharacters.Contains(x.FirstCharacter.Name) && !tranferFromCharacters.Contains(x.SecondCharacter.Name));

            IEnumerable<MultipleCharsOnAttendanceError> multipleCharsErrorsToAdd = multipleCharactersNotTransfer
                .Select(x => new MultipleCharsOnAttendanceError { Attendance = attendance, MultipleCharsInAttendance = x });
            multipleChars.AddRange(multipleCharsErrorsToAdd);
        }

        _raidEntries.MultipleCharsInAttendanceErrors = multipleChars;
    }

    private bool IsRemoveCall(EqLogEntry logEntry, AttendanceEntry call, string logLine)
    {
        if (!logLine.Contains(Constants.Undo) && !logLine.Contains(Constants.Remove))
            return false;

        logEntry.Visited = true;

        AttendanceEntry toBeRemoved = GetAssociatedLogEntry(call);
        if (toBeRemoved != null)
        {
            _raidEntries.AttendanceEntries.Remove(toBeRemoved);
        }

        return true;
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

            _zonePerAttendance.AddRange(zones);
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
                Log.Warning($"{LogPrefix} Unable get get raid name from Time attendance entry: {logEntry.FullLogLine}");
                return;
            }

            call.CallName = splitEntry[3].Trim();
            if (string.IsNullOrEmpty(call.CallName))
            {
                Log.Warning($"{LogPrefix} Unable get get raid name from Time attendance entry: {logEntry.FullLogLine}");
            }
        }
        else
        {
            call.AttendanceCallType = AttendanceCallType.Kill;

            if (splitEntry.Length < 3)
            {
                Log.Warning($"{LogPrefix} Unable get get raid name from Kill attendance entry: {logEntry.FullLogLine}");
                return;
            }

            call.CallName = splitEntry[2].Trim();
            if (string.IsNullOrEmpty(call.CallName))
            {
                Log.Warning($"{LogPrefix} Unable get get raid name from Kill attendance entry: {logEntry.FullLogLine}");
            }
        }
    }

    private void SetZoneName(LogParseResults logParseResults, EqLogEntry logEntry, AttendanceEntry call)
    {
        ZoneNameInfo zoneLogEntry = _zonePerAttendance.FirstOrDefault(x => x.Timestamp.IsWithinDurationOfPopulationThreshold(logEntry.Timestamp));
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
            Log.Warning($"{LogPrefix} Unable get get zone name from attendance entry: {logEntry.FullLogLine}");
        }

        if (call.PossibleError == PossibleError.InvalidZoneName || call.PossibleError == PossibleError.NoZoneName)
        {
            ZealRaidAttendanceFile zealAttendance = logParseResults.ZealRaidAttendanceFiles.FirstOrDefault(x => x.RaidName == call.CallName);
            if (zealAttendance != null)
            {
                string zoneName = zealAttendance.ZoneName;
                if (string.IsNullOrWhiteSpace(zoneName))
                    return;

                if (string.IsNullOrWhiteSpace(call.ZoneName))
                    call.ZoneName = zoneName;

                if (_settings.RaidValue.AllValidRaidZoneNames.Contains(zoneName))
                {
                    call.ZoneName = zoneName;
                    call.PossibleError = PossibleError.None;
                }
                else
                {
                    call.PossibleError = PossibleError.InvalidZoneName;
                }
            }
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
                Log.Warning($"{LogPrefix} No character found in AllCharacters collection to update character info: {characterInCall.CharacterName}");
            }
        }
    }

    [DebuggerDisplay("{DebugText}")]
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

[DebuggerDisplay("{DebugText,nq}")]
public sealed class MultipleCharsOnAttendanceError
{
    public AttendanceEntry Attendance { get; init; }

    public MutipleCharactersOnAccount MultipleCharsInAttendance { get; init; }

    public bool Reviewed { get; set; }

    private string DebugText
        => $"{Attendance.CallName} {MultipleCharsInAttendance.FirstCharacter} {MultipleCharsInAttendance.SecondCharacter}";
}

public interface IAttendanceEntryAnalyzer
{
    void AnalyzeAttendanceCalls(LogParseResults logParseResults, RaidEntries raidEntries);
}
