// -----------------------------------------------------------------------
// EqLogEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugDisplay,nq}")]
public sealed class EqLogEntry
{
    public LogEntryType EntryType { get; set; } = LogEntryType.Unknown;

    public PossibleError ErrorType { get; set; } = PossibleError.None;

    public string LogLine { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Used for debugging, seeing what entries may not have been touched by the analyzer.
    /// </summary>
    public bool Visited { get; set; } = false;

    private string DebugDisplay
        => $"{EntryType} {LogLine.Substring(Constants.LogDateTimeLength, 10)}...";
}
