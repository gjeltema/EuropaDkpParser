// -----------------------------------------------------------------------
// DkpParserExtensions.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using DkpParser.Parsers;

public static class DkpParserExtensions
{
    private const string AttendanceCallStart = $"/rs {Constants.AttendanceDelimiter}Raid Attendance Taken{Constants.AttendanceDelimiter}";

    public static bool Contains(this ReadOnlySpan<char> chars, string searchedFor)
        => chars.Contains(searchedFor, StringComparison.Ordinal);

    public static string GetAttendanceCall(this AttendanceCallType callType, string attendanceName)
    {
        if (callType == AttendanceCallType.Time)
            return $"{AttendanceCallStart}Attendance{Constants.AttendanceDelimiter}{attendanceName}{Constants.AttendanceDelimiter}";
        else
            return $"{AttendanceCallStart}{attendanceName}{Constants.AttendanceDelimiter}KILL{Constants.AttendanceDelimiter}";
    }

    public static bool IsWithinDurationOfPopulationThreshold(this DateTime endTimestamp, DateTime timeStampInPast)
        => endTimestamp >= timeStampInPast && endTimestamp - timeStampInPast <= Constants.DurationOfSearch;

    public static string NormalizeName(this string name)
    {
        if (name == null)
            return string.Empty;

        name = name.Trim();
        if (name.Length > 1)
            return string.Concat(char.ToUpper(name[0]), name[1..].ToLower());
        else
            return name.ToUpper();
    }

    public static string ToUsTimestamp(this DateTime timeStamp, string format)
        => timeStamp.ToString(format, Constants.UsCulture);

    // Just reusing the function already implemented in EqLogParserBase.
    // Leaving that function in that class as I *think* it'll be faster for that parsing there,
    // where performance is more important.
    public static bool TryExtractEqLogTimeStamp(this string logLine, out DateTime result)
        => EqLogParserBase.TryExtractEqLogTimeStamp(logLine, out result);
}
