// -----------------------------------------------------------------------
// LogEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System;

public sealed class LogEntryAnalyzer : ILogEntryAnalyzer
{
    private readonly RaidEntries _raidEntries = new();
    private readonly IDkpParserSettings _settings;

    public LogEntryAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults)
    {
        PopulateMemberLists(logParseResults);
        PopulateLootList(logParseResults);
        PopulateRaidJoin(logParseResults);

        AnalyzeAttendanceCalls(logParseResults);
        AnalyzeLootCalls(logParseResults);

        AddUnvisitedEntries(logParseResults);

        ErrorPostAnalysis(logParseResults);

        return _raidEntries;
    }

    private void PopulateRaidJoin(LogParseResults logParseResults)
    {
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<PlayerJoinRaidEntry> playersJoined = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.JoinedRaid || x.EntryType == LogEntryType.LeftRaid)
                .Select(ExtractePlayerJoin)
                .Where(x => x != null);

            _raidEntries.PlayerJoinCalls = playersJoined.ToList();
        }
    }

    private PlayerJoinRaidEntry ExtractePlayerJoin(EqLogEntry entry)
    {
        // [Tue Feb 27 23:13:23 2024] Orsino has left the raid.
        // [Tue Feb 27 23:14:20 2024] Marco joined the raid.
        // [Sun Feb 25 22:52:46 2024] You have joined the group.
        // [Thu Feb 22 23:13:52 2024] Luciania joined the raid.
        // [Thu Feb 22 23:13:52 2024] You have joined the raid.
        int indexOfLastBracket = entry.LogLine.IndexOf(']');
        string entryMessage = entry.LogLine[(indexOfLastBracket + 1)..];
        if (entryMessage.Contains("You have "))
            return null;

        int indexOfSpace = entryMessage.IndexOf(' ');
        string playerName = entryMessage[0..indexOfSpace];

        return new PlayerJoinRaidEntry
        {
            PlayerName = playerName,
            Timestamp = entry.Timestamp,
            EntryType = entry.EntryType
        };
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
        IAttendanceEntryAnalyzer attendanceAnalyzer = new AttendanceEntryAnalyzer();
        attendanceAnalyzer.AnalyzeAttendanceCalls(logParseResults, _raidEntries);
    }

    private void AnalyzeLootCalls(LogParseResults logParseResults)
    {
        IDkpEntryAnalyzer dkpEntryAnalyzer = new DkpEntryAnalyzer();
        dkpEntryAnalyzer.AnalyzeLootCalls(logParseResults, _raidEntries);
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

    private void ErrorPostAnalysis(LogParseResults logParseResults)
    {
        CheckDuplicateAttendanceEntries(logParseResults);
        CheckRaidBossTypo(logParseResults);
        CheckDkpSpentTypos(logParseResults);
        CheckDuplicateDkpEntries(logParseResults);
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
}

public interface ILogEntryAnalyzer
{
    RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults);
}
