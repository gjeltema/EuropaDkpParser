// -----------------------------------------------------------------------
// PopulationListingStartParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class PopulationListingStartParser : IStartParseEntry
{
    private readonly IParseEntry _attendanceEntryParser;
    private readonly TimeSpan _durationOfSearch = TimeSpan.FromSeconds(2);
    private readonly IParseEntry _populationListingParser;
    private readonly ISetParser _setParser;
    private bool _finishedParse = true;
    private bool _foundFirstLine = false;
    private DateTime _initiateStartOfParseTimeStamp;

    internal PopulationListingStartParser(ISetParser setParser, IParseEntry attendanceEntryParser, IParseEntry populationListingParser)
    {
        _setParser = setParser;
        _attendanceEntryParser = attendanceEntryParser;
        _populationListingParser = populationListingParser;
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        if (_finishedParse)
        {
            _finishedParse = false;
            _foundFirstLine = false;
        }

        if (entryTimeStamp - _initiateStartOfParseTimeStamp > _durationOfSearch)
        {
            _setParser.SetParser(_attendanceEntryParser);
            _attendanceEntryParser.ParseEntry(logLine, entryTimeStamp);

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
            _setParser.SetParser(_populationListingParser);
            return;
        }

        _attendanceEntryParser.ParseEntry(logLine, entryTimeStamp);
    }

    public void SetStartTimeStamp(DateTime startTimeStamp)
        => _initiateStartOfParseTimeStamp = startTimeStamp;
}

public interface IStartParseEntry : IParseEntry
{
    void SetStartTimeStamp(DateTime startTimeStamp);
}
