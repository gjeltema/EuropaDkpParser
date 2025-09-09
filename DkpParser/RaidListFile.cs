// -----------------------------------------------------------------------
// RaidListFile.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.Globalization;
using System.IO;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class RaidListFile
{
    private RaidListFile(string fullFilePath, string fileName, DateTime fileDateTime)
    {
        FullFilePath = fullFilePath;
        FileName = fileName;
        FileDateTime = fileDateTime;

        CharacterNames = [];
    }

    public ICollection<PlayerCharacter> CharacterNames { get; }

    public DateTime FileDateTime { get; }

    public string FileName { get; }

    public string FullFilePath { get; }

    private string DebugDisplay
        => $"{FileDateTime:HH:mm:ss} {CharacterNames.Count}";

    public static RaidListFile CreateRaidListFile(string fullFilePath)
    {
        FileInfo raidListFileInfo = new(fullFilePath);
        string fileName = raidListFileInfo.Name;

        // RaidTick-2024-03-22_09-47-32.txt
        string raidListFileTimeStamp = fileName[Constants.RaidListFileNameStart.Length..^4];
        DateTime fileDateTime = DateTime.ParseExact(raidListFileTimeStamp, Constants.RaidListFileNameTimeFormat, CultureInfo.InvariantCulture);

        return new RaidListFile(fullFilePath, fileName, fileDateTime);
    }

    public void ParseContents(IEnumerable<string> contents)
    {
        /*
/out raidlist
Player	Level	Class	Timestamp	Points
Cinu	50	Ranger	2024-03-22_09-47-32	3
Tester	37	Magician	2024-03-22_09-47-32	
        */

        foreach (string line in contents.Skip(1))
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

            CharacterNames.Add(character);
        }
    }
}

