// -----------------------------------------------------------------------
// IParseEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public interface IParseEntry
{
    EqLogEntry ParseEntry(string logLine, DateTime entryTimeStamp);
}
