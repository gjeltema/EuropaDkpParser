// -----------------------------------------------------------------------
// ConversationParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class ConversationParser : EqLogParserBase, IConversationParser
{
    private readonly string _personConversingWith;
    private readonly IDkpParserSettings _settings;

    public ConversationParser(IDkpParserSettings settings, string personConversingWith)
    {
        _settings = settings;
        _personConversingWith = personConversingWith;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
        => GetEqLogFiles(startTime, endTime, _settings.SelectedLogFiles);

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        ConversationEntryParser conversationEntryParser = new(logFile, _personConversingWith);
        FindStartTimeEntryParser findStartParser = new(this, startTime, conversationEntryParser);

        SetEntryParser(findStartParser);
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

public interface IConversationParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
