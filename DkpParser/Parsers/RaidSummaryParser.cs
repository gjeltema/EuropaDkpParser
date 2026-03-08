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
            if (logLine.StartsWith(Constants.RaidYou) || logLine.Contains(Constants.RaidOther)
            || logLine.StartsWith(Constants.AuctionYou) || logLine.Contains(Constants.AuctionOther)
            || logLine.StartsWith(Constants.OocYou) || logLine.Contains(Constants.OocOther)
            || logLine.StartsWith(Constants.GuildYou) || logLine.Contains(Constants.GuildOther)
            || logLine.StartsWith(Constants.GroupYou) || logLine.Contains(Constants.GroupOther)
            || logLine.Contains(Constants.JoinedRaid) || logLine.Contains(Constants.LeftRaid)
            || logLine.StartsWith(Constants.AuctionYou) || logLine.Contains(Constants.AuctionOther)
            || logLine.EndsWith(Constants.EndLootedDashes) || logLine.StartsWith(Constants.SlainYou)
            || logLine.StartsWith(Constants.YouHealed) || logLine.Contains(Constants.FeelsMuchBetter)
            || logLine.Contains(Constants.Rampage) || logLine.Contains(Constants.Slain)
            || logLine.Contains(" Eu.heals:") || logLine.Contains(" Eu.ch:") || logLine.Contains(" Eu.officers:"))
            {
                AddLogEntry(logLine, entryTimeStamp);
                return;
            }

            if (_includeTells && (logLine.Contains(Constants.TellsYou) || logLine.StartsWith(Constants.YouTold)))
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
