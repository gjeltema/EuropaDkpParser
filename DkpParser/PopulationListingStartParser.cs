// -----------------------------------------------------------------------
// PopulationListingStartParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class PopulationListingStartParser : IParseEntry
{
    private readonly ISetParser _setParser;
    private readonly TimeSpan DurationOfSearch = TimeSpan.FromSeconds(2);
    private bool _finishedParse = true;
    private bool _foundFirstLine = false;
    private DateTime _initiateStartOfParseTimeStamp;

    internal PopulationListingStartParser(ISetParser setParser)
    {
        _setParser = setParser;
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (_finishedParse)
        {
            _finishedParse = false;
            _foundFirstLine = false;
            _initiateStartOfParseTimeStamp = entryTimeStamp;
        }

        if (entryTimeStamp - _initiateStartOfParseTimeStamp > DurationOfSearch)
        {
            //** Switch to normal parser
            //** parse with normal parser

            _finishedParse = true;
            return;
        }

        if (!_foundFirstLine)
        {
            if (logLine.Contains(Constants.PlayersOnEverquest))
            {
                _foundFirstLine = true;
                return;
            }
        }

        if (logLine.EndsWith(Constants.Dashes))
        {
            _finishedParse = true;
            //** Switch to PopulationParser

            return;
        }

        //** Parse with normal parser
    }
}
