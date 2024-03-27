// -----------------------------------------------------------------------
// DkpLogParseProcessor.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Collections.Generic;
using System.IO;

public sealed class DkpLogParseProcessor : IDkpLogParseProcessor
{
    private readonly IDkpParserSettings _settings;

    public DkpLogParseProcessor(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public LogParseResults ParseLogs(DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> raidDumpFiles = GetRaidDumpFiles(startTime, endTime);
        List<RaidListFile> raidListFiles = GetRaidListFiles(startTime, endTime);
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

    private List<RaidDumpFile> GetRaidDumpFiles(DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> relevantDumpFiles = [];

        string fileNameSearchString = Constants.RaidDumpFileNameStart + "*.txt";
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

    private List<RaidListFile> GetRaidListFiles(DateTime startTime, DateTime endTime)
    {
        List<RaidListFile> relevantRaidListFiles = [];

        string fileNameSearchString = Constants.RaidListFileNameStart + "*.txt";
        IEnumerable<RaidListFile> raidListFiles = Directory.EnumerateFiles(_settings.EqDirectory, fileNameSearchString).Select(x => new RaidListFile(x));

        foreach (RaidListFile raidListFile in raidListFiles)
        {
            if (startTime < raidListFile.FileDateTime && raidListFile.FileDateTime < endTime)
            {
                ParseRaidList(raidListFile);
                relevantRaidListFiles.Add(raidListFile);
            }
        }

        return relevantRaidListFiles;
    }

    private void ParseRaidDump(RaidDumpFile dumpFile)
    {
        foreach (string line in File.ReadLines(dumpFile.FileName))
        {
            string[] characterEntry = line.Split('\t');
            string characterName = characterEntry[1];
            int level = int.Parse(characterEntry[2]);
            string className = characterEntry[3];

            PlayerCharacter character = new()
            {
                PlayerName = characterName,
                Level = level,
                ClassName = className,
            };

            dumpFile.Characters.Add(character);
        }
    }

    private void ParseRaidList(RaidListFile raidListFile)
    {
        bool firstLineSkipped = false;
        foreach (string line in File.ReadLines(raidListFile.FileName))
        {
            if (!firstLineSkipped)
            {
                firstLineSkipped = true;
                continue;
            }

            string[] characterEntry = line.Split('\t');
            string characterName = characterEntry[0];
            int level = int.Parse(characterEntry[1]);
            string className = characterEntry[2];

            PlayerCharacter character = new()
            {
                PlayerName = characterName,
                Level = level,
                ClassName = className,
            };

            raidListFile.CharacterNames.Add(character);
        }
    }
}

public interface IDkpLogParseProcessor
{
    LogParseResults ParseLogs(DateTime startTime, DateTime endTime);
}
