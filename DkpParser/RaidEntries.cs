// -----------------------------------------------------------------------
// RaidEntries.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class RaidEntries
{
    public ICollection<AttendanceEntry> AttendanceEntries { get; init; } = new List<AttendanceEntry>();

    public ICollection<DkpEntry> DkpEntries { get; init; } = new List<DkpEntry>();
}
