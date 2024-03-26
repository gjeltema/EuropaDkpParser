// -----------------------------------------------------------------------
// ConversationParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System;
using System.Globalization;
using System.IO;

public sealed class ConversationParser : IConversationParser, ISetParser
{
    private readonly string _personConversingWith;
    private readonly IDkpParserSettings _settings;
    private IParseEntry _currentEntryParser;

    public ConversationParser(IDkpParserSettings settings, string personConversingWith)
    {
        _settings = settings;
        _personConversingWith = personConversingWith;
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

        ConversationEntryParser conversationEntryParser = new(logFile, _personConversingWith);
        FindStartTimeParser findStartParser = new(this, startTime, conversationEntryParser);

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

    private sealed class ConversationEntryParser : IParseEntry
    {
        private readonly EqLogFile _logFile;
        private readonly string _personConversingWith;

        public ConversationEntryParser(EqLogFile logFile, string personConversingWith)
        {
            _logFile = logFile;
            _personConversingWith = personConversingWith;
        }

        public void ParseEntry(string logLine, DateTime entryTimeStamp)
        {
            if (!logLine.Contains(_personConversingWith, StringComparison.OrdinalIgnoreCase))
                return;

            if (logLine.Contains($"{Constants.YouTold} {_personConversingWith}, '", StringComparison.OrdinalIgnoreCase)
                || logLine.Contains($"{_personConversingWith} {Constants.TellsYou}, '", StringComparison.OrdinalIgnoreCase))
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
}

public interface IConversationParser : ILogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
