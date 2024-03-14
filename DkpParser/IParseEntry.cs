// -----------------------------------------------------------------------
// IParseEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public interface IParseEntry
{
    void ParseEntry(string logLine, DateTime entryTimeStamp);
}
