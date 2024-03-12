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

    public ILogParseResults ParseLogs(DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> raidDumpFiles = GetRaidDumpFiles(startTime, endTime);

        return null;
    }

    private List<string> GetAllRaidDumpFileNames(DateTime startTime, DateTime endTime)
    {
        List<string> fileNames = [];
        string endDate = endTime.ToString("yyyyMMdd");
        string date;

        do
        {
            date = startTime.ToString("yyyyMMdd");
            string fileNameSearchString = RaidDumpFile.RaidDumpFileNameStart + date + "-*.txt";
            string[] raidDumpFiles = Directory.GetFiles(fileNameSearchString);
            fileNames.AddRange(raidDumpFiles);

            startTime = startTime.AddDays(1);
            date = startTime.ToString("yyyyMMdd");
        } while (date != endDate);

        return fileNames;
    }

    private List<RaidDumpFile> GetRaidDumpFiles(DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = RaidDumpFile.RaidDumpFileNameStart + "*.txt";
        List<RaidDumpFile> raidDumpFiles = Directory.EnumerateFiles(_settings.EqDirectory, fileNameSearchString).Select(x => new RaidDumpFile(x)).ToList();
        List<RaidDumpFile> relevantDumpFiles = [];

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
    ILogParseResults ParseLogs(DateTime startTime, DateTime endTime);
}
