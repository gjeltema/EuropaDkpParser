// -----------------------------------------------------------------------
// LogEntryAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System;
using System.Diagnostics;
using Gjeltema.Logging;

public sealed class LogEntryAnalyzer : ILogEntryAnalyzer
{
    private const string LogPrefix = $"[{nameof(LogEntryAnalyzer)}]";
    private static readonly TimeSpan JoinedTimeLimit = TimeSpan.FromMinutes(15);
    private readonly RaidEntries _raidEntries = new();
    private readonly IDkpParserSettings _settings;

    public LogEntryAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults)
    {
        Log.Debug($"{LogPrefix} Beginning {nameof(AnalyzeRaidLogEntries)}");

        PopulateMemberLists(logParseResults);
        PopulateLootedList(logParseResults);
        PopulateRaidJoin(logParseResults);

        AnalyzeAttendanceCalls(logParseResults);
        AnalyzeLootCalls(logParseResults);

        AddUnvisitedEntries(logParseResults);

        ErrorPostAnalysis();

        return _raidEntries;
    }

    private void AddUnvisitedEntries(LogParseResults logParseResults)
    {
        Log.Trace($"{LogPrefix} Unvisited entries:{Environment.NewLine}{string.Join(Environment.NewLine, GetUnvisitedEntries(logParseResults))}");
    }

    private void AnalyzeAttendanceCalls(LogParseResults logParseResults)
    {
        IAttendanceEntryAnalyzer attendanceAnalyzer = new AttendanceEntryAnalyzer(_settings);
        attendanceAnalyzer.AnalyzeAttendanceCalls(logParseResults, _raidEntries);
    }

    private void AnalyzeLootCalls(LogParseResults logParseResults)
    {
        IDkpEntryAnalyzer dkpEntryAnalyzer = new DkpEntryAnalyzer();
        dkpEntryAnalyzer.AnalyzeLootCalls(logParseResults, _raidEntries, _settings.CharactersOnDkpServer);
    }

    private void CheckDuplicateAttendanceEntries()
    {
        var grouped = from a in _raidEntries.AttendanceEntries
                      group a by a.CallName.ToUpper() into ae
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

    private void CheckDuplicateDkpEntries()
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
        // For each player in the raid, find any attendances they're missing from.
        List<AttendanceEntry> orderedAttendances = _raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
        foreach (PlayerCharacter playerCharacter in _raidEntries.AllCharactersInRaid)
        {
            IEnumerable<AttendanceEntry> attendancesMissingFrom = _raidEntries.AttendanceEntries.Where(x => !x.Characters.Contains(playerCharacter));

            // Check in between a Joined and Left call to see if the player is missing from any of the attendances in between.  Limit the time between
            // Joined and Left calls to 15 minutes.
            IEnumerable<CharacterJoinRaidEntry> playerJoinedOrLeftCalls = _raidEntries.CharacterJoinCalls
                .Where(x => x.CharacterName == playerCharacter.CharacterName)
                .OrderBy(x => x.Timestamp);

            CharacterJoinRaidEntry lastLeft = null;
            foreach (CharacterJoinRaidEntry playerJoinedOrLeft in playerJoinedOrLeftCalls)
            {
                if (playerJoinedOrLeft.EntryType == LogEntryType.LeftRaid)
                {
                    lastLeft = playerJoinedOrLeft;
                }
                else if (lastLeft == null)
                {
                    continue;
                }
                else // Player Joined message
                {
                    try
                    {
                        if (playerJoinedOrLeft.Timestamp - lastLeft.Timestamp <= JoinedTimeLimit)
                        {
                            IEnumerable<AttendanceEntry> missingAttendancesInBetween = attendancesMissingFrom
                                .Where(x => lastLeft.Timestamp <= x.Timestamp && x.Timestamp <= playerJoinedOrLeft.Timestamp);
                            foreach (AttendanceEntry missingAttendance in missingAttendancesInBetween)
                            {
                                if (_raidEntries.IsPlayerAfkFlagged(playerCharacter, missingAttendance.Timestamp))
                                    continue;

                                // Check to see if a possible linkdead for this character in this attendance was already entered from the earlier foreach.
                                PlayerPossibleLinkdead existingLinkdeadEntry = _raidEntries.PossibleLinkdeads
                                    .FirstOrDefault(x => x.Player.CharacterName == playerCharacter.CharacterName && x.AttendanceMissingFrom == missingAttendance);
                                if (existingLinkdeadEntry == null)
                                {
                                    if (!_settings.CharactersOnDkpServer.IsRelatedCharacterInCollection(playerCharacter, missingAttendance.Characters))
                                        _raidEntries.PossibleLinkdeads.Add(new() { Player = playerCharacter, AttendanceMissingFrom = missingAttendance });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"{LogPrefix} Error when analyzing for potential linkdeads method: {playerJoinedOrLeft}{Environment.NewLine}{ex.ToLogMessage()}");
                    }
                }
            }
        }

        Log.Trace($"{LogPrefix} Possible Linkdeads:{Environment.NewLine}{string.Join(Environment.NewLine, _raidEntries.PossibleLinkdeads)}");
    }

    private void CheckRaidBossTypo()
    {
        ICollection<string> bossMobNames = _settings.RaidValue.AllBossMobNames;

        // Dont bother checking if the data wasnt configured
        if (bossMobNames.Count == 0)
            return;

        foreach (AttendanceEntry killCall in _raidEntries.AttendanceEntries.Where(x => x.AttendanceCallType == AttendanceCallType.Kill))
        {
            if (!bossMobNames.Contains(killCall.CallName))
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

            if (attendance.CallName.Length < minLength)
                attendance.PossibleError = PossibleError.RaidNameTooShort;
        }
    }

    private void ErrorPostAnalysis()
    {
        CheckDuplicateAttendanceEntries();
        CheckRaidBossTypo();
        CheckDuplicateDkpEntries();
        CheckRaidNameErrors();
        CheckPotentialLinkdeads();
    }

    private CharacterJoinRaidEntry ExtractePlayerJoin(EqLogEntry entry)
    {
        // [Tue Feb 27 23:13:23 2024] Orsino has left the raid.
        // [Tue Feb 27 23:14:20 2024] Marco joined the raid.
        // [Sun Feb 25 22:52:46 2024] You have joined the group.
        // [Thu Feb 22 23:13:52 2024] Luciania joined the raid.
        // [Thu Feb 22 23:13:52 2024] You have joined the raid.

        entry.Visited = true;

        string logLine = entry.LogLine;
        if (logLine.Contains("You have "))
            return null;

        int indexOfSpace = logLine.IndexOf(' ');
        if (indexOfSpace < 2)
        {
            Log.Warning($"{LogPrefix} Unable to validate log entry is a Player Joined/Left raid entry: {entry.FullLogLine}");
            return null;
        }

        string playerName = logLine[0..indexOfSpace];
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Log.Warning($"{LogPrefix} Unable to find player name in a Player Joined/Left raid entry: {entry.FullLogLine}");
            return null;
        }

        return new CharacterJoinRaidEntry
        {
            CharacterName = playerName.Trim(),
            Timestamp = entry.Timestamp,
            EntryType = entry.EntryType
        };
    }

    private PlayerLooted ExtractPlayerLooted(EqLogEntry entry)
    {
        entry.Visited = true;

        // [Wed Feb 21 18:49:31 2024] --Orsino has looted a Part of Tasarin's Grimoire Pg. 24.--
        // [Wed Feb 21 16:34:07 2024] --You have looted a Bloodstained Key.--
        int indexOfFirstDashes = entry.LogLine.IndexOf(Constants.DoubleDash);
        if (indexOfFirstDashes < 0)
        {
            Log.Warning($"{LogPrefix} Unable to validate log entry is a player looted entry: {entry.FullLogLine}");
            return null;
        }
        int startIndex = indexOfFirstDashes + Constants.DoubleDash.Length;
        int endIndex = entry.LogLine.Length - Constants.EndLootedDashes.Length;
        if (startIndex >= endIndex)
        {
            Log.Warning($"{LogPrefix} Unable to validate log entry is a player looted entry: {entry.FullLogLine}");
            return null;
        }

        string lootString = entry.LogLine[startIndex..endIndex].Trim();
        if (string.IsNullOrWhiteSpace(lootString))
        {
            Log.Warning($"{LogPrefix} Unable to extract string after timestamp from player looted entry: {entry.FullLogLine}");
            return null;
        }

        int indexOfSpace = lootString.IndexOf(' ');
        if (indexOfSpace < 1)
        {
            Log.Warning($"{LogPrefix} Unable to validate log entry is a player looted entry: {entry.FullLogLine}");
            return null;
        }

        string playerName = lootString[0..indexOfSpace];
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Log.Warning($"{LogPrefix} Unable to extract player name from player looted entry: {entry.FullLogLine}");
            return null;
        }

        int indexOfLooted = lootString.IndexOf(Constants.LootedA);
        int startIndexOfItem = indexOfLooted + Constants.LootedA.Length;
        if (indexOfLooted < 1)
        {
            Log.Warning($"{LogPrefix} Unable to validate log entry is a player looted entry: {entry.FullLogLine}");
            return null;
        }

        string itemName = lootString[startIndexOfItem..];
        if (string.IsNullOrWhiteSpace(itemName))
        {
            Log.Warning($"{LogPrefix} Unable to extract looted item from player looted entry: {entry.FullLogLine}");
            return null;
        }

        return new PlayerLooted
        {
            PlayerName = playerName,
            ItemLooted = itemName,
            Timestamp = entry.Timestamp,
            RawLogLine = entry.FullLogLine
        };
    }

    private IEnumerable<string> GetUnvisitedEntries(LogParseResults logParseResults)
        => from logFile in logParseResults.EqLogFiles
           from entry in logFile.LogEntries
           where !entry.Visited
           select entry.FullLogLine;

    private void PopulateLootedList(LogParseResults logParseResults)
    {
        List<PlayerLooted> lootedEntries = [];
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<PlayerLooted> playersLooted = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.CharacterLooted)
                .Select(ExtractPlayerLooted)
                .Where(x => x != null);

            lootedEntries.AddRange(playersLooted);
        }

        _raidEntries.PlayerLootedEntries = lootedEntries;
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

        foreach (ZealRaidAttendanceFile zealRaidList in logParseResults.ZealRaidAttendanceFiles)
        {
            foreach (PlayerCharacter playerChar in zealRaidList.CharacterNames)
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
        List<CharacterJoinRaidEntry> joins = [];
        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<CharacterJoinRaidEntry> playersJoined = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.JoinedRaid || x.EntryType == LogEntryType.LeftRaid)
                .Select(ExtractePlayerJoin)
                .Where(x => x != null);

            joins.AddRange(playersJoined);
        }

        _raidEntries.CharacterJoinCalls = joins;
    }
}

[DebuggerDisplay("{DebugDisplay}")]
public sealed class PlayerPossibleLinkdead
{
    public bool Addressed { get; set; }

    public AttendanceEntry AttendanceMissingFrom { get; init; }

    public PlayerCharacter Player { get; init; }

    private string DebugDisplay
        => $"{Player.CharacterName} {AttendanceMissingFrom}";

    public override string ToString()
        => DebugDisplay;
}

public interface ILogEntryAnalyzer
{
    RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults);
}
