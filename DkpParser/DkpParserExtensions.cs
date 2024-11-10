// -----------------------------------------------------------------------
// DkpParserExtensions.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using DkpParser.Parsers;

public static class DkpParserExtensions
{
    public static bool Contains(this ReadOnlySpan<char> chars, string searchedFor)
        => chars.Contains(searchedFor, StringComparison.Ordinal);

    public static bool IsWithinTwoSecondsOf(this DateTime endTimestamp, DateTime timeStampInPast)
        => endTimestamp >= timeStampInPast && endTimestamp - timeStampInPast <= Constants.DurationOfSearch;

    public static string ToUsTimestamp(this DateTime timeStamp, string format)
        => timeStamp.ToString(format, Constants.UsCulture);

    // Just reusing the function already implemented in EqLogParserBase.
    // Leaving that function in that class as I *think* it'll be faster for that parsing there,
    // where performance is more important.
    public static bool TryExtractEqLogTimeStamp(this string logLine, out DateTime result)
        => EqLogParserBase.TryExtractEqLogTimeStamp(logLine, out result);
}
