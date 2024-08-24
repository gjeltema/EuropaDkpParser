// -----------------------------------------------------------------------
// ConversationParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Parses out all the /tells between the user and a specified other person(s).
/// </summary>
public sealed class ConversationParser : EqLogParserBase, IConversationParser
{
    private readonly string[] _peopleConversingWith;
    private readonly IDkpParserSettings _settings;

    public ConversationParser(IDkpParserSettings settings, string peopleConversingWith)
    {
        _settings = settings;
        _peopleConversingWith = peopleConversingWith.Split(';');
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
        => GetEqLogFiles(startTime, endTime, _settings.SelectedLogFiles);

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        ConversationEntryParser conversationEntryParser = new(logFile, _peopleConversingWith);
        FindStartTimeEntryParser findStartParser = new(this, startTime, conversationEntryParser);

        SetEntryParser(findStartParser);
    }

    private sealed class ConversationEntryParser : IParseEntry
    {
        private readonly EqLogFile _logFile;
        private readonly ICollection<string> _peopleConversingWith;

        public ConversationEntryParser(EqLogFile logFile, ICollection<string> peopleConversingWith)
        {
            _logFile = logFile;
            _peopleConversingWith = peopleConversingWith;
        }

        public void ParseEntry(string logLine, DateTime entryTimeStamp)
        {
            if (!logLine.Contains(Constants.YouTold) && !logLine.Contains(Constants.TellsYou))
                return;

            // [Fri Mar 01 21:49:34 2024] Klawse tells you, 'need key'
            // [Fri Mar 01 21:55:29 2024] You told Klawse, 'I cant do anything with the raid window.'
            foreach (string person in _peopleConversingWith)
            {
                if (logLine.Contains($"{Constants.YouTold} {person}, '", StringComparison.OrdinalIgnoreCase)
                    || logLine.Contains($"] {person} {Constants.TellsYou}", StringComparison.OrdinalIgnoreCase))
                {
                    EqLogEntry logEntry = new()
                    {
                        EntryType = LogEntryType.Unknown,
                        LogLine = logLine,
                        Timestamp = entryTimeStamp
                    };

                    _logFile.LogEntries.Add(logEntry);

                    return;
                }
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
