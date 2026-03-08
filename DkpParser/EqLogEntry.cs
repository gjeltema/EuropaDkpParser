// -----------------------------------------------------------------------
// EqLogEntry.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugDisplay,nq}")]
public sealed class EqLogEntry
{
    public EqChannel Channel { get; set; } = EqChannel.None;

    public LogEntryType EntryType { get; set; } = LogEntryType.Unknown;

    public PossibleError ErrorType { get; set; } = PossibleError.None;

    public string FullLogLine
        => $"{Timestamp.ToEqLogTimestamp()} {LogLine}";

    public string LogLine { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Used for debugging, seeing what entries may not have been touched by the analyzer.
    /// </summary>
    public bool Visited { get; set; } = false;

    private string DebugDisplay
    {
        get
        {
            if (LogLine.Length < 20)
                return $"{Timestamp:HH:mm:ss} {EntryType} {LogLine}";

            return $"{Timestamp:HH:mm:ss} {EntryType} {LogLine[..20]}...";
        }
    }
}
