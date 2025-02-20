// -----------------------------------------------------------------------
// DkpEntryAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Text.RegularExpressions;
using Gjeltema.Logging;

internal sealed partial class DkpEntryAnalyzer : IDkpEntryAnalyzer
{
    private const string LogPrefix = $"[{nameof(DkpEntryAnalyzer)}]";
    private readonly Regex _findDigits = FindDigitsRegex();
    private DkpSpentAnalyzer _dkpSpentAnalyzer;
    private RaidEntries _raidEntries;
    private DkpServerCharacters _serverCharacters;

    public void AnalyzeLootCalls(LogParseResults logParseResults, RaidEntries raidEntries, DkpServerCharacters serverCharacters)
    {
        _raidEntries = raidEntries;
        _dkpSpentAnalyzer = new DkpSpentAnalyzer();
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

            Log.Debug($"{LogPrefix} Processing {entry.LogLine}");

            string logLineNoTimestamp = entry.LogLine[(Constants.LogDateTimeLength + 1)..^1];
            string messageSender = GetMessageSenderName(logLineNoTimestamp);
            if (string.IsNullOrEmpty(messageSender))
            {
                Log.Warning($"{LogPrefix} Unable to extract message sender from: {entry.LogLine}");
                return null;
            }

            int indexOfFirstQuote = logLineNoTimestamp.IndexOf('\'') + 1;
            // 12 being a dumb-check value - the "player tells a channel, '" part should be AT LEAST this long
            // (the actual minimum is definitely higher, but this should catch super odd situations)
            if (indexOfFirstQuote < 12)
            {
                Log.Warning($"{LogPrefix} Index of first quote is too low: {logLineNoTimestamp}");
                return null;
            }

            string logLineAfterQuote = logLineNoTimestamp[indexOfFirstQuote..].Trim();

            DkpEntry dkpEntry = _dkpSpentAnalyzer.ExtractDkpSpentInfo(logLineAfterQuote, entry.Channel, entry.Timestamp, messageSender);
            dkpEntry.RawLogLine = entry.LogLine;

            if (dkpEntry.PlayerName == Constants.Rot)
            {
                Log.Info($"{LogPrefix} DKP call ({entry.LogLine}) is for {Constants.Rot}.");
                return null;
            }

            if (logLineAfterQuote.IndexOf(Constants.Undo) > 0 || logLineAfterQuote.IndexOf(Constants.Remove) > 0)
            {
                Log.Info($"{LogPrefix} DKP call ({entry.LogLine}) is an {Constants.Undo} or {Constants.Remove}.");

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
        {
            Log.Warning($"{LogPrefix} First index of space is too low: {logLine}");
            return string.Empty;
        }

        string auctioneerName = logLine[0..indexOfSpace].Trim();
        return auctioneerName;
    }

    private DkpEntry ProcessPossibleDkpspentCalls(EqLogEntry entry)
    {
        entry.Visited = true;

        if (entry.LogLine.Length < Constants.LogDateTimeLength + 25)
        {
            Log.Info($"{LogPrefix} {nameof(ProcessPossibleDkpspentCalls)} Log line is too short: {entry.LogLine}");
            return null;
        }

        string logLineNoTimestamp = entry.LogLine[Constants.LogDateTimeLength..];

        string dkpValueText = GetDigits(logLineNoTimestamp);
        if (!int.TryParse(dkpValueText, out int dkpValue))
        {
            Log.Info($"{LogPrefix} {nameof(ProcessPossibleDkpspentCalls)} Unable to parse DKP value from '{dkpValueText}', extracted from {logLineNoTimestamp}");
            return null;
        }

        if (entry.Channel != EqChannel.Raid || entry.Channel != EqChannel.Guild)
        {
            Log.Info($"{LogPrefix} {nameof(ProcessPossibleDkpspentCalls)} Possible entry not in valid channel: {entry.Channel}");
            return null;
        }

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
