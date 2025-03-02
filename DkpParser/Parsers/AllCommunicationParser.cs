// -----------------------------------------------------------------------
// AllCommunicationParser.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

using System;

public sealed class AllCommunicationParser : EqLogParserBase, IAllCommunicationParser
{
    private readonly IDkpParserSettings _settings;

    public AllCommunicationParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
        => GetEqLogFiles(startTime, endTime, _settings.SelectedLogFiles);

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        CommunicationEntryParser communicationEntryParser = new(logFile);
        FindStartTimeEntryParser findStartParser = new(this, startTime, communicationEntryParser);

        SetEntryParser(findStartParser);
    }

    private sealed class CommunicationEntryParser : IParseEntry
    {
        private readonly EqLogFile _logFile;

        public CommunicationEntryParser(EqLogFile logFile)
        {
            _logFile = logFile;
        }

        public void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
        {
            if (!logLine.Contains("You ") && !logLine.Contains(" say") && !logLine.Contains(" tell") && !logLine.Contains(" shout"))
                return;

            // commented out redundant checks, but leaving them in to show they are intended to be found.
            if (logLine.Contains("You tell ") // [Mon Mar 18 23:25:23 2024] You tell Eu.officers:1, '...
                || logLine.Contains(" tells ") // [Mon Mar 18 23:25:59 2024] Overture tells Eu.officers:1, 'awesome'
                || logLine.Contains(Constants.YouTold)
                //|| logLine.Contains(Constants.TellsYou)
                || logLine.Contains(" says, ")
                || logLine.Contains("You say, ")
                //|| logLine.Contains(" tells the guild, ")
                || logLine.Contains("You say to your guild, ")
                //|| logLine.Contains("You tell your raid, ")
                //|| logLine.Contains(" tells the raid, ")
                || logLine.Contains(" says out of character, ")
                || logLine.Contains("You say out of character, ") // Need to verify how you say something to /ooc
                || logLine.Contains(" shouts, ")
                || logLine.Contains("You shout, "))
            {
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
}

public interface IAllCommunicationParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
