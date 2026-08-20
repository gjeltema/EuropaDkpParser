// -----------------------------------------------------------------------
// CharacterDeathParser.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

public sealed class CharacterDeathParser : EqLogParserBase, ICharacterDeathParser
{
    private readonly string _bossName;
    private readonly string _characterName;
    private readonly IDkpParserSettings _settings;

    public CharacterDeathParser(IDkpParserSettings settings, string characterName, string bossName)
    {
        _settings = settings;
        _characterName = characterName.NormalizeName();
        _bossName = bossName;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
    {
        string characterSlainMessage = $"{_characterName}{Constants.Slain}";

        List<EqLogFile> logFiles = [];
        foreach (string logFileName in _settings.SelectedLogFiles)
        {
            EqLogFile parsedFile = ParseLogFile(logFileName, startTime, endTime);
            EqLogFile processedFile = ProcessFile(parsedFile, characterSlainMessage);
            if (processedFile.LogEntries.Count > 0)
                logFiles.Add(processedFile);
        }

        return logFiles;
    }

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        CharacterDeathOtherEntryParser characterDeathEntryParser = new(logFile, _characterName, _bossName);
        FindStartTimeEntryParser findStartParser = new(this, startTime, characterDeathEntryParser);

        SetEntryParser(findStartParser);
    }

    private EqLogFile ProcessFile(EqLogFile parsedFile, string characterSlainMessage)
    {
        EqLogFile processedFile = new();

        List<EqLogEntry> slainEntries = parsedFile.LogEntries.Where(x => x.LogLine.StartsWith(characterSlainMessage)).ToList();
        foreach (EqLogEntry slainEntry in slainEntries)
        {
            DateTime endTimestamp = slainEntry.Timestamp;
            DateTime startTimestamp = endTimestamp.AddSeconds(-20);
            foreach (EqLogEntry entry in parsedFile.LogEntries)
            {
                if (startTimestamp <= entry.Timestamp && entry.Timestamp <= endTimestamp)
                    processedFile.LogEntries.Add(entry);
            }
        }

        return processedFile;
    }

    private sealed class CharacterDeathOtherEntryParser : IParseEntry
    {
        private readonly string _bossName;
        private readonly string _characterName;
        private readonly EqLogFile _logFile;

        public CharacterDeathOtherEntryParser(EqLogFile logFile, string characterName, string bossName)
        {
            _logFile = logFile;
            _characterName = characterName;
            _bossName = bossName;
        }

        public void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
        {
            if (logLine.Contains(_characterName) || logLine.Contains(_bossName)
            || logLine.StartsWith(Constants.RaidYou) || logLine.Contains(Constants.RaidOther)
            || logLine.StartsWith(Constants.AuctionYou) || logLine.Contains(Constants.AuctionOther)
            || logLine.StartsWith(Constants.OocYou) || logLine.Contains(Constants.OocOther)
            || logLine.StartsWith(Constants.GuildYou) || logLine.Contains(Constants.GuildOther)
            || logLine.StartsWith(Constants.GroupYou) || logLine.Contains(Constants.GroupOther)
            || logLine.StartsWith(Constants.SayYou) || logLine.Contains(Constants.SayOther)
            || logLine.StartsWith(Constants.AuctionYou) || logLine.Contains(Constants.AuctionOther)
            || logLine.StartsWith(Constants.Twitches) || logLine.Contains(Constants.Rampage) || logLine.EndsWith(Constants.BeginsCastSpell) || logLine.Contains(Constants.Slain)
            || logLine.Contains(" Eu.heals:") || logLine.Contains(" Eu.ch:") || logLine.Contains(" Eu.officers:"))
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

public interface ICharacterDeathParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
