// -----------------------------------------------------------------------
// ZealRaidAttendanceFile.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.Globalization;
using System.IO;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class ZealRaidAttendanceFile
{
    private const char Delimiter = '|';

    private ZealRaidAttendanceFile(string fullFilePath, string fileName, DateTime fileDateTime)
    {
        FullFilePath = fullFilePath;
        FileName = fileName;
        FileDateTime = fileDateTime;

        CharacterNames = [];
    }

    public AttendanceCallType CallType { get; private set; }

    public ICollection<PlayerCharacter> CharacterNames { get; }

    public DateTime FileDateTime { get; }

    public string FileName { get; }

    public string FullFilePath { get; }

    public string RaidName { get; private set; }

    public string ZoneName { get; private set; }

    private string DebugDisplay
        => $"{FileDateTime:HH:mm:ss} {RaidName} {CharacterNames.Count}";

    public static ZealRaidAttendanceFile CreateZealRaidAttendanceFile(string fullFilePath)
    {
        FileInfo zealRaidAttendanceFileInfo = new(fullFilePath);
        string fileName = zealRaidAttendanceFileInfo.Name;

        // ZealRaidAttendance_2024-03-22_09-47-32.txt
        string zealRaidAttendanceFileTimeStamp = fileName[Constants.ZealAttendanceBasedFileName.Length..^4];
        DateTime fileDateTime = DateTime.ParseExact(zealRaidAttendanceFileTimeStamp, Constants.ZealRaidAttendanceFileNameTimeFormat, CultureInfo.InvariantCulture);

        return new ZealRaidAttendanceFile(fullFilePath, fileName, fileDateTime);
    }

    public static string GetFileLine(string group, string name, string className, string level, string rank)
        => $"{group}{Delimiter}{name}{Delimiter}{className}{Delimiter}{level}{Delimiter}{rank}";

    public static string GetFirstLine(string raidName, string zoneName, AttendanceCallType callType)
        => $"{raidName}{Delimiter}{zoneName}{Delimiter}{callType}";

    public void ParseZealAttendance(IEnumerable<string> contents)
    {
        /*
First Call|Veeshans Peak
1|Kassandra|Bard|60|Raid Leader	
2|Lucismule|Warrior|1|Group Leader	
        */

        string firstLine = contents.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstLine))
            return;

        string[] firstLineSplit = firstLine.Split(Delimiter);
        if (firstLineSplit.Length < 3)
            return;

        RaidName = firstLineSplit[0];
        ZoneName = firstLineSplit[1];

        string callTypeRaw = firstLineSplit[2];
        if (!Enum.TryParse(callTypeRaw, out AttendanceCallType callType))
        {
            callType = AttendanceCallType.Time;
        }

        CallType = callType;

        foreach (string line in contents.Skip(1))
        {
            string[] characterEntry = line.Split(Delimiter);
            string characterName = characterEntry[1];
            string className = characterEntry[2];
            int level = int.Parse(characterEntry[3]);

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
