// -----------------------------------------------------------------------
// EqLogFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class EqLogFile
{
    public IList<EqLogEntry> LogEntries { get; } = [];

    public string LogFile { get; init; }
}
