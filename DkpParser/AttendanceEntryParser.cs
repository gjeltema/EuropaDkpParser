// -----------------------------------------------------------------------
// AttendanceEntryParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class AttendanceEntryParser : IParseEntry
{
    private readonly EqLogFile _logFile;
    private readonly ISetParser _setParser;

    internal AttendanceEntryParser(ISetParser setParser, EqLogFile logFile)
    {
        _setParser = setParser;
        _logFile = logFile;
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
            //** Switch to parser that looks for population listing
            //** Create new instance to remove a bool flag?
        }
        else if (logLine.Contains(Constants.Attendance, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.Attendance;
            //** Switch to parser that looks for population listing
            //** Create new instance to remove a bool flag?
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
