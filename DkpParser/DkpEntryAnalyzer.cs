// -----------------------------------------------------------------------
// DkpEntryAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class DkpEntryAnalyzer : IDkpEntryAnalyzer
{
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
        return logLine;
    }

    private DkpEntry ExtractDkpSpentInfo(EqLogEntry entry)
    {
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
            entry.Visited = true;
            return null;
        }

        CheckDkpPlayerName(dkpEntry);

        string dkpAmountText = playerParts[1].Trim();
        GetDkpAmount(dkpAmountText, dkpEntry);
        GetAuctioneerName(dkpLineParts[0], dkpEntry);

        entry.Visited = true;

        return dkpEntry;
    }

    private DkpEntry GetAssociatedDkpEntry(RaidEntries raidEntries, DkpEntry dkpEntry)
    {
        DkpEntry lastOneFound = null;
        foreach (DkpEntry entry in raidEntries.DkpEntries.Where(x => x.PlayerName == dkpEntry.PlayerName && x.Item == dkpEntry.Item))
        {
            if (entry.Timestamp < dkpEntry.Timestamp)
                lastOneFound = entry;
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

    private void GetDkpAmount(string dkpAmountText, DkpEntry dkpEntry)
    {
        dkpAmountText = dkpAmountText.TrimEnd('\'');
        if (int.TryParse(dkpAmountText, out int dkpAmount))
        {
            dkpEntry.DkpSpent = dkpAmount;
            return;
        }

        // See if lack of space -> 1DKPSPENT
        int dkpSpentIndex = dkpAmountText.IndexOf(Constants.DkpSpent);
        if (dkpSpentIndex > -1)
        {
            string dkpSpentTextWithoutDkpspent = dkpAmountText[0..dkpSpentIndex];
            if (int.TryParse(dkpSpentTextWithoutDkpspent, out dkpAmount))
            {
                dkpEntry.DkpSpent = dkpAmount;
                return;
            }
        }

        dkpEntry.PossibleError = PossibleError.DkpAmountNotANumber;
    }
}

public interface IDkpEntryAnalyzer
{
    void AnalyzeLootCalls(LogParseResults logParseResults, RaidEntries raidEntries);
}
