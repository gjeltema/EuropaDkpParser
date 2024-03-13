// -----------------------------------------------------------------------
// EqLogEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class EqLogEntry
{
    public LogEntryType EntryType { get; set; }

    public PossibleError ErrorType { get; set; } = PossibleError.None;

    public string LogLine { get; set; }

    public DateTime Timestamp { get; set; }
}
