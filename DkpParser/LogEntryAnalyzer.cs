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

                    if (logLine.Contains(Constants.Undo) || logLine.Contains(Constants.Remove))
                    {
                        AttendanceEntry toBeRemoved = GetAssociatedLogEntry(_raidEntries, call);
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

    private void AnalyzeLootCalls(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<DkpEntry> dkpEntries = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.DkpSpent)
                .Select(ExtractDkpSpentInfo)
                .Where(x => x != null);

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

        if (!_raidEntries.AllPlayersInRaid.Any(x => x.PlayerName.Equals(dkpEntry.PlayerName, StringComparison.OrdinalIgnoreCase)))
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

    private void CheckDuplicateDkpEntries(LogParseResults logParseResults)
    {
        var grouped = from a in _raidEntries.DkpEntries
                      group a by new { PlayerName = a.PlayerName.ToUpper(), a.Item, a.DkpSpent } into ae
                      where ae.Count() > 1
                      select new { DkpEntries = ae };

        foreach (var dkpEntryGroup in grouped)
        {
            foreach (DkpEntry dkpEntry in dkpEntryGroup.DkpEntries)
            {
                dkpEntry.PossibleError = PossibleError.DkpDuplicateEntry;
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
        CheckDuplicateDkpEntries(logParseResults);
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

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
        // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
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
            RawLogLine = entry.LogLine,
        };

        if (logLine.Contains(Constants.Undo) || logLine.Contains(Constants.Remove))
        {
            DkpEntry toBeRemoved = GetAssociatedDkpEntry(_raidEntries, dkpEntry);
            if (toBeRemoved != null)
            {
                _raidEntries.DkpEntries.Remove(toBeRemoved);
            }
            entry.Visited = true;
            return null;
        }

        CheckDkpPlayerName(dkpEntry);

        string dkpAmountText = playerParts[1].Trim();
        GetDkpAmount(dkpAmountText, dkpEntry);
        GetAuctioneerName(dkpLineParts[0], dkpEntry);

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

        return new PlayerLooted { PlayerName = playerName, ItemLooted = itemName, Timestamp = entry.Timestamp, RawLogLine = entry.LogLine };
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

    private DkpEntry GetAssociatedDkpEntry(RaidEntries raidEntries, DkpEntry dkpEntry)
    {
        DkpEntry lastOneFound = null;
        foreach (DkpEntry entry in raidEntries.DkpEntries.Where(x => x.PlayerName == dkpEntry.PlayerName && x.Item == dkpEntry.Item))
        {
            if (entry.Timestamp < dkpEntry.Timestamp)
                lastOneFound = entry;
        }

        return lastOneFound;
    }

    private AttendanceEntry GetAssociatedLogEntry(RaidEntries raidEntries, AttendanceEntry call)
    {
        AttendanceEntry lastOneFound = null;
        foreach (AttendanceEntry entry in raidEntries.AttendanceEntries.Where(x => x.RaidName == call.RaidName && x.AttendanceCallType == call.AttendanceCallType))
        {
            if (entry.Timestamp < call.Timestamp)
                lastOneFound = entry;
        }

        return lastOneFound;
    }

    private void GetAuctioneerName(string initialLogLine, DkpEntry dkpEntry)
    {
        int indexOfBracket = initialLogLine.IndexOf(']');
        int indexOfTell = initialLogLine.IndexOf(" tell");

        string auctioneerName = initialLogLine[(indexOfBracket + 1)..indexOfTell].Trim();
        dkpEntry.Auctioneer = auctioneerName;
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

        foreach (RaidDumpFile raidDump in logParseResults.RaidDumpFiles)
        {
            foreach (PlayerCharacter playerChar in raidDump.Characters)
            {
                _raidEntries.AddOrMergeInPlayerCharacter(playerChar);
            }
        }

        foreach (RaidListFile raidList in logParseResults.RaidListFiles)
        {
            foreach (PlayerCharacter playerChar in raidList.CharacterNames)
            {
                _raidEntries.AddOrMergeInPlayerCharacter(playerChar);
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

public interface ILogEntryAnalyzer
{
    RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults);
}
