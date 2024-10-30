// -----------------------------------------------------------------------
// DkpUploadInfo.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class DkpUploadInfo
{
    public AttendanceEntry AssociatedAttendanceCall { get; init; }

    public string CharacterName { get; init; }

    public int DkpSpent { get; init; }

    public string Item { get; init; }

    public DateTime Timestamp { get; init; }

    private string DebugText
        => $"{CharacterName}, {Item} {DkpSpent}";

    public override string ToString()
        => DebugText;
}
