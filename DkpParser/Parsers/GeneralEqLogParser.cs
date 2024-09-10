// -----------------------------------------------------------------------
// GeneralEqLogParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

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

    private static bool TryExtractEqLogTimeStamp(string logLine, out DateTime result)
    {
        // [Wed Feb 21 16:34:07 2024] ...

        if (logLine == null || logLine.Length < Constants.LogDateTimeLength)
        {
            result = DateTime.MinValue;
            return false;
        }

        string timeEntry = logLine[0..Constants.LogDateTimeLength];
        return DateTime.TryParseExact(timeEntry, Constants.LogDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
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
        if (settings.Channel)
        {
            _entryParsers.Add(new ChannelMessageEntryParser());
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
            if (!TryExtractEqLogTimeStamp(logLine, out DateTime entryTimeStamp))
                continue;
            else if (entryTimeStamp < startTime)
                continue;
            else if (entryTimeStamp > endTime)
                break;

            ParseLogEntry(logFile, logLine, entryTimeStamp);
        }

        return logFile;
    }

    private sealed class ConversationEntryParser : IEntryParser
    {
        private readonly ICollection<string> _peopleConversingWith;

        public ConversationEntryParser(ICollection<string> peopleConversingWith)
        {
            _peopleConversingWith = peopleConversingWith;
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
            foreach (string person in _peopleConversingWith)
            {
                if (logLine.Contains($"{Constants.YouTold}{person}, '", StringComparison.OrdinalIgnoreCase)
                    || logLine.Contains($"] {person}{Constants.TellsYou}", StringComparison.OrdinalIgnoreCase))
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

            string logLineWithoutTimestamp = logLine[(Constants.LogDateTimeLength + 1)..];
            if (logLineWithoutTimestamp.Contains("[ANONYMOUS]")
                || (logLineWithoutTimestamp.Contains('(') && logLineWithoutTimestamp.Contains(')') && logLineWithoutTimestamp.Contains('[') && logLineWithoutTimestamp.Contains(']'))
                || logLineWithoutTimestamp.Contains(Constants.Dashes)
                || (logLineWithoutTimestamp.Contains(Constants.WhoZonePrefixPlural) || logLineWithoutTimestamp.Contains(Constants.WhoZonePrefixSingle)) && (logLineWithoutTimestamp.Contains(Constants.PlayersIn) || logLineWithoutTimestamp.Contains(" player in "))
                || logLineWithoutTimestamp.Contains("There are no players in "))
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

public sealed partial class ChannelMessageEntryParser : IEntryParser
{
    private readonly Regex _findTellChannelRegex = FindTellChannelRegex();

    public bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry)
    {
        bool findMatch = _findTellChannelRegex.IsMatch(logLine);

        if (findMatch && (logLine.Contains("You tell ") || logLine.Contains(" tells ")))
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

    [GeneratedRegex(@"[A-Za-z]+:\d+, '", RegexOptions.Compiled)]
    private static partial Regex FindTellChannelRegex();
}

public sealed class GeneralEqLogParserSettings
{
    public bool AllTells { get; set; }

    public bool Auction { get; set; }

    public ICollection<string> CaseInsensitiveSearchTerms { get; set; }

    public ICollection<string> CaseSensitiveSearchTerms { get; set; }

    public bool Channel { get; set; }

    public bool Dies { get; set; }

    public bool FactionStanding { get; set; }

    public bool Guild { get; set; }

    public bool JoinRaid { get; set; }

    public bool Ooc { get; set; }

    public ICollection<string> PeopleConversingWith { get; set; }

    public bool RaidSay { get; set; }

    public bool Say { get; set; }

    public bool Shout { get; set; }

    public bool Who { get; set; }

    public bool You { get; set; }
}

internal interface IEntryParser
{
    bool TryParseEntry(string logLine, DateTime entryTimeStamp, out EqLogEntry eqLogEntry);
}

public interface IGeneralEqLogParser
{
    ICollection<EqLogFile> GetLogFiles(GeneralEqLogParserSettings settings, IEnumerable<string> logFileNames, DateTime startTime, DateTime endTime);
}
