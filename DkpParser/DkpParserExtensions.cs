// -----------------------------------------------------------------------
// DkpParserExtensions.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public static class DkpParserExtensions
{
    public static bool IsWithinTwoSecondsOf(this DateTime endTimestamp, DateTime timeStampInPast)
        => endTimestamp >= timeStampInPast && endTimestamp - timeStampInPast <= Constants.DurationOfSearch;
}
