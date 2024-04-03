// -----------------------------------------------------------------------
// RaidEntries.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("Att: {AttendanceEntries.Count}, DKP: {DkpEntries.Count}, Players: {AllPlayersInRaid.Count}")]
public sealed class RaidEntries
{
    public ICollection<PlayerCharacter> AllPlayersInRaid { get; set; } = new HashSet<PlayerCharacter>();

    public ICollection<AttendanceEntry> AttendanceEntries { get; set; } = new List<AttendanceEntry>();

    public ICollection<DkpEntry> DkpEntries { get; set; } = new List<DkpEntry>();

    public ICollection<PlayerJoinRaidEntry> PlayerJoinCalls { get; set; } = new List<PlayerJoinRaidEntry>();

    public ICollection<PlayerLooted> PlayerLootedEntries { get; set; }

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
}
