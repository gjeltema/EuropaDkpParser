// -----------------------------------------------------------------------
// RaidEntries.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("Att: {AttendanceEntries.Count}, DKP: {DkpEntries.Count}, Players: {AllPlayersInRaid.Count}")]
public sealed class RaidEntries
{
    public ICollection<PlayerCharacter> AllPlayersInRaid { get; set; } = new HashSet<PlayerCharacter>();

    public ICollection<string> AnalysisErrors { get; } = new List<string>();

    public ICollection<AttendanceEntry> AttendanceEntries { get; set; } = new List<AttendanceEntry>();

    public ICollection<DkpEntry> DkpEntries { get; set; } = new List<DkpEntry>();

    public ICollection<PlayerJoinRaidEntry> PlayerJoinCalls { get; set; } = new List<PlayerJoinRaidEntry>();

    public ICollection<PlayerLooted> PlayerLootedEntries { get; set; }

    public ICollection<PlayerPossibleLinkdead> PossibleLinkdeads { get; } = new List<PlayerPossibleLinkdead>();

    public IList<RaidInfo> Raids { get; } = new List<RaidInfo>();

    public ICollection<EqLogEntry> UnvisitedEntries { get; set; } = new List<EqLogEntry>();

    public void AddOrMergeInPlayerCharacter(PlayerCharacter playerCharacter)
    {
        PlayerCharacter currentChar = AllPlayersInRaid.FirstOrDefault(x => x.PlayerName == playerCharacter.PlayerName);
        if (currentChar != null)
        {
            currentChar.Merge(playerCharacter);
        }
        else
        {
            AllPlayersInRaid.Add(playerCharacter);
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
            yield return attEntry.ToString();
            foreach (PlayerCharacter player in attEntry.Players)
            {
                yield return player.ToLogString();
            }
        }

        yield return "";

        yield return "-------------------- DKP Entries -------------------";
        foreach (DkpEntry dkpEntry in DkpEntries)
            yield return dkpEntry.ToString();

        yield return "";

        yield return "-------------------- Player Looted Entries -------------------";
        foreach (PlayerLooted playerLootedEntry in PlayerLootedEntries)
            yield return playerLootedEntry.ToString();

        yield return "";

        yield return "-------------------- All Players Found In Raid -------------------";
        foreach (PlayerCharacter playerInRaid in AllPlayersInRaid.OrderBy(x => x.PlayerName))
            yield return playerInRaid.ToDisplayString();

        yield return "";

        yield return "-------------------- Players Joined or Left Raid -------------------";
        foreach (PlayerJoinRaidEntry playerJoinedRaid in PlayerJoinCalls.OrderBy(x => x.Timestamp))
            yield return playerJoinedRaid.ToDisplayString();

        yield return "";

        yield return "-------------------- Unvisited Entries -------------------";
        foreach (EqLogEntry unvisited in UnvisitedEntries)
            yield return unvisited.LogLine;

        yield return "";
    }

    public IEnumerable<string> GetDkpspentEntries()
    {
        foreach (RaidInfo raid in Raids)
        {
            yield return $"================= {raid.RaidZone} =================";

            IEnumerable<DkpEntry> dkpEntries = DkpEntries
                .Where(x => raid.StartTime <= x.Timestamp && x.Timestamp <= raid.EndTime)
                .OrderBy(x => x.Timestamp);

            // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
            // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
            foreach (DkpEntry dkpEntry in dkpEntries)
            {
                string timestampText = dkpEntry.Timestamp.ToEqLogTimestamp();
                yield return $"{timestampText} {dkpEntry.ToLogString()}";
            }

            yield return "";
        }
    }

    public void UpdateRaids(Func<string, string> getZoneRaidAlias)
    {
        foreach (RaidInfo raidInfo in Raids)
        {
            IOrderedEnumerable<AttendanceEntry> attendancesForRaid = AttendanceEntries
                .Where(x => getZoneRaidAlias(x.ZoneName) == raidInfo.RaidZone)
                .OrderBy(x => x.Timestamp);

            raidInfo.FirstAttendanceCall = attendancesForRaid.First();
            raidInfo.LastAttendanceCall = attendancesForRaid.Last();
        }

        DateTime startTime = DateTime.MinValue;
        DateTime endTime = DateTime.MaxValue;
        for (int i = 0; i < Raids.Count; i++)
        {
            RaidInfo currentRaidInfo = Raids[i];
            currentRaidInfo.StartTime = startTime;

            if (i + 1 < Raids.Count)
            {
                RaidInfo nextRaid = Raids[i + 1];
                currentRaidInfo.EndTime = nextRaid.FirstAttendanceCall.Timestamp.AddSeconds(-2);
                startTime = nextRaid.FirstAttendanceCall.Timestamp;
            }
            else
            {
                currentRaidInfo.EndTime = DateTime.MaxValue;
            }
        }

        AssociateDkpEntriesWithAttendance();
    }

    private void AssociateDkpEntriesWithAttendance()
    {
        foreach (DkpEntry dkpEntry in DkpEntries)
        {
            RaidInfo associatedRaid = Raids
                .FirstOrDefault(x => x.StartTime <= dkpEntry.Timestamp && dkpEntry.Timestamp <= x.EndTime);

            if (associatedRaid == null)
            {
                string analysisError = $"Unable to find any associated attendance entry for DKPSPENT call: {dkpEntry.RawLogLine}";
                AnalysisErrors.Add(analysisError);
                continue;
            }

            dkpEntry.AssociatedAttendanceCall = associatedRaid.LastAttendanceCall;
        }
    }
}
