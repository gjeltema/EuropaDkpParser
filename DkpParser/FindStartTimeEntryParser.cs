// -----------------------------------------------------------------------
// FindStartTimeParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

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

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (entryTimeStamp < _startTime)
            return;

        _setParser.SetEntryParser(_firstParser);
        _firstParser.ParseEntry(logLine, entryTimeStamp);
        return;
    }
}
