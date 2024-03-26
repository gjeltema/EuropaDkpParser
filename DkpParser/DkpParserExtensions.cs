// -----------------------------------------------------------------------
// DkpParserExtensions.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Globalization;

public static class DkpParserExtensions
{
    public static bool ExtractEqLogTimeStamp(this string logLine, out DateTime result)
    {
        if (logLine.Length < Constants.LogDateTimeLength || string.IsNullOrWhiteSpace(logLine))
        {
            result = DateTime.MinValue;
            return false;
        }

        string timeEntry = logLine[0..Constants.LogDateTimeLength];
        return DateTime.TryParseExact(timeEntry, Constants.LogDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    public static bool IsWithinTwoSecondsOf(this DateTime endTimestamp, DateTime timeStampInPast)
        => endTimestamp >= timeStampInPast && endTimestamp - timeStampInPast <= Constants.DurationOfSearch;
}
