// -----------------------------------------------------------------------
// FullEqLogParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class FullEqLogParser : EqLogParserBase, IFullEqLogParser
{
    private readonly IDkpParserSettings _settings;

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

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        LogEverythingParser logEverything = new(logFile);
        FindStartTimeParser findStartParser = new(this, startTime, logEverything);

        SetEntryParser(findStartParser);
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

public interface IFullEqLogParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
