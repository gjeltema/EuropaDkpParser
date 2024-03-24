// -----------------------------------------------------------------------
// RaidEntries.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("Att: {AttendanceEntries.Count}, DKP: {DkpEntries.Count}")]
public sealed class RaidEntries
{
    public ICollection<string> AllPlayersInRaid { get; set; } = new HashSet<string>();

    public ICollection<AttendanceEntry> AttendanceEntries { get; init; } = new List<AttendanceEntry>();

    public ICollection<DkpEntry> DkpEntries { get; set; } = new List<DkpEntry>();

    public ICollection<PlayerLooted> PlayerLootedEntries { get; set; }

    public ICollection<EqLogEntry> UnvisitedEntries { get; set; } = new List<EqLogEntry>();

    public IEnumerable<string> GetAllEntries()
    {
        yield return "-------------------- Attendance Entries -------------------";
        foreach (AttendanceEntry attEntry in AttendanceEntries)
        {
            yield return attEntry.ToString();
            foreach (string player in attEntry.PlayerNames)
            {
                yield return player;
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
        foreach (string playersInRaid in AllPlayersInRaid)
            yield return playersInRaid;

        yield return "";

        yield return "-------------------- Unvisited Entries -------------------";
        foreach (EqLogEntry unvisited in UnvisitedEntries)
            yield return unvisited.LogLine;

        yield return "";
    }
}
