﻿// -----------------------------------------------------------------------
// LogEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

public sealed class LogEntryAnalyzer : ILogEntryAnalyzer
{
    private List<PlayerAttend> _playersAttending;
    private HashSet<string> _playersAttendingRaid = [];
    private List<PlayerLooted> _playersLooted;
    private RaidEntries _raidEntries = new();
    private List<ZoneNameInfo> _zones = new();

    public RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults)
    {
        PopulateMemberLists(logParseResults);
        PopulateLootList(logParseResults);
        PopulateZoneNames(logParseResults);

        AnalyzeAttendanceCalls(logParseResults);
        AnalyzeLootCalls(logParseResults);

        AddUnvisitedEntries(logParseResults);

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
                        call.RaidName = splitEntry[3];
                        call.AttendanceCallType = AttendanceCallType.Time;
                    }
                    else
                    {
                        call.RaidName = splitEntry[2];
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

    private string CorrectDelimiter(string logLine)
    {
        logLine = logLine.Replace(Constants.PossibleErrorDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(Constants.TooLongDelimiter, Constants.AttendanceDelimiter);
        return logLine;
    }

    private PlayerAttend ExtractAttendingPlayerName(EqLogEntry entry)
    {
        // [Tue Mar 19 23:46:05 2024]  <LINKDEAD>[ANONYMOUS] Luwena  <Europa>
        // [Sun Mar 17 21:27:54 2024] [50 Monk] Pullz (Human) <Europa>
        // [Sun Mar 17 21:27:54 2024] [ANONYMOUS] Mendrik <Europa>
        // [Sat Mar 09 20:23:41 2024]  <LINKDEAD>[50 Rogue] Noggen (Dwarf) <Europa>
        int indexOfLastBracket = entry.LogLine.LastIndexOf(']');
        int firstIndexOfEndMarker = entry.LogLine.LastIndexOf("(");
        if (firstIndexOfEndMarker == -1)
        {
            firstIndexOfEndMarker = entry.LogLine.LastIndexOf('<');
            if (indexOfLastBracket == -1)
            {
                firstIndexOfEndMarker = entry.LogLine.Length - 1;
            }
        }
        string playerName = entry.LogLine[indexOfLastBracket..firstIndexOfEndMarker].Trim();

        entry.Visited = true;

        return new PlayerAttend { PlayerName = playerName, Timestamp = entry.Timestamp };
    }

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
        //** Need to do error analysis - check against players looted (player name and item looted name), and players attending
        // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
        string logLine = CorrectDelimiter(entry.LogLine);

        string[] dkpLineParts = logLine.Split(Constants.AttendanceDelimiter);
        string itemName = dkpLineParts[1].Trim();
        string[] playerParts = dkpLineParts[2].Trim().Split(' ');

        string playerName = playerParts[0].Trim();

        DkpEntry dkpEntry = new()
        {
            PlayerName = playerName,
            Item = itemName,
            Timestamp = entry.Timestamp,
        };

        string dkpAmountText = playerParts[1].Trim();
        if (!int.TryParse(dkpAmountText, out int dkpAmount))
        {
            //** how to handle? See if lack of space -> 1DKPSPENT
            dkpEntry.PossibleError = PossibleError.DkpAmountNotANumber;
        }

        dkpEntry.DkpSpent = dkpAmount;

        entry.Visited = true;

        return dkpEntry;
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

    private ZoneNameInfo ExtractZoneName(EqLogEntry entry)
    {
        // [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
        int indexOfPlayersIn = entry.LogLine.IndexOf(Constants.PlayersIn);
        int endIndexOfPlayersIn = indexOfPlayersIn + Constants.PlayersIn.Length;
        string zoneName = entry.LogLine[endIndexOfPlayersIn..^1];

        entry.Visited = true;

        return new() { ZoneName = zoneName, Timestamp = entry.Timestamp };
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

    private void PopulateZoneNames(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            _zones = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.WhoZoneName)
                .Select(ExtractZoneName)
                .ToList();
        }
    }

    [DebuggerDisplay("{PlayerName,nq}")]
    private sealed class PlayerAttend
    {
        public string PlayerName { get; init; }

        public DateTime Timestamp { get; init; }
    }

    [DebuggerDisplay("{PlayerName,nq}, {ItemLooted,nq}")]
    private sealed class PlayerLooted
    {
        public string ItemLooted { get; init; }

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
