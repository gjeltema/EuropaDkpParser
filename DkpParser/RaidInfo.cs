// -----------------------------------------------------------------------
// RaidInfo.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;
using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class RaidInfo
{
    public DateTime EndTime { get; set; }

    public AttendanceEntry FirstAttendanceCall { get; set; }

    public AttendanceEntry LastAttendanceCall { get; set; }

    public string RaidZone { get; set; }

    public DateTime StartTime { get; set; }

    private string DebugText
        => $"{RaidZone} {StartTime:HH:mm:ss}";
}
