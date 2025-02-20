// -----------------------------------------------------------------------
// DkpSpentAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Text.RegularExpressions;
using Gjeltema.Logging;

internal sealed partial class DkpSpentAnalyzer
{
    private const string LogPrefix = $"[{nameof(DkpSpentAnalyzer)}]";
    private readonly Regex _findDigits = FindDigitsRegex();
    private readonly DelimiterStringSanitizer _sanitizer = new();

    public DkpEntry ExtractDkpSpentInfo(string messageFromSender, EqChannel channel, DateTime timestamp, string messageSender)
    {
        Log.Debug($"{LogPrefix} Extracting DKP info from message: {messageFromSender}");

        // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
        // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
        messageFromSender = _sanitizer.SanitizeDelimiterString(messageFromSender);

        int indexOfFirstDelimiter = messageFromSender.IndexOf(Constants.AttendanceDelimiter);
        int startOfItemSectionIndex = indexOfFirstDelimiter + Constants.AttendanceDelimiter.Length;

        string[] messageParts = messageFromSender[startOfItemSectionIndex..].Split(Constants.AttendanceDelimiter);
        if (messageParts.Length != 2)
        {
            Log.Warning($"{LogPrefix} Unable to extract pieces from DkpEntry - not enough messageParts: {messageFromSender}");
            return new DkpEntry
            {
                Timestamp = timestamp,
                RawLogLine = messageFromSender,
                Channel = channel,
                Auctioneer = messageSender,
                PossibleError = PossibleError.MalformedDkpSpentLine
            };
        }

        string itemName = messageParts[0].Trim();

        string afterItemSection = messageParts[1].Trim();

        int indexOfSpace = afterItemSection.IndexOf(' ');
        if (indexOfSpace < 3)
        {
            Log.Warning($"{LogPrefix} Unable to extract pieces from DkpEntry - index of first space too low: {messageFromSender}");
            return new DkpEntry
            {
                Timestamp = timestamp,
                RawLogLine = messageFromSender,
                Channel = channel,
                Auctioneer = messageSender,
                Item = itemName,
                PlayerName = ProcessName(afterItemSection.TrimEnd('\'').Trim().ToString()),
                PossibleError = PossibleError.MalformedDkpSpentLine
            };
        }

        string playerName = afterItemSection[0..indexOfSpace];

        DkpEntry dkpEntry = new()
        {
            PlayerName = ProcessName(playerName),
            Item = itemName,
            Timestamp = timestamp,
            RawLogLine = messageFromSender,
            Auctioneer = messageSender,
            Channel = channel
        };

        string endSection = afterItemSection[(indexOfSpace + 1)..];
        GetDkpAmount(endSection, dkpEntry);

        Log.Trace($"{LogPrefix} Parsed DKP entry: {dkpEntry}");

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
        Log.Debug($"{LogPrefix} {nameof(GetDkpAmount)} for {endText}");

        // Get digits, since it must be assumed that the auctioneer will add extraneous characters such as '-' and 'alt'.
        string dkpNumber = GetDigits(endText);
        if (string.IsNullOrWhiteSpace(dkpNumber))
        {
            Log.Warning($"{LogPrefix} Unable to extract DKP amount from DkpEntry: {dkpEntry.RawLogLine}");
            dkpEntry.DkpSpent = 0;
            dkpEntry.PossibleError = PossibleError.ZeroDkp;
            return;
        }

        int.TryParse(dkpNumber, out int dkpAmount);
        if (dkpAmount == 0)
        {
            Log.Warning($"{LogPrefix} DKP amount for {endText} is 0.  Value parsed: {dkpNumber}");
            dkpEntry.PossibleError = PossibleError.ZeroDkp;
        }
        dkpEntry.DkpSpent = dkpAmount;
    }

    private string ProcessName(string rawName)
    {
        string trimmedName = rawName.Trim();
        if (trimmedName == Constants.Rot)
            return trimmedName;
        else
            return trimmedName.NormalizeName();
    }
}
