// -----------------------------------------------------------------------
// IParseEntry.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Parses one line from a log file.
/// </summary>
public interface IParseEntry
{
    void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp);
}

/// <summary>
/// Parses an entire EQ Log file.
/// </summary>
public interface IEqLogParser : ISetEntryParser
{
    EqLogFile ParseLogFile(string fullFilePath, DateTime startTime, DateTime endTime);
}

/// <summary>
/// Sets what EntryParser should be used next.
/// </summary>
public interface ISetEntryParser
{
    void SetEntryParser(IParseEntry parseEntry);
}
