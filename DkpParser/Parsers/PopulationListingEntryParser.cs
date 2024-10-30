﻿// -----------------------------------------------------------------------
// PopulationListingEntryParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Parses a "/who" log entry, looking for all the players and the ending "players in zone" lines.
/// </summary>
internal sealed class PopulationListingEntryParser : IParseEntry
{
    private readonly EqLogFile _logFile;
    private readonly IParseEntry _primaryEntryParser;
    private readonly ISetEntryParser _setParser;

    internal PopulationListingEntryParser(ISetEntryParser setParser, EqLogFile logFile, IParseEntry primaryEntryParser)
    {
        _setParser = setParser;
        _logFile = logFile;
        _primaryEntryParser = primaryEntryParser;
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        // [Sun Jun 09 19:59:39 2024] [50 Paladin] Trident (Half Elf) <Europa>
        // [Sun Jun 09 19:59:39 2024] [50 Magician] Cemtex (Dark Elf) <Europa> LFG
        // [Sun Jun 09 19:59:39 2024] [ANONYMOUS] Cyberjam  <Europa>
        // [Mon Oct 28 21:32:54 2024]  AFK [55 Blackguard] Ilsidor (Human) <Europa>
        if (logLine.Contains(Constants.EuropaGuildTag))
        {
            EqLogEntry logEntry = new()
            {
                LogLine = logLine,
                Timestamp = entryTimeStamp,
                EntryType = LogEntryType.CharacterName
            };
            _logFile.LogEntries.Add(logEntry);
        }
        // [Sun Jun 09 19:59:39 2024] There are 25 players in Everfrost Peaks.
        // [Mon Oct 28 20:12:20 2024] There is 1 player in Frontier Mountains.
        else if ((logLine.Contains(Constants.WhoZonePrefixPlural) && logLine.Contains(Constants.PlayersIn))
            || (logLine.Contains(Constants.WhoZonePrefixSingle) && logLine.Contains(Constants.PlayerIn)))
        {
            EqLogEntry logEntry = new()
            {
                LogLine = logLine,
                Timestamp = entryTimeStamp,
                EntryType = LogEntryType.WhoZoneName
            };
            _logFile.LogEntries.Add(logEntry);

            _setParser.SetEntryParser(_primaryEntryParser);
        }
    }
}
