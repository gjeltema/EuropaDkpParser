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
    private ZealRaidAttendanceFile(string fullFilePath, string fileName, DateTime fileDateTime)
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

    public string RaidName { get; set; }

    public string ZoneName { get; set; }

    private string DebugDisplay
        => $"{FileDateTime:HH:mm:ss} {CharacterNames.Count}";

    public static ZealRaidAttendanceFile CreateZealRaidAttendanceFile(string fullFilePath)
    {
        FileInfo zealRaidAttendanceFileInfo = new(fullFilePath);
        string fileName = zealRaidAttendanceFileInfo.Name;

        // ZealRaidAttendance_2024-03-22_09-47-32.txt
        string zealRaidAttendanceFileTimeStamp = fileName[Constants.ZealAttendanceBasedFileName.Length..^4];
        DateTime fileDateTime = DateTime.ParseExact(zealRaidAttendanceFileTimeStamp, Constants.ZealRaidAttendanceFileNameTimeFormat, CultureInfo.InvariantCulture);

        return new ZealRaidAttendanceFile(fullFilePath, fileName, fileDateTime);
    }
}
