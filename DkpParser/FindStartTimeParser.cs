// -----------------------------------------------------------------------
// FindStartTimeParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System;

internal sealed class FindStartTimeParser : IParseEntry
{
    private readonly ISetParser _setParser;
    private readonly DateTime _startTime;
    private readonly IParseEntry _firstParser;

    internal FindStartTimeParser(ISetParser setParser, DateTime startTime, IParseEntry firstParser)
    {
        _setParser = setParser;
        _startTime = startTime;
        _firstParser = firstParser;
    }

    public EqLogEntry ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (entryTimeStamp < _startTime)
            return null;

        _setParser.SetParser(_firstParser);
        return _firstParser.ParseEntry(logLine, entryTimeStamp);
    }
}
