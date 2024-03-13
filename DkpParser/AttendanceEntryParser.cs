namespace DkpParser;

internal sealed class AttendanceEntryParser : IParseEntry
{
    private readonly ISetParser _setParser;

    internal AttendanceEntryParser(ISetParser setParser)
    {
        _setParser = setParser;
    }

    public EqLogEntry ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (!logLine.Contains(Constants.PossibleErrorDelimiter))
            return null;

        if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
        {
            EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
            logEntry.EntryType = LogEntryType.DkpSpent;
            CheckForTwoColonError(logEntry, logLine);
            return logEntry;
        }
        else if (logLine.Contains(Constants.KillCall, StringComparison.OrdinalIgnoreCase))
        {
            EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
            logEntry.EntryType = LogEntryType.Kill;
            CheckForTwoColonError(logEntry, logLine);
            //** Switch to parser that looks for population listing
            return logEntry;
        }
        else if (logLine.Contains(Constants.Attendance, StringComparison.OrdinalIgnoreCase))
        {
            EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
            logEntry.EntryType = LogEntryType.Attendance;
            CheckForTwoColonError(logEntry, logLine);
            //** Switch to parser that looks for population listing
            return logEntry;
        }        

        return null;
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
