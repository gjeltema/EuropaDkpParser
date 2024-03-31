﻿// -----------------------------------------------------------------------
// PrimaryEntryParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class PrimaryEntryParser : IParseEntry
{
    private readonly EqLogFile _logFile;
    private readonly ISetEntryParser _setParser;
    private IParseEntry _populationListingParser;
    private IStartParseEntry _populationListingStartParser;

    internal PrimaryEntryParser(ISetEntryParser setParser, EqLogFile logFile)
    {
        _setParser = setParser;
        _logFile = logFile;

        _populationListingParser = new PopulationListingEntryParser(setParser, logFile, this);
        _populationListingStartParser = new PopulationListingStartEntryParser(setParser, this, _populationListingParser);
    }

    // [Tue Feb 27 23:13:23 2024] Orsino has left the raid.
    // [Tue Feb 27 23:14:20 2024] Marco joined the raid.
    // [Sun Feb 25 22:52:46 2024] You have joined the group.
    // [Thu Feb 22 23:13:52 2024] Luciania joined the raid.
    // [Thu Feb 22 23:13:52 2024] You have joined the raid.

    // [Wed Feb 21 23:10:33 2024] Remote has joined your guild.
    // [Wed Feb 21 23:10:33 2024] Remote is now a regular member of your guild.
    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (logLine.Contains(Constants.PossibleErrorDelimiter))
        {
            AddDelimiterEntry(logLine, entryTimeStamp);
        }
        else if (logLine.EndsWith(Constants.EndLootedDashes) && logLine.Contains(Constants.LootedA))
        {
            AddLootedEntry(logLine, entryTimeStamp);
        }
        else if (logLine.Contains(Constants.Raid))
        {
            AddRaidJoinLeaveEntry(logLine, entryTimeStamp);
        }
    }

    private void AddDelimiterEntry(string logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
        _logFile.LogEntries.Add(logEntry);
        CheckForTwoColonError(logEntry, logLine);

        if (logLine.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.DkpSpent;
        }
        else if (logLine.Contains(Constants.KillCall, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.Kill;
            _populationListingStartParser.SetStartTimeStamp(entryTimeStamp);
            _setParser.SetEntryParser(_populationListingStartParser);
        }
        else if (logLine.Contains(Constants.RaidAttendanceTaken, StringComparison.OrdinalIgnoreCase))
        {
            logEntry.EntryType = LogEntryType.Attendance;
            _populationListingStartParser.SetStartTimeStamp(entryTimeStamp);
            _setParser.SetEntryParser(_populationListingStartParser);
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
        if (logLine.Contains(Constants.JoinedRaid))
        {
            EqLogEntry logEntry = CreateLogEntry(logLine, entryTimeStamp);
            _logFile.LogEntries.Add(logEntry);
            logEntry.EntryType = LogEntryType.JoinedRaid;
        }
        else if (logLine.Contains(Constants.LeftRaid))
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

    private EqLogEntry CreateLogEntry(string logLine, DateTime entryTimeStamp)
        => new() { LogLine = logLine, Timestamp = entryTimeStamp };
}
