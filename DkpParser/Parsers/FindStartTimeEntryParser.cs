// -----------------------------------------------------------------------
// FindStartTimeEntryParser.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Skips past all entries until it finds a timestamp that is the same or later than the specified start time.
/// </summary>
internal sealed class FindStartTimeEntryParser : IParseEntry
{
    private readonly IParseEntry _firstParser;
    private readonly ISetEntryParser _setParser;
    private readonly DateTime _startTime;

    internal FindStartTimeEntryParser(ISetEntryParser setParser, DateTime startTime, IParseEntry firstParser)
    {
        _setParser = setParser;
        _startTime = startTime;
        _firstParser = firstParser;
    }

    public void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
    {
        if (entryTimeStamp < _startTime)
            return;

        _setParser.SetEntryParser(_firstParser);
        _firstParser.ParseEntry(logLine, entryTimeStamp);
        return;
    }
}
