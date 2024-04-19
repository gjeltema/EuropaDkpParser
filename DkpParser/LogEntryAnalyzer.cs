// -----------------------------------------------------------------------
// LogEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System;
using System.Diagnostics;

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

        SetRaidInfo();
        ErrorPostAnalysis(logParseResults);

        return _raidEntries;
    }

    private void AddRaidInfoEntries()
    {
        IEnumerable<string> zoneNames = _raidEntries.AttendanceEntries
            .OrderBy(x => x.Timestamp)
            .Select(x => GetZoneRaidAlias(x.ZoneName ?? ""))
            .Distinct();

        foreach (string zoneName in zoneNames)
        {
            RaidInfo raidInfo = new() { RaidZone = zoneName };
            _raidEntries.Raids.Add(raidInfo);
        }

        foreach (RaidInfo raidInfo in _raidEntries.Raids)
        {
            IOrderedEnumerable<AttendanceEntry> attendancesForRaid = _raidEntries.AttendanceEntries
                .Where(x => GetZoneRaidAlias(x.ZoneName) == raidInfo.RaidZone)
                .OrderBy(x => x.Timestamp);

            raidInfo.FirstAttendanceCall = attendancesForRaid.First();
            raidInfo.LastAttendanceCall = attendancesForRaid.Last();
        }

        DateTime startTime = DateTime.MinValue;
        DateTime endTime = DateTime.MaxValue;
        for (int i = 0; i < _raidEntries.Raids.Count; i++)
        {
            RaidInfo currentRaidInfo = _raidEntries.Raids[i];
            currentRaidInfo.StartTime = startTime;

            if (i + 1 < _raidEntries.Raids.Count)
            {
                RaidInfo nextRaid = _raidEntries.Raids[i + 1];
                currentRaidInfo.EndTime = nextRaid.FirstAttendanceCall.Timestamp.AddSeconds(-2);
                startTime = nextRaid.FirstAttendanceCall.Timestamp;
            }
            else
            {
                currentRaidInfo.EndTime = DateTime.MaxValue;
            }
        }
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

    private void AssociateDkpEntriesWithAttendance()
    {
        foreach (DkpEntry dkpEntry in _raidEntries.DkpEntries)
        {
            RaidInfo associatedRaid = _raidEntries.Raids
                .FirstOrDefault(x => x.StartTime <= dkpEntry.Timestamp && dkpEntry.Timestamp <= x.EndTime);

            dkpEntry.AssociatedAttendanceCall = associatedRaid.LastAttendanceCall;
        }
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
        var grouped = from d in _raidEntries.DkpEntries
                      group d by new { PlayerName = d.PlayerName.ToUpper(), d.Item, d.DkpSpent } into de
                      where de.Count() > 1
                      select new { DkpEntries = de };

        foreach (var dkpEntryGroup in grouped)
        {
            foreach (DkpEntry dkpEntry in dkpEntryGroup.DkpEntries)
            {
                dkpEntry.PossibleError = PossibleError.DkpDuplicateEntry;
            }
        }
    }

    private void CheckPotentialLinkdeads()
    {
        List<AttendanceEntry> orderedAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
        foreach (PlayerCharacter player in _raidEntries.AllPlayersInRaid)
        {
            IEnumerable<AttendanceEntry> attendancesMissingFrom = _raidEntries.AttendanceEntries.Where(x => !x.Players.Contains(player));
            foreach (AttendanceEntry attendance in attendancesMissingFrom)
            {
                AttendanceEntry previousAttendance = orderedAttendances
                    .Where(x => x.Timestamp < attendance.Timestamp && x.AttendanceCallType != AttendanceCallType.Kill)
                    .LastOrDefault();

                AttendanceEntry nextAttendance = orderedAttendances
                    .Where(x => x.Timestamp > attendance.Timestamp && x.AttendanceCallType != AttendanceCallType.Kill)
                    .FirstOrDefault();

                bool playerInPreviousAttendance = previousAttendance == null || previousAttendance.Players.Any(x => x.PlayerName == player.PlayerName);
                bool playerInNextAttendance = nextAttendance == null || nextAttendance.Players.Any(x => x.PlayerName == player.PlayerName);
                if (playerInPreviousAttendance && playerInNextAttendance)
                {
                    PlayerPossibleLinkdead ld = new()
                    {
                        Player = player,
                        AttendanceMissingFrom = attendance
                    };
                    _raidEntries.PossibleLinkdeads.Add(ld);
                }
            }
        }
    }

    private void CheckRaidBossTypo(LogParseResults logParseResults)
    {
        ICollection<string> bossMobNames = _settings.RaidValue.AllBossMobNames;

        // Dont bother checking if the file wasnt found
        if (bossMobNames.Count == 0)
            return;

        foreach (AttendanceEntry killCall in _raidEntries.AttendanceEntries.Where(x => x.AttendanceCallType == AttendanceCallType.Kill))
        {
            if (!bossMobNames.Contains(killCall.RaidName))
            {
                killCall.PossibleError = PossibleError.BossMobNameTypo;
            }
        }
    }

    private void CheckRaidNameErrors()
    {
        string shortestBossName = _settings.RaidValue.AllBossMobNames.MinBy(x => x.Length);
        int minBossNameLength = shortestBossName == null ? 5 : shortestBossName.Length;
        foreach (AttendanceEntry attendance in _raidEntries.AttendanceEntries)
        {
            int minLength = attendance.AttendanceCallType == AttendanceCallType.Time
                ? Constants.MinimumRaidNameLength
                : minBossNameLength;

            if (attendance.RaidName.Length < minLength)
                attendance.PossibleError = PossibleError.RaidNameTooShort;
        }
    }

    private void ErrorPostAnalysis(LogParseResults logParseResults)
    {
        CheckDuplicateAttendanceEntries(logParseResults);
        CheckRaidBossTypo(logParseResults);
        CheckDkpSpentTypos(logParseResults);
        CheckDuplicateDkpEntries(logParseResults);
        CheckRaidNameErrors();
        CheckPotentialLinkdeads();
    }

    private PlayerJoinRaidEntry ExtractePlayerJoin(EqLogEntry entry)
    {
        // [Tue Feb 27 23:13:23 2024] Orsino has left the raid.
        // [Tue Feb 27 23:14:20 2024] Marco joined the raid.
        // [Sun Feb 25 22:52:46 2024] You have joined the group.
        // [Thu Feb 22 23:13:52 2024] Luciania joined the raid.
        // [Thu Feb 22 23:13:52 2024] You have joined the raid.
        int indexOfLastBracket = entry.LogLine.IndexOf(']');
        string entryMessage = entry.LogLine[(indexOfLastBracket + 2)..].Trim();
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

    private PlayerLooted ExtractPlayerLooted(EqLogEntry entry)
    {
        // [Wed Feb 21 18:49:31 2024] --Orsino has looted a Part of Tasarin's Grimoire Pg. 24.--
        // [Wed Feb 21 16:34:07 2024] --You have looted a Bloodstained Key.--
        int indexOfFirstDashes = entry.LogLine.IndexOf(Constants.DoubleDash);
        int startIndex = indexOfFirstDashes + Constants.DoubleDash.Length;
        int endIndex = entry.LogLine.Length - Constants.EndLootedDashes.Length;
        string lootString = entry.LogLine[startIndex..endIndex];

        int indexOfSpace = lootString.IndexOf(' ');
        string playerName = lootString[0..indexOfSpace];

        int indexOfLooted = lootString.IndexOf(Constants.LootedA);
        int startIndexOfItem = indexOfLooted + Constants.LootedA.Length;
        string itemName = lootString[startIndexOfItem..];

        entry.Visited = true;

        return new PlayerLooted
        {
            PlayerName = playerName,
            ItemLooted = itemName,
            Timestamp = entry.Timestamp,
            RawLogLine = entry.LogLine
        };
    }

    private string GetZoneRaidAlias(string zoneName)
        => _settings.RaidValue.GetZoneRaidAlias(zoneName);

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

    private void SetRaidInfo()
    {
        AddRaidInfoEntries();
        AssociateDkpEntriesWithAttendance();
    }
}

[DebuggerDisplay("{DebugDisplay}")]
public sealed class PlayerPossibleLinkdead
{
    public bool Addressed { get; set; }

    public AttendanceEntry AttendanceMissingFrom { get; init; }

    public PlayerCharacter Player { get; init; }

    private string DebugDisplay
        => $"{Player} {AttendanceMissingFrom}";
}

public interface ILogEntryAnalyzer
{
    RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults);
}
