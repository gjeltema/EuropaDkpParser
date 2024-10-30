// -----------------------------------------------------------------------
// AttendanceUploadInfo.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class AttendanceUploadInfo
{
    public AttendanceCallType AttendanceCallType { get; init; }

    /// <summary>
    /// Either the call name if a time based entry (e.g. First Call), or the boss name if a Kill entry.
    /// </summary>
    public string CallName { get; init; }

    public ICollection<PlayerCharacter> Characters { get; init; } = new HashSet<PlayerCharacter>();

    public DateTime Timestamp { get; init; }

    public string ZoneName { get; init; }

    private string DebugText
        => $"{CallName} {AttendanceCallType}";

    public string ToDkpServerDescription()
        => AttendanceCallType == AttendanceCallType.Time
            ? $"Attendance - {CallName}"
            : $"{CallName} - KILL";
}
