// -----------------------------------------------------------------------
// RaidSummaryParser.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

public sealed class RaidSummaryParser : EqLogParserBase, IRaidSummaryParser
{
    private readonly bool _includeTells;
    private readonly IDkpParserSettings _settings;

    public RaidSummaryParser(IDkpParserSettings settings, bool includeTells)
    {
        _settings = settings;
        _includeTells = includeTells;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
        => GetEqLogFiles(startTime, endTime, _settings.SelectedLogFiles);

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        RaidSummaryEntryParser raidSummaryEntryParser = new(logFile, _includeTells);
        FindStartTimeEntryParser findStartParser = new(this, startTime, raidSummaryEntryParser);

        SetEntryParser(findStartParser);
    }

    private sealed class RaidSummaryEntryParser : IParseEntry
    {
        private readonly bool _includeTells;
        private readonly EqLogFile _logFile;

        public RaidSummaryEntryParser(EqLogFile logFile, bool includeTells)
        {
            _logFile = logFile;
            _includeTells = includeTells;
        }

        public void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
        {
            if (logLine.Contains(Constants.RaidYou) || logLine.Contains(Constants.RaidOther)
            || logLine.Contains(Constants.AuctionYou) || logLine.Contains(Constants.AuctionOther)
            || logLine.Contains(Constants.OocYou) || logLine.Contains(Constants.OocOther)
            || logLine.Contains(Constants.GuildYou) || logLine.Contains(Constants.GuildOther)
            || logLine.Contains(Constants.GroupYou) || logLine.Contains(Constants.GroupOther)
            || logLine.Contains(Constants.JoinedRaid) || logLine.Contains(Constants.LeftRaid)
            || logLine.Contains(Constants.AuctionYou) || logLine.Contains(Constants.AuctionOther)
            || logLine.Contains(Constants.LootedA)
            || logLine.Contains("You have been healed for ") || logLine.Contains(" feels much better.")
            || logLine.Contains("goes on a RAMPAGE against ")
            || logLine.Contains(" Eu.heals:") || logLine.Contains(" Eu.ch:") || logLine.Contains(" Eu.officers:"))
            {
                AddLogEntry(logLine, entryTimeStamp);
                return;
            }

            if (_includeTells && (logLine.Contains(Constants.TellsYou) || logLine.Contains(Constants.YouTold)))
            {
                AddLogEntry(logLine, entryTimeStamp);
                return;
            }
        }

        private void AddLogEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
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

public interface IRaidSummaryParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
