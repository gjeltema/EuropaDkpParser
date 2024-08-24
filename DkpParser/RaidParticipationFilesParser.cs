// -----------------------------------------------------------------------
// RaidParticipationFilesParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class RaidParticipationFilesParser
{
    private readonly IDkpParserSettings _settings;

    public RaidParticipationFilesParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public IList<RaidDumpFile> GetParsedRelevantRaidDumpFiles(DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> relevantRaidDumpFiles = GetRelevantRaidDumpFiles(startTime, endTime).ToList();
        foreach (RaidDumpFile raidDumpFile in relevantRaidDumpFiles)
        {
            ParseRaidDump(raidDumpFile);
        }

        return relevantRaidDumpFiles;
    }

    public IList<RaidListFile> GetParsedRelevantRaidListFiles(DateTime startTime, DateTime endTime)
    {
        List<RaidListFile> relevantRaidListFiles = GetRelevantRaidListFiles(startTime, endTime).ToList();
        foreach (RaidListFile raidListFile in relevantRaidListFiles)
        {
            ParseRaidList(raidListFile);
        }

        return relevantRaidListFiles;
    }

    public IEnumerable<RaidDumpFile> GetRelevantRaidDumpFiles(DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.RaidDumpFileNameStart + "*.txt";
        return Directory.EnumerateFiles(_settings.EqDirectory, fileNameSearchString)
            .Select(RaidDumpFile.CreateRaidDumpFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    public IEnumerable<RaidListFile> GetRelevantRaidListFiles(DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.RaidListFileNameStart + "*.txt";
        return Directory.EnumerateFiles(_settings.EqDirectory, fileNameSearchString)
            .Select(RaidListFile.CreateRaidListFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    private void ParseRaidDump(RaidDumpFile dumpFile)
    {
        /*
/raiddump
1	Kassandra	50	Bard	Raid Leader	
2	Lucismule	1	Warrior	Group Leader	
        */

        foreach (string line in File.ReadLines(dumpFile.FullFilePath))
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
        foreach (string line in File.ReadLines(raidListFile.FullFilePath))
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
