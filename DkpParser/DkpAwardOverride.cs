// -----------------------------------------------------------------------
// DkpAwardOverride.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText}")]
public sealed class DkpAwardOverride
{
    public int DkpAmount { get; init; }

    public DateTime EndTime { get; set; } = DateTime.MaxValue;

    public string LogLine { get; init; }

    public DateTime StartTime { get; init; }

    private string DebugText
        => $"Start:{StartTime:HH:mm:ss} End:{EndTime:HH:mm:ss} DKP:{DkpAmount}";

    public bool IsOverridden(DateTime timestamp)
        => StartTime <= timestamp && timestamp <= EndTime;
}
