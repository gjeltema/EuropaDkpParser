// -----------------------------------------------------------------------
// PrimaryEntryParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Main entry parser for EQ Log files for DKP entry.
/// </summary>
internal sealed class PrimaryEntryParser : IParseEntry
{
    private readonly EqLogFile _logFile;
    private readonly ISetEntryParser _setParser;
    private IParseEntry _populationListingParser;
    private IPopulationListingStartEntryParser _populationListingStartParser;

    internal PrimaryEntryParser(ISetEntryParser setParser, EqLogFile logFile)
    {
        _setParser = setParser;
        _logFile = logFile;

        _populationListingParser = new PopulationListingEntryParser(setParser, logFile, this);
        _populationListingStartParser = new PopulationListingStartEntryParser(setParser, this, _populationListingParser);
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        // Check for just '::' first as it's a fast check.  Do more in depth parsing of the line if this is present.
        if (logLine.Contains(Constants.PossibleErrorDelimiter))
        {
            AddDelimiterEntry(logLine, entryTimeStamp);
        }
        else if (logLine.EndsWith(Constants.EndLootedDashes) && logLine.Contains(Constants.LootedA))
        {
            AddLootedEntry(logLine, entryTimeStamp);
        }
        // Check for just 'raid.' first as it's a fast check. Do more in depth parsing of the line if this is present.
        else if (logLine.EndsWith(Constants.Raid))
        {
            AddRaidJoinLeaveEntry(logLine, entryTimeStamp);
        }
    }

    private void AddDelimiterEntry(string logLine, DateTime entryTimeStamp)
    {
        if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
        {
            if (!IsValidDkpspentChannel(logLine))
                return;

            EqLogEntry logEntry = CreateAndAddLogEntry(logLine, entryTimeStamp);
            logEntry.EntryType = LogEntryType.DkpSpent;
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
            else if (logLine.Contains(Constants.Crashed))
            {
                logEntry.EntryType = LogEntryType.Crashed;
            }
        }
    }

    private void AddLootedEntry(string logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
        logEntry.EntryType = LogEntryType.PlayerLooted;
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

    private void CheckForTwoColonError(EqLogEntry logEntry, string logLine)
    {
        if (!logLine.Contains(Constants.AttendanceDelimiter))
        {
            logEntry.ErrorType = PossibleError.TwoColons;
        }
    }

    private EqLogEntry CreateAndAddLogEntry(string logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
        _logFile.LogEntries.Add(logEntry);
        CheckForTwoColonError(logEntry, logLine);
        return logEntry;
    }

    private EqLogEntry CreateLogEntry(string logLine, DateTime entryTimeStamp)
        => new() { LogLine = logLine, Timestamp = entryTimeStamp };

    private bool IsValidDkpspentChannel(string logLine)
        => logLine.Contains(Constants.RaidYou)
            || logLine.Contains(Constants.RaidOther)
            || logLine.Contains(Constants.GuildYou)
            || logLine.Contains(Constants.GuildOther)
            || logLine.Contains(Constants.OocYou)
            || logLine.Contains(Constants.OocOther)
            || logLine.Contains(Constants.AuctionYou)
            || logLine.Contains(Constants.AuctionOther);
}
