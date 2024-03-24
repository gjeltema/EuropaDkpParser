// -----------------------------------------------------------------------
// AttendanceEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{RaidName,nq} {AttendanceCallType,nq}")]
public sealed class AttendanceEntry
{
    public AttendanceCallType AttendanceCallType { get; set; }

    public ICollection<string> PlayerNames { get; private set; } = new HashSet<string>();

    public string RaidName { get; set; }

    public DateTime Timestamp { get; set; }

    public string ZoneName { get; set; }

    public PossibleError PossibleError { get; set; }

    public override string ToString()
        => $"{Timestamp:HH:mm:ss} {AttendanceCallType}\t{RaidName}";
}
