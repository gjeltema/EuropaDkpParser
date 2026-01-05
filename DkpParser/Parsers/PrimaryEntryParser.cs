// -----------------------------------------------------------------------
// PrimaryEntryParser.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Main entry parser for EQ Log files for DKP entry.
/// </summary>
internal sealed class PrimaryEntryParser : IParseEntry
{
    private readonly ChannelAnalyzer _channelAnalyzer;
    private readonly EqLogFile _logFile;
    private readonly IParseEntry _populationListingParser;
    private readonly IPopulationListingStartEntryParser _populationListingStartParser;
    private readonly DelimiterStringSanitizer _sanitizer = new();
    private readonly ISetEntryParser _setParser;
    private readonly char[] _tempString = new char[400];

    internal PrimaryEntryParser(ISetEntryParser setParser, IDkpParserSettings settings, EqLogFile logFile)
    {
        _setParser = setParser;
        _logFile = logFile;

        _channelAnalyzer = new(settings);

        _populationListingParser = new PopulationListingEntryParser(setParser, logFile, this);
        _populationListingStartParser = new PopulationListingStartEntryParser(setParser, this, _populationListingParser);
    }

    public void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
    {
        // Check for just '::' first as it's a fast check.  Do more in depth parsing of the line if this is present.
        if (logLine.Contains(Constants.PossibleErrorDelimiter) || logLine.Contains(Constants.AlternateDelimiter))
        {
            AddDelimiterEntry(logLine, entryTimeStamp);
        }
        else if (logLine.EndsWith(Constants.EndLootedDashes))
        {
            if (logLine.Contains(Constants.LootedA))
                AddLootedEntry(logLine, entryTimeStamp);
        }
        // Check for just 'raid.' first as it's a fast check. Do more in depth parsing of the line if this is present.
        else if (logLine.EndsWith(Constants.Raid))
        {
            AddRaidJoinLeaveEntry(logLine, entryTimeStamp);
        }
        else if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
        {
            AddSpentCall(logLine, entryTimeStamp, LogEntryType.PossibleDkpSpent);
        }
    }

    private void AddDelimiterEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
    {
        if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
        {
            AddSpentCall(logLine, entryTimeStamp, LogEntryType.DkpSpent);
        }
        else if (logLine.Contains(Constants.RaidYou) || logLine.Contains(Constants.RaidOther))
        {
            if (logLine.Contains(Constants.RaidAttendanceTaken, StringComparison.OrdinalIgnoreCase))
            {
                // Only accept raid attendance calls from yourself into /rs.
                if (!logLine.Contains(Constants.RaidYou))
                    return;

                _populationListingStartParser.SetStartTimeStamp(entryTimeStamp);
                _setParser.SetEntryParser(_populationListingStartParser);

                // [Sun Mar 17 23:18:28 2024] You tell your raid, ':::Raid Attendance Taken:::Sister of the Spire:::Kill:::'
                // [Sun Mar 17 22:15:31 2024] You tell your raid, ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'
                LogEntryType entryType = logLine.Contains(Constants.KillCall, StringComparison.OrdinalIgnoreCase)
                    ? LogEntryType.Kill
                    : LogEntryType.Attendance;
                CreateAndAddLogEntry(logLine, entryTimeStamp, entryType);
            }
            else
            {
                HandleUserCommands(logLine, entryTimeStamp);
            }
        }
        else if (logLine.Contains(Constants.GuildYou) || logLine.Contains(Constants.GuildOther))
        {
            HandleUserCommands(logLine, entryTimeStamp);
        }
    }

    private void AddLootedEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.CharacterLooted);
    }

    private void AddRaidJoinLeaveEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
    {
        // [Tue Feb 27 23:13:23 2024] Orsino has left the raid.
        // [Tue Feb 27 23:14:20 2024] Marco joined the raid.
        // [Sun Feb 25 22:52:46 2024] You have joined the group.
        // [Thu Feb 22 23:13:52 2024] Luciania joined the raid.
        // [Thu Feb 22 23:13:52 2024] You have joined the raid.

        if (logLine.EndsWith(Constants.JoinedRaid))
        {
            CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.JoinedRaid);
        }
        else if (logLine.EndsWith(Constants.LeftRaid))
        {
            CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.LeftRaid);
        }
    }

    private void AddSpentCall(ReadOnlySpan<char> logLine, DateTime entryTimeStamp, LogEntryType entryType)
    {
        EqChannel channel = _channelAnalyzer.GetValidDkpChannel(logLine);
        if (channel == EqChannel.None)
            return;

        EqLogEntry logEntry = CreateAndAddLogEntry(logLine, entryTimeStamp, entryType);
        logEntry.Channel = channel;
    }

    private EqLogEntry CreateAndAddLogEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp, LogEntryType entryType)
    {
        EqLogEntry logEntry = new() { LogLine = logLine.ToString(), Timestamp = entryTimeStamp, EntryType = entryType };
        _logFile.LogEntries.Add(logEntry);
        return logEntry;
    }

    private void HandleUserCommands(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
    {
        int numberOfChars = logLine.RemoveAllWhitespace(_tempString);
        ReadOnlySpan<char> noWhitespaceLogline = _tempString.AsSpan(0, numberOfChars);
        string noWhitespaceSanitized = _sanitizer.SanitizeDelimiterString(noWhitespaceLogline.ToString());

        if (noWhitespaceSanitized.Contains(Constants.CrashedWithDelimiter, StringComparison.OrdinalIgnoreCase)
            || noWhitespaceSanitized.Contains(Constants.CrashedAlternateDelimiter, StringComparison.OrdinalIgnoreCase))
        {
            CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.Crashed);
        }
        else if (noWhitespaceSanitized.Contains(Constants.AfkWithDelimiter, StringComparison.OrdinalIgnoreCase)
            || noWhitespaceSanitized.Contains(Constants.AfkAlternateDelimiter, StringComparison.OrdinalIgnoreCase))
        {
            CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.AfkStart);
        }
        else if (noWhitespaceSanitized.Contains(Constants.AfkEndWithDelimiter, StringComparison.OrdinalIgnoreCase)
            || noWhitespaceSanitized.Contains(Constants.AfkEndAlternateDelimiter, StringComparison.OrdinalIgnoreCase))
        {
            CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.AfkEnd);
        }
        else if (logLine.Contains(Constants.Transfer))
        {
            CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.Transfer);
        }
        else if (noWhitespaceSanitized.Contains(Constants.HitSquadWithDelimiter, StringComparison.OrdinalIgnoreCase)
            || noWhitespaceSanitized.Contains(Constants.HitSquadWithAlternateDelimiter, StringComparison.OrdinalIgnoreCase))
        {
            CreateAndAddLogEntry(logLine, entryTimeStamp, LogEntryType.HitSquad);
        }
    }
}
