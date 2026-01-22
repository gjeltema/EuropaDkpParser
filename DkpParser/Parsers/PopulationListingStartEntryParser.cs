// -----------------------------------------------------------------------
// PopulationListingStartEntryParser.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Parses for the beginning of a listing of players from a "/who" command.
/// </summary>
internal sealed class PopulationListingStartEntryParser : IPopulationListingStartEntryParser
{
    private readonly IParseEntry _populationListingParser;
    private readonly IParseEntry _primaryEntryParser;
    private readonly ISetEntryParser _setParser;
    private bool _finishedParse = true;
    private bool _foundFirstLine = false;
    private DateTime _initiateStartOfParseTimeStamp;

    internal PopulationListingStartEntryParser(ISetEntryParser setParser, IParseEntry primaryEntryParser, IParseEntry populationListingParser)
    {
        _setParser = setParser;
        _primaryEntryParser = primaryEntryParser;
        _populationListingParser = populationListingParser;
    }

    public void ParseEntry(ReadOnlySpan<char> logLine, DateTime entryTimeStamp)
    {
        /*
[Tue Dec 02 21:12:41 2025] Players on EverQuest:
[Tue Dec 02 21:12:41 2025] ---------------------------
[Tue Dec 02 21:12:41 2025] [60 Warder] Huggin (Human) <Europa>
[Tue Dec 02 21:12:41 2025] [ANONYMOUS] Squidhunter  <Europa>
[Tue Dec 02 21:12:41 2025] [60 Virtuoso] Kassandra (Wood Elf) <Europa>
[Tue Dec 02 21:12:41 2025] There are 74 players in Temple of Veeshan.
        */

        if (_finishedParse)
        {
            _finishedParse = false;
            _foundFirstLine = false;
        }

        if (!entryTimeStamp.IsWithinDurationOfPopulationThreshold(_initiateStartOfParseTimeStamp))
        {
            _setParser.SetEntryParser(_primaryEntryParser);
            _primaryEntryParser.ParseEntry(logLine, entryTimeStamp);

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
            _setParser.SetEntryParser(_populationListingParser);
            return;
        }

        _primaryEntryParser.ParseEntry(logLine, entryTimeStamp);
    }

    public void SetStartTimeStamp(DateTime startTimeStamp)
        => _initiateStartOfParseTimeStamp = startTimeStamp;
}

public interface IPopulationListingStartEntryParser : IParseEntry
{
    void SetStartTimeStamp(DateTime startTimeStamp);
}
