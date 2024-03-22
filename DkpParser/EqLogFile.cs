// -----------------------------------------------------------------------
// EqLogFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugDisplay,nq")]
public sealed class EqLogFile
{
    public IList<EqLogEntry> LogEntries { get; } = [];

    public string LogFile { get; init; }

    private string DebugDisplay
        => $"{LogFile} {LogEntries.Count}";
}
