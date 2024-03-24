// -----------------------------------------------------------------------
// EqLogParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class EqLogParser : IEqLogParser
{
    private readonly IDkpParserSettings _settings;

    public EqLogParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public IList<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
    {
        List<EqLogFile> logFiles = [];
        LogParser parser = new(_settings);
        foreach (string logFileName in _settings.SelectedLogFiles)
        {
            EqLogFile parsedFile = parser.ParseLogFile(logFileName, startTime, endTime);
            if (parsedFile.LogEntries.Count > 0)
                logFiles.Add(parsedFile);
        }

        return logFiles;
    }
}

public interface IEqLogParser
{
    IList<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
