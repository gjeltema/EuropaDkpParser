// -----------------------------------------------------------------------
// RaidDumpFile.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.Globalization;
using System.IO;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class RaidDumpFile
{
    private RaidDumpFile(string fullFilePath, string fileName, DateTime fileDateTime)
    {
        FullFilePath = fullFilePath;
        FileName = fileName;
        FileDateTime = fileDateTime;

        Characters = [];
    }

    public ICollection<PlayerCharacter> Characters { get; }

    public DateTime FileDateTime { get; }

    public string FileName { get; }

    public string FullFilePath { get; }

    private string DebugDisplay
        => $"{FileDateTime:HH:mm:ss} {Characters.Count}";

    public static RaidDumpFile CreateRaidDumpFile(string fullFilePath)
    {
        FileInfo dumpFileInfo = new(fullFilePath);
        string fileName = dumpFileInfo.Name;

        // RaidRoster-20240312-161830.txt
        string dumpFileTimeStamp = fileName[Constants.RaidDumpFileNameStart.Length..^4];
        DateTime fileDateTime = DateTime.ParseExact(dumpFileTimeStamp, Constants.RaidDumpFileNameTimeFormat, CultureInfo.InvariantCulture);

        return new RaidDumpFile(fullFilePath, fileName, fileDateTime);
    }

    public void ParseContents(IEnumerable<string> contents)
    {
        /*
/raiddump
1	Kassandra	50	Bard	Raid Leader	
2	Lucismule	1	Warrior	Group Leader	
        */

        foreach (string line in contents)
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

            Characters.Add(character);
        }
    }
}
