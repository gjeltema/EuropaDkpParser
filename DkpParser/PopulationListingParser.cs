// -----------------------------------------------------------------------
// PopulationListingParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class PopulationListingParser : IParseEntry
{
    private readonly EqLogFile _logFile;
    private readonly IParseEntry _primaryEntryParser;
    private readonly ISetEntryParser _setParser;

    internal PopulationListingParser(ISetEntryParser setParser, EqLogFile logFile, IParseEntry primaryEntryParser)
    {
        _setParser = setParser;
        _logFile = logFile;
        _primaryEntryParser = primaryEntryParser;
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

            _setParser.SetEntryParser(_primaryEntryParser);
        }
    }
}
