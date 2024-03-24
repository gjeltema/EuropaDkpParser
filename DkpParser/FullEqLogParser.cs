// -----------------------------------------------------------------------
// FullEqLogParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Globalization;
using System.IO;

public sealed class FullEqLogParser : IFullEqLogParser, ISetParser
{
    private readonly IDkpParserSettings _settings;
    private IParseEntry _currentEntryParser;

    public FullEqLogParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
    {
        List<EqLogFile> logFiles = [];
        foreach (string logFileName in _settings.SelectedLogFiles)
        {
            EqLogFile parsedFile = ParseLogFile(logFileName, startTime, endTime);
            if (parsedFile.LogEntries.Count > 0)
                logFiles.Add(parsedFile);
        }

        return logFiles;
    }

    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        LogEverythingParser logEverything = new(logFile);
        FindStartTimeParser findStartParser = new(this, startTime, logEverything);

        SetParser(findStartParser);

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!GetTimeStamp(logLine, out DateTime entryTimeStamp))
            {
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

    private sealed class LogEverythingParser : IParseEntry
    {
        private readonly EqLogFile _logFile;

        public LogEverythingParser(EqLogFile logFile)
        {
            _logFile = logFile;
        }

        public void ParseEntry(string logLine, DateTime entryTimeStamp)
        {
            EqLogEntry logEntry = new()
            {
                EntryType = LogEntryType.Unknown,
                LogLine = logLine,
                Timestamp = entryTimeStamp
            };

            _logFile.LogEntries.Add(logEntry);
        }
    }
}

public interface IFullEqLogParser : ILogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
