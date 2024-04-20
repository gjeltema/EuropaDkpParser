// -----------------------------------------------------------------------
// DkpEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Text.RegularExpressions;

internal sealed partial class DkpEntryAnalyzer : IDkpEntryAnalyzer
{
    private readonly Regex _findDigits = FindDigitsRegex();
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

    private string CorrectDelimiter(string logLine)
    {
        logLine = logLine.Replace(Constants.PossibleErrorDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(Constants.TooLongDelimiter, Constants.AttendanceDelimiter);
        logLine = logLine.Replace(';', ':');
        return logLine;
    }

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
        try
        {
            entry.Visited = true;

            // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
            // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
            string logLine = CorrectDelimiter(entry.LogLine);

            string[] dkpLineParts = logLine.Split(Constants.AttendanceDelimiter);
            string itemName = dkpLineParts[1].Trim();
            string[] playerParts = dkpLineParts[2].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string playerName = playerParts[0].Trim();

            DkpEntry dkpEntry = new()
            {
                PlayerName = playerName,
                Item = itemName,
                Timestamp = entry.Timestamp,
                RawLogLine = entry.LogLine,
            };

            if (logLine.Contains(Constants.Undo) || logLine.Contains(Constants.Remove))
            {
                DkpEntry toBeRemoved = GetAssociatedDkpEntry(_raidEntries, dkpEntry);
                if (toBeRemoved != null)
                {
                    _raidEntries.DkpEntries.Remove(toBeRemoved);
                }
                return null;
            }

            CheckDkpPlayerName(dkpEntry);

            GetDkpAmount(dkpLineParts[2], dkpEntry);
            GetAuctioneerName(dkpLineParts[0], dkpEntry);

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
        DkpEntry lastOneFound = null;
        IOrderedEnumerable<DkpEntry> associatedEntries = raidEntries.DkpEntries
            .Where(x => x.PlayerName == dkpEntry.PlayerName && x.Item == dkpEntry.Item)
            .OrderBy(x => x.Timestamp);

        foreach (DkpEntry entry in associatedEntries)
        {
            if (entry.Timestamp < dkpEntry.Timestamp)
                lastOneFound = entry;
            else
                break;
        }

        return lastOneFound;
    }

    private void GetAuctioneerName(string initialLogLine, DkpEntry dkpEntry)
    {
        int indexOfBracket = initialLogLine.IndexOf(']');
        int indexOfTell = initialLogLine.IndexOf(" tell");

        string auctioneerName = initialLogLine[(indexOfBracket + 1)..indexOfTell].Trim();
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
