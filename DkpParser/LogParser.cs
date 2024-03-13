// -----------------------------------------------------------------------
// LogParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Globalization;
using System.IO;

internal sealed class LogParser : ILogParser, ISetParser
{
    private readonly IDkpParserSettings _settings;
    private IParseEntry _currentEntryParser;
    private IParseEntry _attendanceEntryParser;

    public LogParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        _attendanceEntryParser = null; //** Need to create and set this
        SetParser(new FindStartTimeParser(this, startTime, _attendanceEntryParser));

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!GetTimeStamp(logLine, out DateTime entryTimeStamp))
            {
                // Log error?
                continue;
            }

            if (entryTimeStamp > endTime)
                break;

            EqLogEntry logEntry = _currentEntryParser.ParseEntry(logLine, entryTimeStamp);
            if(logEntry != null)
                logFile.LogEntries.Add(logEntry);

            //if (logLine.Contains(Constants.PossibleErrorDelimiter))
            //{
            //    if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
            //    {
            //        logEntry.EntryType = LogEntryType.DkpSpent;
            //        logFile.LogEntries.Add(logEntry);
            //    }
            //    else if (logLine.Contains(Constants.KillCall, StringComparison.OrdinalIgnoreCase))
            //    {
            //        logEntry.EntryType = LogEntryType.Kill;
            //        logFile.LogEntries.Add(logEntry);
            //        lookForAttendanceEntries = true;
            //    }
            //    else if (logLine.Contains(Constants.Attendance, StringComparison.OrdinalIgnoreCase))
            //    {
            //        logEntry.EntryType = LogEntryType.Attendance;
            //        logFile.LogEntries.Add(logEntry);
            //        lookForAttendanceEntries = true;
            //    }

            //    if (!logLine.Contains(Constants.AttendanceDelimiter))
            //    {
            //        logEntry.ErrorType = PossibleError.TwoColons;
            //    }

            //    continue;
            //}

            ////** Also, what if parsing a log call by a person who didnt do the /who guild?
            //if (lookForAttendanceEntries)
            //{
            //    if (logLine.Contains(Constants.WhoZonePrefixPlural) || logLine.Contains(Constants.WhoZonePrefixSingle))
            //    {
            //        logEntry.EntryType = LogEntryType.WhoZoneName;
            //        logFile.LogEntries.Add(logEntry);
            //        lookForAttendanceEntries = false;
            //    }
            //    else if (logLine.Contains(Constants.Dashes) || logLine.Contains(Constants.PlayersOnEverquest))
            //    {
            //        continue;
            //    }
            //    else // Assume this is the player character entry  //** -> Bad assumption.  Can have other log spam
            //    {
            //        logEntry.EntryType = LogEntryType.PlayerName;
            //        logFile.LogEntries.Add(logEntry);
            //    }
            //}
        }

        return logFile;
    }

    public void SetParser(IParseEntry parseEntry) 
        => _currentEntryParser = parseEntry;

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

internal interface ISetParser
{
    void SetParser(IParseEntry parseEntry);
}

