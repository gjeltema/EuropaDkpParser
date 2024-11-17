// -----------------------------------------------------------------------
// DkpSpentAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Text.RegularExpressions;

internal sealed partial class DkpSpentAnalyzer
{
    private readonly Action<string> _errorMessageHandler;
    private readonly Regex _findDigits = FindDigitsRegex();
    private readonly DelimiterStringSanitizer _sanitizer = new();

    public DkpSpentAnalyzer(Action<string> errorMessageHandler)
    {
        _errorMessageHandler = errorMessageHandler;
    }

    public DkpEntry ExtractDkpSpentInfo(string logLine, EqChannel channel, DateTime timestamp)
    {
        // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
        // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
        logLine = _sanitizer.SanitizeDelimiterString(logLine);

        int indexOfFirstDelimiter = logLine.IndexOf(Constants.AttendanceDelimiter);
        string auctioneerSection = logLine[0..indexOfFirstDelimiter];
        int indexOfSpace = auctioneerSection.IndexOf(' ');
        if (indexOfSpace < 1)
        {
            _errorMessageHandler($"Unable to extract pieces from DkpEntry: {logLine}");
            return new DkpEntry
            {
                Timestamp = timestamp,
                RawLogLine = logLine,
                Channel = channel,
                PossibleError = PossibleError.MalformedDkpSpentLine
            };
        }
        string auctioneer = auctioneerSection[..indexOfSpace].ToString();

        int startOfItemSectionIndex = indexOfFirstDelimiter + Constants.AttendanceDelimiter.Length;
        int indexOfSecondDelimiter = logLine[startOfItemSectionIndex..].IndexOf(Constants.AttendanceDelimiter);
        if (indexOfSecondDelimiter < 1)
        {
            _errorMessageHandler($"Unable to extract pieces from DkpEntry: {logLine}");
            return new DkpEntry
            {
                Timestamp = timestamp,
                RawLogLine = logLine,
                Channel = channel,
                Auctioneer = auctioneer,
                PossibleError = PossibleError.MalformedDkpSpentLine
            };
        }
        string itemName = logLine[startOfItemSectionIndex..][..indexOfSecondDelimiter].Trim();

        ReadOnlySpan<char> playerSection = logLine[(startOfItemSectionIndex + indexOfSecondDelimiter + Constants.AttendanceDelimiter.Length)..].Trim();
        indexOfSpace = playerSection.IndexOf(' ');
        if (indexOfSpace < 1)
        {
            _errorMessageHandler($"Unable to extract pieces from DkpEntry: {logLine}");
            return new DkpEntry
            {
                Timestamp = timestamp,
                RawLogLine = logLine,
                Channel = channel,
                Auctioneer = auctioneer,
                Item = itemName,
                PlayerName = playerSection.TrimEnd('\'').Trim().ToString(),
                PossibleError = PossibleError.MalformedDkpSpentLine
            };
        }
        string playerName = playerSection[..indexOfSpace].ToString();

        DkpEntry dkpEntry = new()
        {
            PlayerName = playerName.Trim(),
            Item = itemName,
            Timestamp = timestamp,
            RawLogLine = logLine,
            Auctioneer = auctioneer,
            Channel = channel
        };

        string endSection = playerSection[playerName.Length..].ToString();
        GetDkpAmount(endSection, dkpEntry);

        return dkpEntry;
    }

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex FindDigitsRegex();

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
            _errorMessageHandler($"Unable to extract DKP amount from DkpEntry: {dkpEntry.RawLogLine}");
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
