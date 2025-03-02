// -----------------------------------------------------------------------
// DelimiterStringSanitizer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Text.RegularExpressions;

internal sealed partial class DelimiterStringSanitizer
{
    private static readonly Regex _findShort = FindShortDelimiterRegex();

    public string SanitizeDelimiterString(string toBeSanitized)
    {
        toBeSanitized = toBeSanitized.Replace(';', ':');
        toBeSanitized = toBeSanitized.Replace(Constants.TooLongDelimiter, Constants.AttendanceDelimiter);

        while (_findShort.IsMatch(toBeSanitized))
        {
            Match test = _findShort.Match(toBeSanitized);
            string replacementText = test.Value.Replace(Constants.PossibleErrorDelimiter, Constants.AttendanceDelimiter);
            toBeSanitized = toBeSanitized.Replace(test.Value, replacementText);
        }

        return toBeSanitized;
    }

    [GeneratedRegex("[^:]::[^:]", RegexOptions.Compiled)]
    private static partial Regex FindShortDelimiterRegex();
}
