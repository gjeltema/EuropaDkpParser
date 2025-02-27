// -----------------------------------------------------------------------
// PrimaryEntryParser.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Main entry parser for EQ Log files for DKP entry.
/// </summary>
internal sealed class PrimaryEntryParser : IParseEntry
{
    private readonly ChannelAnalyzer _channelAnalyzer;
    private readonly EqLogFile _logFile;
    private readonly ISetEntryParser _setParser;
    private readonly IDkpParserSettings _settings;
    private IParseEntry _populationListingParser;
    private IPopulationListingStartEntryParser _populationListingStartParser;

    internal PrimaryEntryParser(ISetEntryParser setParser, IDkpParserSettings settings, EqLogFile logFile)
    {
        _setParser = setParser;
        _settings = settings;
        _logFile = logFile;

        _channelAnalyzer = new(settings);

        _populationListingParser = new PopulationListingEntryParser(setParser, logFile, this);
        _populationListingStartParser = new PopulationListingStartEntryParser(setParser, this, _populationListingParser);
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        ReadOnlySpan<char> logLineSpan = logLine.AsSpan()[(Constants.LogDateTimeLength + 1)..];

        // Check for just '::' first as it's a fast check.  Do more in depth parsing of the line if this is present.
        if (logLineSpan.IndexOf(Constants.PossibleErrorDelimiter) > 0)
        {
            AddDelimiterEntry(logLine, entryTimeStamp);
        }
        else if (logLineSpan.EndsWith(Constants.EndLootedDashes))
        {
            if (logLine.Contains(Constants.LootedA))
                AddLootedEntry(logLine, entryTimeStamp);
        }
        // Check for just 'raid.' first as it's a fast check. Do more in depth parsing of the line if this is present.
        else if (logLineSpan.EndsWith(Constants.Raid))
        {
            AddRaidJoinLeaveEntry(logLine, entryTimeStamp);
        }
        else if (logLineSpan.IndexOf(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase) > 0)
        {
            AddSpentCall(logLine, entryTimeStamp, false);
        }
    }

    private void AddDelimiterEntry(string logLine, DateTime entryTimeStamp)
    {
        if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
        {
            AddSpentCall(logLine, entryTimeStamp);
        }
        else if (logLine.Contains(Constants.RaidYou) || logLine.Contains(Constants.RaidOther))
        {
            EqLogEntry logEntry = CreateAndAddLogEntry(logLine, entryTimeStamp);

            if (logLine.Contains(Constants.RaidAttendanceTaken, StringComparison.OrdinalIgnoreCase))
            {
                // Only accept raid attendance calls from yourself into /rs.
                if (!logLine.Contains(Constants.RaidYou))
                    return;

                _populationListingStartParser.SetStartTimeStamp(entryTimeStamp);
                _setParser.SetEntryParser(_populationListingStartParser);

                // [Sun Mar 17 23:18:28 2024] You tell your raid, ':::Raid Attendance Taken:::Sister of the Spire:::Kill:::'
                // [Sun Mar 17 22:15:31 2024] You tell your raid, ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'
                logEntry.EntryType = logLine.Contains(Constants.KillCall, StringComparison.OrdinalIgnoreCase)
                    ? LogEntryType.Kill
                    : LogEntryType.Attendance;
            }
            else
            {
                string noWhitespaceLogline = logLine.RemoveAllWhitespace();
                if (noWhitespaceLogline.Contains(Constants.CrashedWithDelimiter, StringComparison.OrdinalIgnoreCase))
                {
                    logEntry.EntryType = LogEntryType.Crashed;
                }
                else if (noWhitespaceLogline.Contains(Constants.AfkStart, StringComparison.OrdinalIgnoreCase))
                {
                    logEntry.EntryType = LogEntryType.AfkStart;
                }
                else if (noWhitespaceLogline.Contains(Constants.AfkEnd, StringComparison.OrdinalIgnoreCase))
                {
                    logEntry.EntryType = LogEntryType.AfkEnd;
                }
                else if (logLine.Contains(Constants.Transfer))
                {
                    logEntry.EntryType = LogEntryType.Transfer;
                }
            }
        }
    }

    private void AddLootedEntry(string logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
        logEntry.EntryType = LogEntryType.CharacterLooted;
        _logFile.LogEntries.Add(logEntry);
    }

    private void AddRaidJoinLeaveEntry(string logLine, DateTime entryTimeStamp)
    {
        // [Tue Feb 27 23:13:23 2024] Orsino has left the raid.
        // [Tue Feb 27 23:14:20 2024] Marco joined the raid.
        // [Sun Feb 25 22:52:46 2024] You have joined the group.
        // [Thu Feb 22 23:13:52 2024] Luciania joined the raid.
        // [Thu Feb 22 23:13:52 2024] You have joined the raid.

        if (logLine.EndsWith(Constants.JoinedRaid))
        {
            EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
            _logFile.LogEntries.Add(logEntry);
            logEntry.EntryType = LogEntryType.JoinedRaid;
        }
        else if (logLine.EndsWith(Constants.LeftRaid))
        {
            EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
            _logFile.LogEntries.Add(logEntry);
            logEntry.EntryType = LogEntryType.LeftRaid;
        }
    }

    private void AddSpentCall(string logLine, DateTime entryTimeStamp, bool confirmed = true)
    {
        EqChannel channel = _channelAnalyzer.GetValidDkpChannel(logLine);
        if (channel == EqChannel.None)
            return;

        EqLogEntry logEntry = CreateAndAddLogEntry(logLine, entryTimeStamp);
        logEntry.EntryType = confirmed ? LogEntryType.DkpSpent : LogEntryType.PossibleDkpSpent;
        logEntry.Channel = channel;
    }

    private EqLogEntry CreateAndAddLogEntry(string logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
        _logFile.LogEntries.Add(logEntry);
        return logEntry;
    }

    private EqLogEntry CreateLogEntry(string logLine, DateTime entryTimeStamp)
        => new() { LogLine = logLine, Timestamp = entryTimeStamp };
}
