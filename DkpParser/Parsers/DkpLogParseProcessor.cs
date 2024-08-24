// -----------------------------------------------------------------------
// DkpLogParseProcessor.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

/// <summary>
/// Parses the EQ generated log files, RaidDumps and RaidList files for relevante DKP attendance and spending entries.
/// </summary>
public sealed class DkpLogParseProcessor : IDkpLogParseProcessor
{
    private readonly RaidParticipationFilesParser _raidParticipationFilesParser;
    private readonly IDkpParserSettings _settings;

    public DkpLogParseProcessor(IDkpParserSettings settings)
    {
        _settings = settings;
        _raidParticipationFilesParser = new(_settings);
    }

    public LogParseResults ParseGeneratedLog(string generatedLogFile)
    {
        EqLogDkpParser parser = new(_settings);
        EqLogFile parsedFile = parser.ParseLogFile(generatedLogFile, DateTime.MinValue, DateTime.MaxValue);

        LogParseResults results = new([parsedFile], [], []);
        return results;
    }

    public LogParseResults ParseLogs(DateTime startTime, DateTime endTime)
    {
        IList<RaidDumpFile> raidDumpFiles = _raidParticipationFilesParser.GetParsedRelevantRaidDumpFiles(startTime, endTime);
        IList<RaidListFile> raidListFiles = _raidParticipationFilesParser.GetParsedRelevantRaidListFiles(startTime, endTime);
        IList<EqLogFile> logFiles = GetEqLogFiles(startTime, endTime);

        LogParseResults results = new(logFiles, raidDumpFiles, raidListFiles);
        return results;
    }

    private IList<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
    {
        List<EqLogFile> logFiles = [];
        EqLogDkpParser parser = new(_settings);
        foreach (string logFileName in _settings.SelectedLogFiles)
        {
            EqLogFile parsedFile = parser.ParseLogFile(logFileName, startTime, endTime);
            if (parsedFile.LogEntries.Count > 0)
                logFiles.Add(parsedFile);
        }

        return logFiles;
    }
}

/// <summary>
/// Parses the EQ generated log files, RaidDumps and RaidList files for relevante DKP attendance and spending entries.
/// </summary>
public interface IDkpLogParseProcessor
{
    LogParseResults ParseGeneratedLog(string generatedLogFile);

    LogParseResults ParseLogs(DateTime startTime, DateTime endTime);
}
