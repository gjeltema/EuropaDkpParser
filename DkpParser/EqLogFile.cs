// -----------------------------------------------------------------------
// EqLogFile.cs Copyright 2024 Craig Gjeltema
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
        yield return $"----------------------- {LogFile} Begin --------------------------";

        foreach (EqLogEntry logEntry in LogEntries)
            yield return logEntry.LogLine;

        yield return "";
        yield return "";
    }
}
