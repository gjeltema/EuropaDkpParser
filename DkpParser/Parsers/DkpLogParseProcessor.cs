// -----------------------------------------------------------------------
// DkpLogParseProcessor.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

using System.IO;

/// <summary>
/// Parses the EQ generated log files, RaidDumps and RaidList files for relevante DKP attendance and spending entries.
/// </summary>
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
        /*
/raiddump
1	Kassandra	50	Bard	Raid Leader	
2	Lucismule	1	Warrior	Group Leader	
        */

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
        /*
/out raidlist
Player	Level	Class	Timestamp	Points
Cinu	50	Ranger	2024-03-22_09-47-32	3
Tester	37	Magician	2024-03-22_09-47-32	
        */

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

/// <summary>
/// Parses the EQ generated log files, RaidDumps and RaidList files for relevante DKP attendance and spending entries.
/// </summary>
public interface IDkpLogParseProcessor
{
    LogParseResults ParseLogs(DateTime startTime, DateTime endTime);
}
