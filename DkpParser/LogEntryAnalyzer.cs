// -----------------------------------------------------------------------
// LogEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class LogEntryAnalyzer : ILogEntryAnalyzer
{
    private List<PlayerAttend> _playersAttending;
    private HashSet<string> _playersAttendingRaid = [];
    private List<PlayerLooted> _playersLooted;
    private RaidEntries _raidEntries = new();

    public RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults)
    {
        PopulateMemberLists(logParseResults);
        PopulateLootList(logParseResults);

        AnalyzeAttendanceCalls(logParseResults);
        AnalyzeLootCalls(logParseResults);



        return _raidEntries;
    }

    private void AnalyzeAttendanceCalls(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            foreach (EqLogEntry logEntry in log.LogEntries)
            {
                if (logEntry.EntryType == LogEntryType.Attendance || logEntry.EntryType == LogEntryType.Kill)
                {
                    //** Verify indexes
                    string logLine = CorrectDelimiter(logEntry.LogLine);
                    string[] splitEntry = logLine.Split(Constants.AttendanceDelimiter);
                    string raidName = splitEntry[3];

                    AttendanceEntry call = new() { Timestamp = logEntry.Timestamp, RaidName = raidName };
                    RaidDumpFile raidDump = logParseResults.RaidDumpFiles.FirstOrDefault(x => x.FileDateTime.IsWithinTwoSecondsOf(logEntry.Timestamp));
                    if (raidDump != null)
                    {
                        foreach (string player in raidDump.CharacterNames)
                        {
                            call.PlayerNames.Add(player);
                        }
                    }
                    foreach (PlayerAttend player in _playersAttending.Where(x => x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp)))
                    {
                        call.PlayerNames.Add(player.PlayerName);
                    }

                    EqLogEntry zoneLogEntry = log.LogEntries.FirstOrDefault(x => x.EntryType == LogEntryType.WhoZoneName && x.Timestamp.IsWithinTwoSecondsOf(logEntry.Timestamp));
                    if (zoneLogEntry != null)
                    {
                        // [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
                        int indexOfPlayersIn = zoneLogEntry.LogLine.IndexOf(Constants.PlayersIn);
                        int endIndexOfPlayersIn = indexOfPlayersIn + Constants.PlayersIn.Length;
                        string zoneName = zoneLogEntry.LogLine[endIndexOfPlayersIn..^1];
                        call.ZoneName = zoneName;
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

    private string CorrectDelimiter(string logLine)
    {
        logLine = logLine.Replace(Constants.PossibleErrorDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(Constants.TooLongDelimiter, Constants.AttendanceDelimiter);
        return logLine;
    }

    private void AnalyzeLootCalls(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<DkpEntry> lootAwardCalls = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.DkpSpent)
                .Select(ExtractDkpSpentInfo);
        }
    }

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
        //** Verify indexes
        string logLine = CorrectDelimiter(entry.LogLine);

        string[] dkpLineParts = logLine.Split(Constants.AttendanceDelimiter);
        string itemName = dkpLineParts[1].Trim();
        string[] playerParts = dkpLineParts[2].Trim().Split(' ');

        string playerName = playerParts[0].Trim();
        string dkpAmountText = dkpLineParts[1].Trim();
        if(!int.TryParse(dkpAmountText, out int dkpAmount))
        {
            //** how to handle?
        }

        DkpEntry dkpEntry = new()
        {
            PlayerName = playerName,
            Item = itemName,
            DkpSpent = dkpAmount,
            Timestamp = entry.Timestamp,
        };
        //**

        return dkpEntry;
    }

    private PlayerAttend ExtractAttendingPlayerName(EqLogEntry entry)
    {
        int indexOfLastBracket = entry.LogLine.LastIndexOf(']');
        int firstIndexOfEndMarker = entry.LogLine.IndexOf("(");
        if (firstIndexOfEndMarker == -1)
        {
            firstIndexOfEndMarker = entry.LogLine.IndexOf('<');
        }
        string playerName = entry.LogLine[indexOfLastBracket..firstIndexOfEndMarker].Trim();

        entry.Visited = true;

        return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
    }

    private PlayerLooted ExtractPlayerLooted(EqLogEntry entry)
    {
        int indexOfFirstDashes = entry.LogLine.IndexOf(Constants.DoubleDash);
        int startIndex = indexOfFirstDashes + 2;
        int endIndex = entry.LogLine.Length - 3;
        string lootString = entry.LogLine[startIndex..endIndex];

        int indexOfSpace = lootString.IndexOf(' ');
        string playerName = lootString[0..indexOfSpace];

        int indexOfLooted = lootString.IndexOf(Constants.LootedA);
        int startIndexOfItem = indexOfLooted + Constants.LootedA.Length;
        string itemName = lootString[startIndexOfItem..(lootString.Length - 1)];

        entry.Visited = true;

        return new PlayerLooted { PlayerName = playerName, ItemLooted = itemName, Timestamp = entry.Timestamp };
    }

    private void PopulateLootList(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            _playersLooted = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.PlayerLooted)
                .Select(ExtractPlayerLooted)
                .ToList();
        }
    }

    private void PopulateMemberLists(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            _playersAttending = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.PlayerName)
                .Select(ExtractAttendingPlayerName)
                .ToList();
        }

        foreach (PlayerAttend playerAttend in _playersAttending)
        {
            _playersAttendingRaid.Add(playerAttend.PlayerName);
        }

        foreach (RaidDumpFile raidDump in logParseResults.RaidDumpFiles)
        {
            foreach (string playerName in raidDump.CharacterNames)
            {
                _playersAttendingRaid.Add(playerName);
            }
        }
    }

    private sealed class PlayerAttend
    {
        public string PlayerName { get; init; }

        public DateTime Timestamp { get; init; }
    }

    private sealed class PlayerLooted
    {
        public string ItemLooted { get; init; }

        public string PlayerName { get; init; }

        public DateTime Timestamp { get; init; }
    }
}

public interface ILogEntryAnalyzer
{
    RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults);
}
