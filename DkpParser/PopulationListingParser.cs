// -----------------------------------------------------------------------
// PopulationListingParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class PopulationListingParser : IParseEntry
{
    private readonly IParseEntry _attendanceEntryParser;
    private readonly EqLogFile _logFile;
    private readonly ISetParser _setParser;

    internal PopulationListingParser(ISetParser setParser, EqLogFile logFile, IParseEntry attendanceEntryParser)
    {
        _setParser = setParser;
        _logFile = logFile;
        _attendanceEntryParser = attendanceEntryParser;
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (logLine.EndsWith(Constants.EuropaGuildTag))
        {
            EqLogEntry logEntry = new()
            {
                LogLine = logLine,
                Timestamp = entryTimeStamp,
                EntryType = LogEntryType.PlayerName
            };
            _logFile.LogEntries.Add(logEntry);
        }
        else if (logLine.Contains(Constants.WhoZonePrefixPlural) || logLine.Contains(Constants.WhoZonePrefixSingle))
        {
            EqLogEntry logEntry = new()
            {
                LogLine = logLine,
                Timestamp = entryTimeStamp,
                EntryType = LogEntryType.WhoZoneName
            };
            _logFile.LogEntries.Add(logEntry);

            _setParser.SetParser(_attendanceEntryParser);
        }
    }
}
