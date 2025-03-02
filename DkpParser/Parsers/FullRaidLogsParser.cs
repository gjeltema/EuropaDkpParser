// -----------------------------------------------------------------------
// FullRaidLogsParser.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

public sealed class FullRaidLogsParser : EqLogParserBase, IFullRaidLogsParser
{
    private readonly IDkpParserSettings _settings;

    public FullRaidLogsParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
        => GetEqLogFiles(startTime, endTime, _settings.SelectedLogFiles);

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        IParseEntry logEverything = _settings.IncludeTellsInRawLog ? new LogEverythingEntryParser(logFile) : new LogEverythingExceptTellsEntryParser(logFile);
        FindStartTimeEntryParser findStartParser = new(this, startTime, logEverything);

        SetEntryParser(findStartParser);
    }

    private sealed class LogEverythingExceptTellsEntryParser : IParseEntry
    {
        private readonly EqLogFile _logFile;

        public LogEverythingExceptTellsEntryParser(EqLogFile logFile)
        {
            _logFile = logFile;
        }

        public void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
        {
            if (logLine.Contains(Constants.YouTold) || logLine.Contains(Constants.TellsYou))
                return;

            EqLogEntry logEntry = new()
            {
                EntryType = LogEntryType.Unknown,
                LogLine = logLine.ToString(),
                Timestamp = entryTimeStamp
            };

            _logFile.LogEntries.Add(logEntry);
        }
    }
}

public interface IFullRaidLogsParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
