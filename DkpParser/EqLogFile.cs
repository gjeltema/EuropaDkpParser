// -----------------------------------------------------------------------
// EqLogFile.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugDisplay,nq}")]
public sealed class EqLogFile
{
    public IList<EqLogEntry> LogEntries { get; } = [];

    /// <summary>
    /// The file name of this log file.
    /// </summary>
    public string LogFile { get; init; }

    private string DebugDisplay
        => $"{LogFile} {LogEntries.Count}";

    public IEnumerable<string> GetAllLogLines()
    {
        DateTime firstTimestamp = LogEntries[0].Timestamp;
        yield return $"{firstTimestamp.ToEqLogTimestamp()} ----------------------- {LogFile} Begin --------------------------";

        foreach (EqLogEntry logEntry in LogEntries)
            yield return logEntry.FullLogLine;

        yield return "";
        yield return "";
    }
}
