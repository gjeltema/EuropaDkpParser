// -----------------------------------------------------------------------
// ConversationParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Parses out all the /tells between the user and a specified other person.
/// </summary>
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

            // [Fri Mar 01 21:49:34 2024] Klawse tells you, 'need key'
            // [Fri Mar 01 21:55:29 2024] You told Klawse, 'I cant do anything with the raid window.'
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

/// <summary>
/// Parses out all the /tells between the user and a specified other person.
/// </summary>
public interface IConversationParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
