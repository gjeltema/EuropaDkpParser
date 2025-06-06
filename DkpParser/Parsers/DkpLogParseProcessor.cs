﻿// -----------------------------------------------------------------------
// DkpLogParseProcessor.cs Copyright 2025 Craig Gjeltema
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
        _raidParticipationFilesParser = new();
    }

    public LogParseResults ParseGeneratedLog(string generatedLogFile)
    {
        EqLogDkpParser parser = new(_settings);
        EqLogFile parsedFile = parser.ParseLogFile(generatedLogFile, DateTime.MinValue, DateTime.MaxValue);

        LogParseResults results = new([parsedFile], [], [], []);
        return results;
    }

    public LogParseResults ParseLogs(DkpLogGenerationSessionSettings sessionSettings)
    {
        IList<RaidDumpFile> raidDumpFiles =
            _raidParticipationFilesParser.GetParsedRelevantRaidDumpFiles(sessionSettings.SourceDirectory, sessionSettings.StartTime, sessionSettings.EndTime);
        IList<ZealRaidAttendanceFile> zealRaidAttendanceFiles =
            _raidParticipationFilesParser.GetParsedZealRaidAttendanceFiles(sessionSettings.SourceDirectory, sessionSettings.StartTime, sessionSettings.EndTime);
        IList<RaidListFile> raidListFiles =
            _raidParticipationFilesParser.GetParsedRelevantRaidListFiles(sessionSettings.SourceDirectory, sessionSettings.StartTime, sessionSettings.EndTime);

        List<EqLogFile> logFiles = GetEqLogFiles(sessionSettings.FilesToParse, sessionSettings.StartTime, sessionSettings.EndTime);

        LogParseResults results = new(logFiles, raidDumpFiles, raidListFiles, zealRaidAttendanceFiles);
        return results;
    }

    private List<EqLogFile> GetEqLogFiles(IEnumerable<string> logFilesToParse, DateTime startTime, DateTime endTime)
    {
        List<EqLogFile> logFiles = [];
        EqLogDkpParser parser = new(_settings);
        foreach (string logFileName in logFilesToParse)
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

    LogParseResults ParseLogs(DkpLogGenerationSessionSettings sessionSettings);
}
