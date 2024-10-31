// -----------------------------------------------------------------------
// RaidEntries.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("Att: {AttendanceEntries.Count}, DKP: {DkpEntries.Count}, Players: {AllCharactersInRaid.Count}")]
public sealed class RaidEntries
{
    public ICollection<AfkEntry> AfkEntries { get; } = new List<AfkEntry>();

    public ICollection<PlayerCharacter> AllCharactersInRaid { get; set; } = new HashSet<PlayerCharacter>();

    public ICollection<string> AnalysisErrors { get; } = new List<string>();

    public TimeSpan AnalysisTime { get; set; } = TimeSpan.Zero;

    public ICollection<AttendanceEntry> AttendanceEntries { get; set; } = new List<AttendanceEntry>();

    public ICollection<CharacterJoinRaidEntry> CharacterJoinCalls { get; set; } = new List<CharacterJoinRaidEntry>();

    public ICollection<DkpEntry> DkpEntries { get; set; } = new List<DkpEntry>();

    public TimeSpan ParseTime { get; set; } = TimeSpan.Zero;

    public ICollection<PlayerLooted> PlayerLootedEntries { get; set; }

    public ICollection<PlayerPossibleLinkdead> PossibleLinkdeads { get; } = new List<PlayerPossibleLinkdead>();

    public ICollection<DkpEntry> RemovedDkpEntries { get; set; } = new List<DkpEntry>();

    public ICollection<PlayerCharacter> RemovedPlayerCharacters { get; } = new HashSet<PlayerCharacter>();

    public ICollection<EqLogEntry> UnvisitedEntries { get; set; } = new List<EqLogEntry>();

    public void AddOrMergeInPlayerCharacter(PlayerCharacter playerCharacter)
    {
        PlayerCharacter currentChar = AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == playerCharacter.CharacterName);
        if (currentChar != null)
        {
            currentChar.Merge(playerCharacter);
        }
        else
        {
            AllCharactersInRaid.Add(playerCharacter);
        }
    }

    public IEnumerable<string> GetAllDkpspentEntries(Func<string, string> getZoneRaidAlias)
    {
        ICollection<RaidInfo> raids = GetRaidInfo(getZoneRaidAlias);

        foreach (RaidInfo raid in raids)
        {
            yield return $"================= {raid.RaidZone} =================";

            foreach (string dkpEntryText in GetDkpspentEntriesForRaid(raid))
                yield return dkpEntryText;

            yield return "";
        }
    }

    public IEnumerable<string> GetAllEntries()
    {
        yield return "-------------------- Analyzer Errors -------------------";
        foreach (string error in AnalysisErrors)
            yield return error;

        yield return "-------------------- Attendance Entries -------------------";
        foreach (AttendanceEntry attEntry in AttendanceEntries)
        {
            yield return attEntry.ToDebugString();
            foreach (PlayerCharacter player in attEntry.Characters)
            {
                yield return player.ToDisplayString();
            }
        }

        yield return "";

        yield return "-------------------- DKP Entries -------------------";
        foreach (DkpEntry dkpEntry in DkpEntries)
            yield return dkpEntry.ToDebugString();

        yield return "";

        yield return "-------------------- Player Looted Entries -------------------";
        foreach (PlayerLooted playerLootedEntry in PlayerLootedEntries)
            yield return playerLootedEntry.ToString();

        yield return "";

        yield return "-------------------- All Players Found In Raid -------------------";
        foreach (PlayerCharacter playerInRaid in AllCharactersInRaid.OrderBy(x => x.CharacterName))
            yield return playerInRaid.ToDisplayString();

        yield return "";

        yield return "-------------------- Players Joined or Left Raid -------------------";
        foreach (CharacterJoinRaidEntry playerJoinedRaid in CharacterJoinCalls.OrderBy(x => x.Timestamp))
            yield return playerJoinedRaid.ToDisplayString();

        yield return "";

        yield return "-------------------- Players Declared AFK -------------------";
        foreach (AfkEntry afkEntry in AfkEntries.OrderBy(x => x.StartTime))
            yield return afkEntry.ToDisplayString();

        yield return "";

        yield return "-------------------- Unvisited Entries -------------------";
        foreach (EqLogEntry unvisited in UnvisitedEntries)
            yield return unvisited.LogLine;

        yield return "";
    }

    public IEnumerable<string> GetDkpspentEntriesForRaid(RaidInfo raid)
    {
        IEnumerable<DkpEntry> dkpEntries = DkpEntries
                .Where(x => raid.StartTime <= x.Timestamp && x.Timestamp <= raid.EndTime)
                .OrderBy(x => x.Timestamp);

        foreach (DkpEntry dkpEntry in dkpEntries)
            yield return dkpEntry.ToLogString();
    }

    public ICollection<RaidInfo> GetRaidInfo(Func<string, string> getZoneRaidAlias)
    {
        List<RaidInfo> raidInfo = [];

        IEnumerable<string> zoneNames = AttendanceEntries
            .Select(x => getZoneRaidAlias(x.ZoneName))
            .Distinct();

        foreach (string zoneName in zoneNames)
        {
            IOrderedEnumerable<AttendanceEntry> attendancesForRaid = AttendanceEntries
                .Where(x => getZoneRaidAlias(x.ZoneName) == zoneName)
                .OrderBy(x => x.Timestamp);

            RaidInfo newRaidInfo = new()
            {
                RaidZone = zoneName,
                FirstAttendanceCall = attendancesForRaid.First(),
                LastAttendanceCall = attendancesForRaid.Last()
            };

            raidInfo.Add(newRaidInfo);
        }

        DateTime startTime = DateTime.MinValue;
        DateTime endTime = DateTime.MaxValue;
        for (int i = 0; i < raidInfo.Count; i++)
        {
            RaidInfo currentRaidInfo = raidInfo[i];
            currentRaidInfo.StartTime = startTime;

            if (i + 1 < raidInfo.Count)
            {
                RaidInfo nextRaid = raidInfo[i + 1];
                currentRaidInfo.EndTime = nextRaid.FirstAttendanceCall.Timestamp.AddSeconds(-2);
                startTime = nextRaid.FirstAttendanceCall.Timestamp;
            }
            else
            {
                currentRaidInfo.EndTime = DateTime.MaxValue;
            }
        }

        return raidInfo;
    }

    public void RemoveAttendance(AttendanceEntry toBeRemoved)
    {
        List<PlayerPossibleLinkdead> possibleLinkdeadsToRemove = PossibleLinkdeads.Where(x => x.AttendanceMissingFrom == toBeRemoved).ToList();
        foreach (PlayerPossibleLinkdead possibleLinkdead in possibleLinkdeadsToRemove)
        {
            PossibleLinkdeads.Remove(possibleLinkdead);
        }

        AttendanceEntries.Remove(toBeRemoved);
    }

    public ICollection<DkpEntry> RemoveCharacter(string characterName)
    {
        ICollection<DkpEntry> dkpSpentsToRemove = DkpEntries.Where(x => x.PlayerName.Equals(characterName, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (DkpEntry dkpToRemove in dkpSpentsToRemove)
        {
            DkpEntries.Remove(dkpToRemove);
            RemovedDkpEntries.Add(dkpToRemove);
        }

        PlayerCharacter playerChar = AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == characterName);
        if (playerChar == null)
            return dkpSpentsToRemove;

        ICollection<PlayerPossibleLinkdead> possibleLinkdeadsToRemove = PossibleLinkdeads.Where(x => x.Player == playerChar).ToList();
        foreach (PlayerPossibleLinkdead ldChar in possibleLinkdeadsToRemove)
        {
            PossibleLinkdeads.Remove(ldChar);
        }

        IEnumerable<AttendanceEntry> attendancesToRemoveFrom = AttendanceEntries.Where(x => x.Characters.Contains(playerChar));
        foreach (AttendanceEntry attendance in attendancesToRemoveFrom)
        {
            attendance.Characters.Remove(playerChar);
        }

        AllCharactersInRaid.Remove(playerChar);
        RemovedPlayerCharacters.Add(playerChar);

        return dkpSpentsToRemove;
    }
}
