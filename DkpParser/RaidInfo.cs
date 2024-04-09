// -----------------------------------------------------------------------
// RaidInfo.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class RaidInfo
{
    public DateTime EndTime { get; set; }

    public AttendanceEntry FirstAttendanceCall { get; set; }

    public AttendanceEntry LastAttendanceCall { get; set; }

    public string RaidZone { get; set; }

    public DateTime StartTime { get; set; }
}
