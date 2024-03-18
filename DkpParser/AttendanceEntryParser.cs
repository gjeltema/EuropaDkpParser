// -----------------------------------------------------------------------
// AttendanceEntryParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class AttendanceEntryParser : IParseEntry
{
    private readonly EqLogFile _logFile;
    private readonly ISetParser _setParser;
    private IParseEntry _populationListingParser;
    private IStartParseEntry _populationListingStartParser;

    internal AttendanceEntryParser(ISetParser setParser, EqLogFile logFile)
    {
        _setParser = setParser;
        _logFile = logFile;

        _populationListingParser = new PopulationListingParser(setParser, logFile, this);
        _populationListingStartParser = new PopulationListingStartParser(setParser, this, _populationListingParser);
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (!logLine.Contains(Constants.PossibleErrorDelimiter))
            return;

        EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
        _logFile.LogEntries.Add(logEntry);
        CheckForTwoColonError(logEntry, logLine);

        if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.DkpSpent;
        }
        else if (logLine.Contains(Constants.KillCall, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.Kill;
            _populationListingStartParser.SetStartTimeStamp(entryTimeStamp);
            _setParser.SetParser(_populationListingStartParser);

        }
        else if (logLine.Contains(Constants.Attendance, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.Attendance;
            _populationListingStartParser.SetStartTimeStamp(entryTimeStamp);
            _setParser.SetParser(_populationListingStartParser);
        }
    }

    private void CheckForTwoColonError(EqLogEntry logEntry, string logLine)
    {
        if (!logLine.Contains(Constants.AttendanceDelimiter))
        {
            logEntry.ErrorType = PossibleError.TwoColons;
        }
    }

    private EqLogEntry CreateLogEntry(string logLine, DateTime entryTimeStamp)
        => new() { LogLine = logLine, Timestamp = entryTimeStamp };
}
