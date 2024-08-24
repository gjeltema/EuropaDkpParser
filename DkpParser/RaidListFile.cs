// -----------------------------------------------------------------------
// RaidListFile.cs Copyright 2024 Craig Gjeltema
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
}

