// -----------------------------------------------------------------------
// RaidParticipationFilesParser.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class RaidParticipationFilesParser
{
    public IList<RaidDumpFile> GetParsedRelevantRaidDumpFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> relevantRaidDumpFiles = GetRelevantRaidDumpFiles(sourceDirectory, startTime, endTime).ToList();
        foreach (RaidDumpFile raidDumpFile in relevantRaidDumpFiles)
        {
            ParseRaidDump(raidDumpFile);
        }

        return relevantRaidDumpFiles;
    }

    public IList<RaidListFile> GetParsedRelevantRaidListFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        List<RaidListFile> relevantRaidListFiles = GetRelevantRaidListFiles(sourceDirectory, startTime, endTime).ToList();
        foreach (RaidListFile raidListFile in relevantRaidListFiles)
        {
            ParseRaidList(raidListFile);
        }

        return relevantRaidListFiles;
    }

    public IList<ZealRaidAttendanceFile> GetParsedZealRaidAttendanceFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        List<ZealRaidAttendanceFile> relevantZealAttendanceFiles = GetRelevantZealRaidAttendanceFiles(sourceDirectory, startTime, endTime).ToList();
        foreach (ZealRaidAttendanceFile zealAttendanceFile in relevantZealAttendanceFiles)
        {
            ParseZealAttendance(zealAttendanceFile);
        }

        return relevantZealAttendanceFiles.Where(x => x.CharacterNames.Count > 0).ToList();
    }

    public IEnumerable<RaidDumpFile> GetRelevantRaidDumpFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.RaidDumpFileNameStart + "*.txt";
        return Directory.EnumerateFiles(sourceDirectory, fileNameSearchString)
            .Select(RaidDumpFile.CreateRaidDumpFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    public IEnumerable<RaidListFile> GetRelevantRaidListFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.RaidListFileNameStart + "*.txt";
        return Directory.EnumerateFiles(sourceDirectory, fileNameSearchString)
            .Select(RaidListFile.CreateRaidListFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    public IEnumerable<ZealRaidAttendanceFile> GetRelevantZealRaidAttendanceFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.ZealAttendanceBasedFileName + "*.txt";
        return Directory.EnumerateFiles(sourceDirectory, fileNameSearchString)
            .Select(ZealRaidAttendanceFile.CreateZealRaidAttendanceFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    private void ParseRaidDump(RaidDumpFile dumpFile)
    {
        /*
/raiddump
1	Kassandra	50	Bard	Raid Leader	
2	Lucismule	1	Warrior	Group Leader	
        */

        foreach (string line in File.ReadAllLines(dumpFile.FullFilePath))
        {
            string[] characterEntry = line.Split('\t');
            string characterName = characterEntry[1];
            int level = int.Parse(characterEntry[2]);
            string className = characterEntry[3];

            PlayerCharacter character = new()
            {
                CharacterName = characterName,
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

        foreach (string line in File.ReadAllLines(raidListFile.FullFilePath).Skip(1))
        {

            string[] characterEntry = line.Split('\t');
            string characterName = characterEntry[0];
            int level = int.Parse(characterEntry[1]);
            string className = characterEntry[2];

            PlayerCharacter character = new()
            {
                CharacterName = characterName,
                Level = level,
                ClassName = className,
            };

            raidListFile.CharacterNames.Add(character);
        }
    }

    private void ParseZealAttendance(ZealRaidAttendanceFile zealAttendanceFile)
    {
        /*
First Call|Veeshans Peak
1|Kassandra|Bard|60|Raid Leader	
2|Lucismule|Warrior|1|Group Leader	
        */

        string[] fileContents = File.ReadAllLines(zealAttendanceFile.FullFilePath);
        if (fileContents.Length < 2)
            return;

        string firstLine = fileContents[0];
        string[] firstLineSplit = firstLine.Split('|');
        if (firstLineSplit.Length < 2)
            return;

        zealAttendanceFile.RaidName = firstLineSplit[0];
        zealAttendanceFile.ZoneName = firstLineSplit[1];

        foreach (string line in fileContents.Skip(1))
        {
            string[] characterEntry = line.Split('|');
            string characterName = characterEntry[1];
            string className = characterEntry[2];
            int level = int.Parse(characterEntry[3]);

            PlayerCharacter character = new()
            {
                CharacterName = characterName,
                Level = level,
                ClassName = className,
            };

            zealAttendanceFile.CharacterNames.Add(character);
        }
    }
}
