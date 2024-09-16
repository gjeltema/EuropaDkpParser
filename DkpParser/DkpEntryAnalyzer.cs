// -----------------------------------------------------------------------
// DkpEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Text.RegularExpressions;

internal sealed partial class DkpEntryAnalyzer : IDkpEntryAnalyzer
{
    private readonly Regex _findDigits = FindDigitsRegex();
    private readonly DelimiterStringSanitizer _sanitizer = new();
    private RaidEntries _raidEntries;

    public void AnalyzeLootCalls(LogParseResults logParseResults, RaidEntries raidEntries)
    {
        _raidEntries = raidEntries;
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
        }
    }

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex FindDigitsRegex();

    private void CheckDkpPlayerName(DkpEntry dkpEntry)
    {
        foreach (PlayerLooted playerLootedEntry in _raidEntries.PlayerLootedEntries.Where(x => x.PlayerName.Equals(dkpEntry.PlayerName, StringComparison.OrdinalIgnoreCase)))
        {
            if (playerLootedEntry.ItemLooted == dkpEntry.Item)
                return;
        }

        if (!_raidEntries.AllPlayersInRaid.Any(x => x.PlayerName.Equals(dkpEntry.PlayerName, StringComparison.OrdinalIgnoreCase)))
        {
            dkpEntry.PossibleError = PossibleError.DkpSpentPlayerNameTypo;
            return;
        }

        dkpEntry.PossibleError = PossibleError.PlayerLootedMessageNotFound;
    }

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
        try
        {
            entry.Visited = true;

            // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
            // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
            ReadOnlySpan<char> logLine = _sanitizer.SanitizeDelimiterString(entry.LogLine).AsSpan(Constants.LogDateTimeLength + 1);

            int indexOfFirstDelimiter = logLine.IndexOf(Constants.AttendanceDelimiter);
            ReadOnlySpan<char> auctioneerSection = logLine[0..indexOfFirstDelimiter];
            int indexOfSpace = auctioneerSection.IndexOf(' ');
            if (indexOfSpace < 1)
            {
                _raidEntries.AnalysisErrors.Add($"Unable to extract pieces from DkpEntry: {entry.LogLine}");
                return null;
            }
            string auctioneer = auctioneerSection[..indexOfSpace].ToString();

            int startOfItemSectionIndex = indexOfFirstDelimiter + Constants.AttendanceDelimiter.Length;
            int indexOfSecondDelimiter = logLine[startOfItemSectionIndex..].IndexOf(Constants.AttendanceDelimiter);
            if (indexOfSecondDelimiter < 1)
            {
                _raidEntries.AnalysisErrors.Add($"Unable to extract pieces from DkpEntry: {entry.LogLine}");
                return null;
            }
            string itemName = logLine[startOfItemSectionIndex..][..indexOfSecondDelimiter].ToString();

            ReadOnlySpan<char> playerSection = logLine[(startOfItemSectionIndex + indexOfSecondDelimiter + Constants.AttendanceDelimiter.Length)..].Trim();
            indexOfSpace = playerSection.IndexOf(' ');
            if (indexOfSpace < 1)
            {
                _raidEntries.AnalysisErrors.Add($"Unable to extract pieces from DkpEntry: {entry.LogLine}");
                return null;
            }
            string playerName = playerSection[..indexOfSpace].ToString();

            DkpEntry dkpEntry = new()
            {
                PlayerName = playerName.Trim(),
                Item = itemName.Trim(),
                Timestamp = entry.Timestamp,
                RawLogLine = entry.LogLine,
                Auctioneer = auctioneer,
                Channel = entry.Channel
            };

            if (logLine.IndexOf(Constants.Undo) > 0 || logLine.IndexOf(Constants.Remove) > 0)
            {
                DkpEntry toBeRemoved = GetAssociatedDkpEntry(_raidEntries, dkpEntry);
                if (toBeRemoved != null)
                {
                    _raidEntries.DkpEntries.Remove(toBeRemoved);
                }
                return null;
            }

            string endSection = playerSection[playerName.Length..].ToString();
            GetDkpAmount(endSection, dkpEntry);
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

    private void GetAuctioneerName(string initialLogLine, DkpEntry dkpEntry)
    {
        ReadOnlySpan<char> lineWithoutTimestamp = initialLogLine.AsSpan()[(Constants.LogDateTimeLength + 1)..];
        if (lineWithoutTimestamp.Length == 0)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to extract auctioneer name from DkpEntry: {initialLogLine}");
            dkpEntry.Auctioneer = string.Empty;
            return;
        }

        int indexOfSpace = lineWithoutTimestamp.IndexOf(' ');
        if (indexOfSpace == 0)
        {
            _raidEntries.AnalysisErrors.Add($"Unable to extract auctioneer name from DkpEntry: {initialLogLine}");
            dkpEntry.Auctioneer = string.Empty;
            return;
        }

        string auctioneerName = lineWithoutTimestamp[0..indexOfSpace].ToString();
        dkpEntry.Auctioneer = auctioneerName;
    }

    private string GetDigits(string endText)
    {
        Match m = _findDigits.Match(endText);
        return m.Value;
    }

    private void GetDkpAmount(string endText, DkpEntry dkpEntry)
    {
        // Get digits, since it must be assumed that the auctioneer will add extraneous characters such as '-' and 'alt'.
        string dkpNumber = GetDigits(endText);
        if (string.IsNullOrWhiteSpace(dkpNumber))
        {
            _raidEntries.AnalysisErrors.Add($"Unable to extract DKP amount from DkpEntry: {dkpEntry.RawLogLine}");
            dkpEntry.DkpSpent = 0;
            dkpEntry.PossibleError = PossibleError.ZeroDkp;
            return;
        }

        int.TryParse(dkpNumber, out int dkpAmount);
        if (dkpAmount == 0)
        {
            dkpEntry.PossibleError = PossibleError.ZeroDkp;
        }
        dkpEntry.DkpSpent = dkpAmount;
    }
}

public interface IDkpEntryAnalyzer
{
    void AnalyzeLootCalls(LogParseResults logParseResults, RaidEntries raidEntries);
}
