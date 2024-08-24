// -----------------------------------------------------------------------
// FullEqLogParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Extracts out every log entry between the specified timestamps.
/// </summary>
public sealed class FullEqLogParser : EqLogParserBase, IFullEqLogParser
{
    private readonly IDkpParserSettings _settings;

    public FullEqLogParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
        => GetEqLogFiles(startTime, endTime, _settings.SelectedLogFiles);

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        LogEverythingEntryParser logEverything = new(logFile);
        FindStartTimeEntryParser findStartParser = new(this, startTime, logEverything);

        SetEntryParser(findStartParser);
    }
}

public sealed class LogEverythingEntryParser : IParseEntry
{
    private readonly EqLogFile _logFile;

    public LogEverythingEntryParser(EqLogFile logFile)
    {
        _logFile = logFile;
    }

    public void ParseEntry(string logLine, DateTime entryTimeStamp)
    {
        EqLogEntry logEntry = new()
        {
            EntryType = LogEntryType.Unknown,
            LogLine = logLine,
            Timestamp = entryTimeStamp
        };

        _logFile.LogEntries.Add(logEntry);
    }
}

/// <summary>
/// Extracts out every log entry between the specified timestamps.
/// </summary>
public interface IFullEqLogParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
