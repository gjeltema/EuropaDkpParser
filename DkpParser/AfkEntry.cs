// -----------------------------------------------------------------------
// AfkEntry.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class AfkEntry
{
    public PlayerCharacter Character { get; init; }

    public DateTime EndTime { get; init; }

    public string LogLine { get; init; }

    public DateTime StartTime { get; init; }

    private string DebugDisplay
        => $"{Character} {StartTime:HH:mm:ss} {EndTime:HH:mm:ss}";

    public bool IsTimeWithinAfkPeriod(DateTime time)
        => StartTime <= time && time <= EndTime;

    public string ToDisplayString()
        => $"{Character} Start AFK:{StartTime:HH:mm:ss}, End AFK:{EndTime:HH:mm:ss}";
}
