// -----------------------------------------------------------------------
// LogParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Globalization;
using System.IO;

internal sealed class LogParser : ILogParser
{
    private readonly IDkpParserSettings _settings;

    public LogParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    // [Sat Mar 02 13:50:52 2024] Schitshow begins to cast a spell.
    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        bool foundBeginning = false;

        bool lookForAttendanceEntries = false;

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!GetTimeStamp(logLine, out DateTime entryTimeStamp))
            {
                // Log error?
                continue;
            }

            if (!foundBeginning)
            {
                if (entryTimeStamp >= startTime)
                    foundBeginning = true;
                else
                    continue;
            }

            if (entryTimeStamp > endTime)
                break;

            EqLogEntry logEntry = new()
            {
                LogLine = logLine,
                Timestamp = entryTimeStamp,
            };

            if (logLine.Contains(Constants.PossibleErrorDelimiter))
            {
                if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
                {
                    logEntry.EntryType = LogEntryType.DkpSpent;
                    logFile.LogEntries.Add(logEntry);
                }
                else if (logLine.Contains(Constants.KillCall, StringComparison.OrdinalIgnoreCase))
                {
                    logEntry.EntryType = LogEntryType.Kill;
                    logFile.LogEntries.Add(logEntry);
                    lookForAttendanceEntries = true;
                }
                else if (logLine.Contains(Constants.Attendance, StringComparison.OrdinalIgnoreCase))
                {
                    logEntry.EntryType = LogEntryType.Attendance;
                    logFile.LogEntries.Add(logEntry);
                    lookForAttendanceEntries = true;
                }

                if (!logLine.Contains(Constants.AttendanceDelimiter))
                {
                    logEntry.ErrorType = PossibleError.TwoColons;
                }

                continue;
            }

            //** Also, what if parsing a log call by a person who didnt do the /who guild?
            if (lookForAttendanceEntries)
            {
                if (logLine.Contains(Constants.WhoZonePrefixPlural) || logLine.Contains(Constants.WhoZonePrefixSingle))
                {
                    logEntry.EntryType = LogEntryType.WhoZoneName;
                    logFile.LogEntries.Add(logEntry);
                    lookForAttendanceEntries = false;
                }
                else if (logLine.Contains(Constants.Dashes) || logLine.Contains(Constants.PlayersOnEverquest))
                {
                    continue;
                }
                else // Assume this is the player character entry  //** -> Bad assumption.  Can have other log spam
                {
                    logEntry.EntryType = LogEntryType.PlayerName;
                    logFile.LogEntries.Add(logEntry);
                }
            }
        }

        return logFile;
    }

    private bool GetTimeStamp(string logLine, out DateTime result)
    {
        if (string.IsNullOrWhiteSpace(logLine))
        {
            result = DateTime.MinValue;
            return false;
        }

        string timeEntry = logLine[1..25];
        return DateTime.TryParseExact(timeEntry, Constants.LogDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}

public interface ILogParser
{
    EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime);
}
