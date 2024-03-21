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
                    AttendanceCall call = new() { Timestamp = logEntry.Timestamp };
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
                }
            }
        }
    }

    private void AnalyzeLootCalls(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {

        }
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

    private void HandleAttendance(ref int logEntryIndex, IList<EqLogEntry> logEntries, EqLogEntry logEntry)
    {

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

    private sealed class AttendanceCall
    {
        public AttendanceCallType AttendanceCallType { get; set; }

        public HashSet<string> PlayerNames { get; } = [];

        public DateTime Timestamp { get; init; }

        public string ZoneName { get; set; }
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
