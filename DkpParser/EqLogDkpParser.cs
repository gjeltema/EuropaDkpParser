﻿// -----------------------------------------------------------------------
// EqLogDkpParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class EqLogDkpParser : EqLogParserBase, IEqLogParser
{
    private readonly IDkpParserSettings _settings;
    private IParseEntry _primaryEntryParser;

    public EqLogDkpParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        _primaryEntryParser = new PrimaryEntryParser(this, logFile);
        SetEntryParser(new FindStartTimeEntryParser(this, startTime, _primaryEntryParser));
    }
}
