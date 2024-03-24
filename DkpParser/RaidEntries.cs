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
}
