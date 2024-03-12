// -----------------------------------------------------------------------
// LogParseProcessor.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Collections.Generic;
using System.IO;

public sealed class LogParseProcessor : ILogParseProcessor
{
    // eqlog_Luciasmule_pq.proj.txt
    private readonly IDkpParserSettings _settings;

    public LogParseProcessor(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public LogParseResults ParseLogs(DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> raidDumpFiles = GetRaidDumpFiles(startTime, endTime);
        List<EqLogFile> logFiles = GetEqLogFiles(startTime, endTime);

        LogParseResults results = new(logFiles, raidDumpFiles);
        return results;
    }

    private List<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
    {
        List<EqLogFile> logFiles = [];
        LogParser parser = new(_settings);
        foreach (string logFileName in _settings.SelectedLogFiles)
        {
            EqLogFile parsedFile = parser.ParseLogFile(logFileName, startTime, endTime);
            logFiles.Add(parsedFile);
        }

        return logFiles;
    }

    private List<RaidDumpFile> GetRaidDumpFiles(DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> relevantDumpFiles = [];

        string fileNameSearchString = RaidDumpFile.RaidDumpFileNameStart + "*.txt";
        IEnumerable<RaidDumpFile> raidDumpFiles = Directory.EnumerateFiles(_settings.EqDirectory, fileNameSearchString).Select(x => new RaidDumpFile(x));
        
        foreach (RaidDumpFile dumpFile in raidDumpFiles)
        {
            if (startTime < dumpFile.FileDateTime && dumpFile.FileDateTime < endTime)
            {
                ParseRaidDump(dumpFile);
                relevantDumpFiles.Add(dumpFile);
            }
        }

        return relevantDumpFiles;
    }

    private void ParseRaidDump(RaidDumpFile dumpFile)
    {
        foreach (string line in File.ReadLines(dumpFile.FileName))
        {
            string characterName = line.Split('\t')[1];
            dumpFile.CharacterNames.Add(characterName);
        }
    }
}

public interface ILogParseProcessor
{
    LogParseResults ParseLogs(DateTime startTime, DateTime endTime);
}
