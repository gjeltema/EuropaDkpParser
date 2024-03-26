// -----------------------------------------------------------------------
// IParseEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public interface IParseEntry
{
    void ParseEntry(string logLine, DateTime entryTimeStamp);
}

public interface IEqLogParser : ISetEntryParser
{
    EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime);
}

public interface ISetEntryParser
{
    void SetEntryParser(IParseEntry parseEntry);
}
