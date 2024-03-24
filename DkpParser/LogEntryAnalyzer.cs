// -----------------------------------------------------------------------
// LogEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

public sealed class LogEntryAnalyzer : ILogEntryAnalyzer
{
    private readonly List<PlayerAttend> _playersAttending = [];
    private readonly RaidEntries _raidEntries = new();
    private readonly IDkpParserSettings _settings;
    private readonly List<ZoneNameInfo> _zones = [];

    public LogEntryAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults)
    {
        PopulateMemberLists(logParseResults);
        PopulateLootList(logParseResults);
        PopulateZoneNames(logParseResults);

        AnalyzeAttendanceCalls(logParseResults);
        AnalyzeLootCalls(logParseResults);

        AddUnvisitedEntries(logParseResults);

        ErrorPostAnalysis(logParseResults);

        return _raidEntries;
    }

    private void AddUnvisitedEntries(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<EqLogEntry> entries = log.LogEntries.Where(x => !x.Visited);
            foreach (EqLogEntry entry in entries)
            {
                _raidEntries.UnvisitedEntries.Add(entry);
            }
        }
    }

    private void AnalyzeAttendanceCalls(LogParseResults logParseResults)
    {
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

                    RaidDumpFile raidDump = logParseResults.RaidDumpFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
                    if (raidDump != null)
                    {
                        foreach (string player in raidDump.CharacterNames)
                        {
                            call.PlayerNames.Add(player);
                        }
                    }

                    RaidListFile raidList = logParseResults.RaidListFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
                    if (raidList != null)
                    {
                        foreach (string player in raidList.CharacterNames)
                        {
                            call.PlayerNames.Add(player);
                        }
                    }

                    foreach (PlayerAttend player in _playersAttending.Where(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp)))
                    {
                        call.PlayerNames.Add(player.PlayerName);
                    }

                    ZoneNameInfo zoneLogEntry = _zones.FirstOrDefault(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp));
                    if (zoneLogEntry != null)
                    {
                        call.ZoneName = zoneLogEntry.ZoneName;
                    }
                    else
                    {
                        //** Heuristic to find nearby zone name
                    }

                    logEntry.Visited = true;
                    _raidEntries.AttendanceEntries.Add(call);
                }
            }
        }
    }

    private void AnalyzeLootCalls(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<DkpEntry> dkpEntries = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.DkpSpent)
                .Select(ExtractDkpSpentInfo);

            foreach (DkpEntry dkpEntry in dkpEntries)
            {
                _raidEntries.DkpEntries.Add(dkpEntry);
            }
        }
    }

    private void CheckDkpPlayerName(DkpEntry dkpEntry)
    {
        foreach (PlayerLooted playerLootedEntry in _raidEntries.PlayerLootedEntries.Where(x => x.PlayerName.Equals(dkpEntry.PlayerName, StringComparison.OrdinalIgnoreCase)))
        {
            if (playerLootedEntry.ItemLooted == dkpEntry.Item)
                return;
        }

        if (!_raidEntries.AllPlayersInRaid.Contains(dkpEntry.PlayerName))
        {
            dkpEntry.PossibleError = PossibleError.DkpSpentPlayerNameTypo;
            return;
        }

        dkpEntry.PossibleError = PossibleError.PlayerLootedMessageNotFound;
    }

    private void CheckDkpSpentTypos(LogParseResults logParseResults)
    {
        //** Still debating on this.
    }

    private void CheckDuplicateAttendanceEntries(LogParseResults logParseResults)
    {
        var grouped = from a in _raidEntries.AttendanceEntries
                      group a by a.RaidName.ToUpper() into ae
                      where ae.Count() > 1
                      select new { Attendances = ae };

        foreach (var attendanceGroup in grouped)
        {
            foreach (AttendanceEntry att in attendanceGroup.Attendances)
            {
                att.PossibleError = PossibleError.DuplicateRaidEntry;
            }
        }
    }

    private void CheckRaidBossTypo(LogParseResults logParseResults)
    {
        // Dont bother checking if the file wasnt found
        if (_settings.BossMobs.Count == 0)
            return;

        foreach (AttendanceEntry killCall in _raidEntries.AttendanceEntries.Where(x => x.AttendanceCallType == AttendanceCallType.Kill))
        {
            if (!_settings.BossMobs.Contains(killCall.RaidName))
            {
                killCall.PossibleError = PossibleError.BossMobNameTypo;
            }
        }
    }

    private string CorrectDelimiter(string logLine)
    {
        logLine = logLine.Replace(Constants.PossibleErrorDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(Constants.TooLongDelimiter, Constants.AttendanceDelimiter);
        return logLine;
    }

    private void ErrorPostAnalysis(LogParseResults logParseResults)
    {
        CheckDuplicateAttendanceEntries(logParseResults);
        CheckRaidBossTypo(logParseResults);
        CheckDkpSpentTypos(logParseResults);
    }

    private PlayerAttend ExtractAttendingPlayerName(EqLogEntry entry)
    {
        // [Sun Mar 17 21:27:54 2024]  AFK [50 Bard] Grindcore (Half Elf) <Europa>
        // [Tue Mar 19 20:30:56 2024]  AFK [ANONYMOUS] Ecliptor  <Europa>
        // [Tue Mar 19 23:46:05 2024]  <LINKDEAD>[ANONYMOUS] Luwena  <Europa>
        // [Sun Mar 17 21:27:54 2024] [50 Monk] Pullz (Human) <Europa>
        // [Sun Mar 17 21:27:54 2024] [ANONYMOUS] Mendrik <Europa>
        // [Sat Mar 09 20:23:41 2024]  <LINKDEAD>[50 Rogue] Noggen (Dwarf) <Europa>
        int indexOfLastBracket = entry.LogLine.LastIndexOf(']') + 1;
        int firstIndexOfEndMarker = entry.LogLine.LastIndexOf('(');
        if (firstIndexOfEndMarker == -1)
        {
            firstIndexOfEndMarker = entry.LogLine.LastIndexOf('<');
            if (indexOfLastBracket == -1)
            {
                // If cant find one of the above symbols for some reason, just go to the end of the string.
                // Should not reach here.
                Debug.Fail($"Reached a place in {nameof(ExtractAttendingPlayerName)} that should not be reached.  Logline: {entry.LogLine}");
                firstIndexOfEndMarker = entry.LogLine.Length - 1;
            }
        }
        string playerName = entry.LogLine[indexOfLastBracket..firstIndexOfEndMarker].Trim();

        entry.Visited = true;

        return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
    }

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
        // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
        string logLine = CorrectDelimiter(entry.LogLine);

        string[] dkpLineParts = logLine.Split(Constants.AttendanceDelimiter);
        string itemName = dkpLineParts[1].Trim();
        string[] playerParts = dkpLineParts[2].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string playerName = playerParts[0].Trim();

        DkpEntry dkpEntry = new()
        {
            PlayerName = playerName,
            Item = itemName,
            Timestamp = entry.Timestamp,
        };

        CheckDkpPlayerName(dkpEntry);

        string dkpAmountText = playerParts[1].Trim();
        GetDkpAmount(dkpAmountText, dkpEntry);

        entry.Visited = true;

        return dkpEntry;
    }

    private PlayerLooted ExtractPlayerLooted(EqLogEntry entry)
    {
        // [Wed Feb 21 18:49:31 2024] --Orsino has looted a Part of Tasarin's Grimoire Pg. 24.--
        // [Wed Feb 21 16:34:07 2024] --You have looted a Bloodstained Key.--
        int indexOfFirstDashes = entry.LogLine.IndexOf(Constants.DoubleDash);
        int startIndex = indexOfFirstDashes + 2;
        int endIndex = entry.LogLine.Length - 3;
        string lootString = entry.LogLine[startIndex..endIndex];

        int indexOfSpace = lootString.IndexOf(' ');
        string playerName = lootString[0..indexOfSpace];

        int indexOfLooted = lootString.IndexOf(Constants.LootedA);
        int startIndexOfItem = indexOfLooted + Constants.LootedA.Length;
        string itemName = lootString[startIndexOfItem..];

        entry.Visited = true;

        return new PlayerLooted { PlayerName = playerName, ItemLooted = itemName, Timestamp = entry.Timestamp };
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

    private void GetDkpAmount(string dkpAmountText, DkpEntry dkpEntry)
    {
        dkpAmountText = dkpAmountText.TrimEnd('\'');
        if (int.TryParse(dkpAmountText, out int dkpAmount))
        {
            dkpEntry.DkpSpent = dkpAmount;
            return;
        }

        // See if lack of space -> 1DKPSPENT
        int dkpSpentIndex = dkpAmountText.IndexOf(Constants.DkpSpent);
        if (dkpSpentIndex > -1)
        {
            string dkpSpentTextWithoutDkpspent = dkpAmountText[0..dkpSpentIndex];
            if (int.TryParse(dkpSpentTextWithoutDkpspent, out dkpAmount))
            {
                dkpEntry.DkpSpent = dkpAmount;
                return;
            }
        }

        dkpEntry.PossibleError = PossibleError.DkpAmountNotANumber;
    }

    private void PopulateLootList(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<PlayerLooted> playersLooted = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.PlayerLooted)
                .Select(ExtractPlayerLooted);

            _raidEntries.PlayerLootedEntries = playersLooted.ToList();
        }
    }

    private void PopulateMemberLists(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<PlayerAttend> players = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.PlayerName)
                .Select(ExtractAttendingPlayerName);

            _playersAttending.AddRange(players);
        }

        foreach (PlayerAttend playerAttend in _playersAttending)
        {
            _raidEntries.AllPlayersInRaid.Add(playerAttend.PlayerName);
        }

        foreach (RaidDumpFile raidDump in logParseResults.RaidDumpFiles)
        {
            foreach (string playerName in raidDump.CharacterNames)
            {
                _raidEntries.AllPlayersInRaid.Add(playerName);
            }
        }

        foreach (RaidListFile raidList in logParseResults.RaidListFiles)
        {
            foreach (string playerName in raidList.CharacterNames)
            {
                _raidEntries.AllPlayersInRaid.Add(playerName);
            }
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

    [DebuggerDisplay("{PlayerName,nq}")]
    private sealed class PlayerAttend
    {
        public string PlayerName { get; init; }

        public DateTime Timestamp { get; init; }
    }

    [DebuggerDisplay("{ZoneName,nq}, {Timestamp,nq}")]
    private sealed class ZoneNameInfo
    {
        public DateTime Timestamp { get; init; }

        public string ZoneName { get; init; }
    }
}

public interface ILogEntryAnalyzer
{
    RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults);
}
