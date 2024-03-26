// -----------------------------------------------------------------------
// PrimaryEntryParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class PrimaryEntryParser : IParseEntry
{
    private readonly EqLogFile _logFile;
    private readonly ISetEntryParser _setParser;
    private IParseEntry _populationListingParser;
    private IStartParseEntry _populationListingStartParser;

    internal PrimaryEntryParser(ISetEntryParser setParser, EqLogFile logFile)
    {
        _setParser = setParser;
        _logFile = logFile;

        _populationListingParser = new PopulationListingParser(setParser, logFile, this);
        _populationListingStartParser = new PopulationListingStartParser(setParser, this, _populationListingParser);
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (logLine.Contains(Constants.PossibleErrorDelimiter))
        {
            AddDelimiterEntry(logLine, entryTimeStamp);
        }
        else if (logLine.EndsWith(Constants.EndLootedDashes) && logLine.Contains(Constants.Looted))
        {
            AddLootedEntry(logLine, entryTimeStamp);
        }
    }

    private void AddDelimiterEntry(string logLine, DateTime entryTimeStamp)
    {
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
            _setParser.SetEntryParser(_populationListingStartParser);
        }
        else if (logLine.Contains(Constants.RaidAttendanceTaken, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.Attendance;
            _populationListingStartParser.SetStartTimeStamp(entryTimeStamp);
            _setParser.SetEntryParser(_populationListingStartParser);
        }
    }

    private void AddLootedEntry(string logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
        logEntry.EntryType = LogEntryType.PlayerLooted;
        _logFile.LogEntries.Add(logEntry);
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
