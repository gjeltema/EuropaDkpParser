// -----------------------------------------------------------------------
// GeneralEqLogParser.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

using System.IO;

public sealed partial class GeneralEqLogParser : IGeneralEqLogParser
{
    private const string DiesTerm = " dies.";
    private const string FactionStandingTerm = "Your faction standing with ";
    private const string OocTerm = " out of character, ";
    private const string OtherAuctionTerm = " auctions, ";
    private const string OtherGuildTerm = " tells the guild, ";
    private const string OtherRaidTerm = " tells the raid, ";
    private const string OtherSayTerm = " says, ";
    private const string OtherShoutTerm = " shouts, ";
    private const string YouAuctionTerm = "You auction, ";
    private const string YouGuildTerm = "You say to your guild, ";
    private const string YouRaidTerm = "You tell your raid, ";
    private const string YouSayTerm = "You say, ";
    private const string YouShoutTerm = "You shout, ";
    private const string YouTerm = "You";
    private readonly List<IEntryParser> _entryParsers = [];

    public ICollection<EqLogFile> GetLogFiles(GeneralEqLogParserSettings settings, IEnumerable<string> logFileNames, DateTime startTime, DateTime endTime)
    {
        InitializeEntryParsers(settings);

        List<EqLogFile> logFiles = [];

        foreach (string fileName in logFileNames)
        {
            EqLogFile parsedFile = ParseLogFile(fileName, startTime, endTime);
            if (parsedFile.LogEntries.Count > 0)
                logFiles.Add(parsedFile);
        }

        return logFiles;
    }

    private void InitializeEntryParsers(GeneralEqLogParserSettings settings)
    {
        _entryParsers.Clear();

        if (settings.Guild)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(YouGuildTerm));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(OtherGuildTerm));
        }
        if (settings.RaidSay)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(YouRaidTerm));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(OtherRaidTerm));
        }
        if (settings.Say)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(YouSayTerm));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(OtherSayTerm));
        }
        if (settings.Ooc)
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(OocTerm));
        if (settings.Group)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(Constants.GroupYou));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(Constants.GroupOther));
        }
        if (settings.Shout)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(YouShoutTerm));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(OtherShoutTerm));
        }
        if (settings.You)
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(YouTerm));
        if (settings.Auction)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(YouAuctionTerm));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(OtherAuctionTerm));
        }
        if (settings.Dies)
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(DiesTerm));
        if (settings.FactionStanding)
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(FactionStandingTerm));
        if (settings.AllTells)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(Constants.YouTold));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(Constants.TellsYou));
        }
        if (settings.JoinRaid)
        {
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(Constants.JoinedRaid));
            _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(Constants.LeftRaid));
        }
        if (settings.PeopleConversingWith != null && settings.PeopleConversingWith.Count > 0 && !settings.AllTells)
        {
            _entryParsers.Add(new ConversationEntryParser(settings.PeopleConversingWith));
        }
        if (settings.CaseInsensitiveSearchTerms != null && settings.CaseInsensitiveSearchTerms.Count > 0)
        {
            foreach (string searchTerm in settings.CaseInsensitiveSearchTerms)
                _entryParsers.Add(new SearchTermCaseInsensitiveEntryParser(searchTerm));
        }
        if (settings.CaseSensitiveSearchTerms != null && settings.CaseSensitiveSearchTerms.Count > 0)
        {
            foreach (string searchTerm in settings.CaseInsensitiveSearchTerms)
                _entryParsers.Add(new SearchTermCaseSensitiveEntryParser(searchTerm));
        }
        if (settings.Channels != null && settings.Channels.Count > 0)
        {
            foreach (string channel in settings.Channels)
                _entryParsers.Add(new CustomChannelEntryParser(channel));
        }
        if (settings.Who)
        {
            WhoBodyEntryParser bodyParser = new();
            WhoStartEntryParser startParser = new(bodyParser);
            _entryParsers.Add(startParser);
            _entryParsers.Add(bodyParser);
        }
    }

    private void ParseLogEntry(EqLogFile logFile, string logLine, DateTime entryTimeStamp)
    {
        for (int i = 0; i < _entryParsers.Count; i++)
        {
            IEntryParser entryParser = _entryParsers[i];
            if (entryParser.TryParseEntry(logLine, entryTimeStamp, out EqLogEntry entry))
            {
                logFile.LogEntries.Add(entry);
                return;
            }
        }
    }

    private EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!logLine.TryExtractEqLogTimeStamp(out DateTime entryTimeStamp))
                continue;
            else if (entryTimeStamp < startTime)
                continue;
            else if (entryTimeStamp > endTime)
                break;

            string logLineNoTimestamp = logLine[(Constants.EqLogDateTimeLength + 1)..];
            ParseLogEntry(logFile, logLineNoTimestamp, entryTimeStamp);
        }

        return logFile;
    }

    private sealed class ConversationEntryParser : IEntryParser
    {
        private readonly ICollection<string> _peopleConversingWith;

        public ConversationEntryParser(ICollection<string> peopleConversingWith)
        {
            _peopleConversingWith = peopleConversingWith.Select(x => x.NormalizeName()).ToList();
        }

        public bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry)
        {
            if (!logLine.Contains(Constants.YouTold) && !logLine.Contains(Constants.TellsYou))
            {
                eqLogEntry = null;
                return false;
            }

            // [Fri Mar 01 21:49:34 2024] Klawse tells you, 'need key'
            // [Fri Mar 01 21:55:29 2024] You told Klawse, 'I cant do anything with the raid window.'
            // [Thu Oct 24 00:02:39 2024] You told Shaper '[queued], You're in WC.  If you come to EC, I can tag you.'
            foreach (string person in _peopleConversingWith)
            {
                if (logLine.StartsWith($"{Constants.YouTold}{person}")
                    || logLine.Contains($"{person}{Constants.TellsYou}"))
                {
                    eqLogEntry = new()
                    {
                        EntryType = LogEntryType.Unknown,
                        LogLine = logLine,
                        Timestamp = entryTimeStamp
                    };

                    return true;
                }
            }

            eqLogEntry = null;
            return false;
        }
    }

    private sealed class SearchTermCaseInsensitiveEntryParser : IEntryParser
    {
        private readonly string _searchTerm;

        public SearchTermCaseInsensitiveEntryParser(string searchTerm)
        {
            _searchTerm = searchTerm;
        }

        public bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry)
        {
            if (logLine.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                eqLogEntry = new()
                {
                    EntryType = LogEntryType.Unknown,
                    LogLine = logLine,
                    Timestamp = entryTimeStamp
                };

                return true;
            }
            else
            {
                eqLogEntry = null;
                return false;
            }
        }
    }

    private sealed class SearchTermCaseSensitiveEntryParser : IEntryParser
    {
        private readonly string _searchTerm;

        public SearchTermCaseSensitiveEntryParser(string searchTerm)
        {
            _searchTerm = searchTerm;
        }

        public bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry)
        {
            if (logLine.Contains(_searchTerm))
            {
                eqLogEntry = new()
                {
                    EntryType = LogEntryType.Unknown,
                    LogLine = logLine,
                    Timestamp = entryTimeStamp
                };

                return true;
            }
            else
            {
                eqLogEntry = null;
                return false;
            }
        }
    }

    private sealed class WhoBodyEntryParser : IEntryParser
    {
        public DateTime LimitTimestamp { get; set; } = DateTime.MinValue;

        public bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry)
        {
            if (entryTimeStamp > LimitTimestamp)
            {
                eqLogEntry = null;
                return false;
            }

            if (logLine.Contains("[ANONYMOUS]")
                || (logLine.Contains('(') && logLine.Contains(')') && logLine.Contains('[') && logLine.Contains(']'))
                || logLine.Contains(Constants.Dashes)
                || (logLine.Contains(Constants.WhoZonePrefixPlural) || logLine.Contains(Constants.WhoZonePrefixSingle)) && (logLine.Contains(Constants.PlayersIn) || logLine.Contains(Constants.PlayerIn))
                || logLine.Contains("There are no players in "))
            {
                eqLogEntry = new()
                {
                    EntryType = LogEntryType.Unknown,
                    LogLine = logLine,
                    Timestamp = entryTimeStamp
                };

                return true;
            }
            else
            {
                eqLogEntry = null;
                return false;
            }
        }
    }

    private sealed class WhoStartEntryParser : IEntryParser
    {
        private readonly WhoBodyEntryParser _bodyParser;

        public WhoStartEntryParser(WhoBodyEntryParser bodyParser)
        {
            _bodyParser = bodyParser;
        }

        public bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry)
        {
            if (logLine.Contains(Constants.PlayersOnEverquest))
            {
                eqLogEntry = new()
                {
                    EntryType = LogEntryType.Unknown,
                    LogLine = logLine,
                    Timestamp = entryTimeStamp
                };

                _bodyParser.LimitTimestamp = entryTimeStamp.AddSeconds(2);

                return true;
            }
            else
            {
                eqLogEntry = null;
                return false;
            }
        }
    }
}

public sealed class CustomChannelEntryParser : IEntryParser
{
    private readonly string _otherTellsChannelName;
    private readonly string _youTellChannelName;

    // [Tue Jan 14 20:27:57 2025] Rezzt tells Eu.heals:3, 'Celestial Elixir on -- Mcporty -- at 100% mana'
    // [Tue Jan 14 21:25:13 2025] You tell Eu.officers:1, 'So... what do we infer from that?'
    public CustomChannelEntryParser(string channelName)
    {
        string normalizedChannelName = channelName.NormalizeName();
        _youTellChannelName = $"You tell {normalizedChannelName}:";
        _otherTellsChannelName = $" tells {normalizedChannelName}:";
    }

    public bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry)
    {
        if (logLine.StartsWith(_youTellChannelName) || logLine.Contains(_otherTellsChannelName))
        {
            eqLogEntry = new()
            {
                EntryType = LogEntryType.Unknown,
                LogLine = logLine,
                Timestamp = entryTimeStamp
            };

            return true;
        }
        else
        {
            eqLogEntry = null;
            return false;
        }
    }
}

public sealed class GeneralEqLogParserSettings
{
    public bool AllTells { get; set; }

    public bool Auction { get; set; }

    public ICollection<string> CaseInsensitiveSearchTerms { get; set; }

    public ICollection<string> CaseSensitiveSearchTerms { get; set; }

    public ICollection<string> Channels { get; set; }

    public bool Dies { get; set; }

    public bool FactionStanding { get; set; }

    public bool Group { get; set; }

    public bool Guild { get; set; }

    public bool JoinRaid { get; set; }

    public bool LeaveRaid { get; set; }

    public bool Looted { get; set; }

    public bool Ooc { get; set; }

    public bool OthersHealed { get; set; }

    public ICollection<string> PeopleConversingWith { get; set; }

    public bool RaidSay { get; set; }

    public bool Rampage { get; set; }

    public bool Say { get; set; }

    public bool Shout { get; set; }

    public bool Who { get; set; }

    public bool You { get; set; }

    public bool YourHeals { get; set; }
}

internal interface IEntryParser
{
    bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry);
}

public interface IGeneralEqLogParser
{
    ICollection<EqLogFile> GetLogFiles(GeneralEqLogParserSettings settings, IEnumerable<string> logFileNames, DateTime startTime, DateTime endTime);
}
