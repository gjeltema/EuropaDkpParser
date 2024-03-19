// -----------------------------------------------------------------------
// AttendanceEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class AttendanceEntry
{
    public AttendanceCallType AttendanceCallType { get; set; }

    public ICollection<string> PlayerNames { get; private set; } = new HashSet<string>();

    public DateTime Timestamp { get; set; }

    public string ZoneName { get; set; }
}
