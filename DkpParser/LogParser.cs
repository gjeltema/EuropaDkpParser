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
    private IParseEntry _primaryEntryParser;

    public LogParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        _primaryEntryParser = new PrimaryEntryParser(this, logFile);
        SetParser(new FindStartTimeParser(this, startTime, _primaryEntryParser));

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!GetTimeStamp(logLine, out DateTime entryTimeStamp))
            {
                // Log error?  Empty lines in log file, so expected.
                continue;
            }

            if (entryTimeStamp > endTime)
                break;

            _currentEntryParser.ParseEntry(logLine, entryTimeStamp);
        }

        return logFile;
    }

    public void SetParser(IParseEntry parseEntry)
        => _currentEntryParser = parseEntry;

    private bool GetTimeStamp(string logLine, out DateTime result)
    {
        if (logLine.Length < Constants.TypicalTimestamp.Length || string.IsNullOrWhiteSpace(logLine))
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

