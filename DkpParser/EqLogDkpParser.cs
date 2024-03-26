// -----------------------------------------------------------------------
// EqLogDkpParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

internal sealed class EqLogDkpParser : IEqLogParser, ISetEntryParser
{
    private readonly IDkpParserSettings _settings;
    private IParseEntry _currentEntryParser;
    private IParseEntry _primaryEntryParser;

    public EqLogDkpParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        _primaryEntryParser = new PrimaryEntryParser(this, logFile);
        SetEntryParser(new FindStartTimeParser(this, startTime, _primaryEntryParser));

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!logLine.ExtractEqLogTimeStamp(out DateTime entryTimeStamp))
            {
                // Log error?  Empty lines in log file, so expected.
                continue;
            }

            if (entryTimeStamp > endTime)
                break;

            _currentEntryParser.ParseEntry(logLine, entryTimeStamp);
        }

        return logFile;
    }

    public void SetEntryParser(IParseEntry parseEntry)
        => _currentEntryParser = parseEntry;
}
