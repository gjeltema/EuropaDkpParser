﻿// -----------------------------------------------------------------------
// DkpEntryAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Text.RegularExpressions;

internal sealed partial class DkpEntryAnalyzer : IDkpEntryAnalyzer
{
    private readonly Regex _findDigits = FindDigitsRegex();
    private DkpSpentAnalyzer _dkpSpentAnalyzer;
    private RaidEntries _raidEntries;
    private DkpServerCharacters _serverCharacters;

    public void AnalyzeLootCalls(LogParseResults logParseResults, RaidEntries raidEntries, DkpServerCharacters serverCharacters)
    {
        _raidEntries = raidEntries;
        _dkpSpentAnalyzer = new DkpSpentAnalyzer(_raidEntries.AnalysisErrors.Add);
        _serverCharacters = serverCharacters;

        foreach (EqLogFile log in logParseResults.EqLogFiles)
        {
            IEnumerable<DkpEntry> dkpEntries = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.DkpSpent)
                .Select(ExtractDkpSpentInfo)
                .Where(x => x != null);

            foreach (DkpEntry dkpEntry in dkpEntries)
            {
                _raidEntries.DkpEntries.Add(dkpEntry);
            }

            IEnumerable<DkpEntry> possibleDkpEntries = log.LogEntries
                .Where(x => x.EntryType == LogEntryType.PossibleDkpSpent)
                .Select(ProcessPossibleDkpspentCalls)
                .Where(x => x != null);

            foreach (DkpEntry possibleDkpEntry in possibleDkpEntries)
            {
                _raidEntries.DkpEntries.Add(possibleDkpEntry);
            }
        }
    }

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex FindDigitsRegex();

    private void CheckDkpPlayerName(DkpEntry dkpEntry)
    {
        if (dkpEntry.PossibleError != PossibleError.None)
            return;

        foreach (PlayerLooted playerLootedEntry in _raidEntries.PlayerLootedEntries.Where(x => x.PlayerName.Equals(dkpEntry.PlayerName, StringComparison.OrdinalIgnoreCase)))
        {
            if (playerLootedEntry.ItemLooted == dkpEntry.Item)
                return;
        }

        bool characterNameFound = _serverCharacters.CharacterConfirmedExistsOnDkpServer(dkpEntry.PlayerName)
            || _raidEntries.AllCharactersInRaid.Any(x => x.CharacterName.Equals(dkpEntry.PlayerName, StringComparison.OrdinalIgnoreCase));
        if (!characterNameFound)
        {
            dkpEntry.PossibleError = PossibleError.DkpSpentPlayerNameTypo;
            return;
        }

        // Commented out - too many false positives, almost no real positives.
        //dkpEntry.PossibleError = PossibleError.PlayerLootedMessageNotFound;
    }

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
        try
        {
            entry.Visited = true;

            string logLineNoTimestamp = entry.LogLine[(Constants.LogDateTimeLength + 1)..^1];
            string messageSender = GetMessageSenderName(logLineNoTimestamp);
            if (string.IsNullOrEmpty(messageSender))
                return null;

            int indexOfFirstQuote = logLineNoTimestamp.IndexOf('\'') + 1;
            // 12 being a dumb-check value - the "player tells a channel, '" part should be AT LEAST this long
            // (the actual minimum is definitely higher, but this should catch super odd situations)
            if (indexOfFirstQuote < 12)
                return null;

            string logLineAfterQuote = logLineNoTimestamp[indexOfFirstQuote..].Trim();

            DkpEntry dkpEntry = _dkpSpentAnalyzer.ExtractDkpSpentInfo(logLineAfterQuote, entry.Channel, entry.Timestamp, messageSender);
            dkpEntry.RawLogLine = entry.LogLine;

            if (dkpEntry.PlayerName == Constants.Rot)
                return null;

            if (logLineAfterQuote.IndexOf(Constants.Undo) > 0 || logLineAfterQuote.IndexOf(Constants.Remove) > 0)
            {
                DkpEntry toBeRemoved = GetAssociatedDkpEntry(_raidEntries, dkpEntry);
                if (toBeRemoved != null)
                {
                    _raidEntries.DkpEntries.Remove(toBeRemoved);
                }
                return null;
            }

            CheckDkpPlayerName(dkpEntry);

            return dkpEntry;
        }
        catch (Exception ex)
        {
            EuropaDkpParserException eex = new("An unexpected error occurred when analyzing a DKPSPENT call.", entry.LogLine, ex);
            throw eex;
        }
    }

    private DkpEntry GetAssociatedDkpEntry(RaidEntries raidEntries, DkpEntry dkpEntry)
    {
        DkpEntry associatedEntry = raidEntries.DkpEntries
            .Where(x => x.Timestamp < dkpEntry.Timestamp && x.PlayerName == dkpEntry.PlayerName && x.Item == dkpEntry.Item)
            .MaxBy(x => x.Timestamp);

        return associatedEntry;
    }

    private string GetDigits(string endText)
    {
        Match m = _findDigits.Match(endText);
        return m.Value;
    }

    private string GetMessageSenderName(string logLine)
    {
        int indexOfSpace = logLine.IndexOf(' ');
        if (indexOfSpace < 3)
            return string.Empty;

        string auctioneerName = logLine[0..indexOfSpace].Trim();
        return auctioneerName;
    }

    private DkpEntry ProcessPossibleDkpspentCalls(EqLogEntry entry)
    {
        entry.Visited = true;

        if (entry.LogLine.Length < Constants.LogDateTimeLength + 25)
            return null;

        string logLineNoTimestamp = entry.LogLine[Constants.LogDateTimeLength..];

        string dkpValueText = GetDigits(logLineNoTimestamp);
        if (!int.TryParse(dkpValueText, out int dkpValue))
            return null;

        bool hasSpent = logLineNoTimestamp.Contains(Constants.DkpSpent, StringComparison.OrdinalIgnoreCase);
        if (!hasSpent)
            return null;

        bool hasDkp = logLineNoTimestamp.Contains("DKP", StringComparison.OrdinalIgnoreCase);
        if (!hasDkp)
            return null;

        DkpEntry dkpEntry = new()
        {
            Timestamp = entry.Timestamp,
            RawLogLine = entry.LogLine,
            Channel = entry.Channel,
            DkpSpent = dkpValue,
            PossibleError = PossibleError.MalformedDkpSpentLine
        };

        return dkpEntry;
    }
}

public interface IDkpEntryAnalyzer
{
    void AnalyzeLootCalls(LogParseResults logParseResults, RaidEntries raidEntries, DkpServerCharacters serverCharacters);
}
