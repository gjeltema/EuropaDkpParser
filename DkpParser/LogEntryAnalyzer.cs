// -----------------------------------------------------------------------
// LogEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class LogEntryAnalyzer : ILogEntryAnalyzer
{
    public RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults)
    {
        RaidEntries raidEntries = new();

        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IList<EqLogEntry> logEntries = log.LogEntries;
            for (int logEntryIndex = 0; logEntryIndex < logEntries.Count; logEntryIndex++)
            {
                EqLogEntry logEntry = logEntries[logEntryIndex];
                switch (logEntry.EntryType)
                {
                    case LogEntryType.Attendance:
                    case LogEntryType.Kill:
                        HandleAttendance(ref logEntryIndex, logEntries, logEntry);
                        break;
                    default:
                        break;
                }

            }
        }

        return raidEntries;
    }

    private void HandleAttendance(ref int logEntryIndex, IList<EqLogEntry> logEntries, EqLogEntry logEntry)
    {

    }
}

public interface ILogEntryAnalyzer
{
    RaidEntries AnalyzeRaidLogEntries(LogParseResults logParseResults);
}
