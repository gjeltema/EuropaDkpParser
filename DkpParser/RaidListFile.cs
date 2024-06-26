﻿// -----------------------------------------------------------------------
// RaidListFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.Globalization;
using System.IO;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class RaidListFile
{
    public RaidListFile(string fileName)
    {
        FileName = fileName;
        CharacterNames = [];

        FileInfo raidListFileInfo = new(fileName);
        fileName = raidListFileInfo.Name;

        // RaidTick-2024-03-22_09-47-32.txt
        string raidListFileTimeStamp = fileName[Constants.RaidListFileNameStart.Length..^4];
        FileDateTime = DateTime.ParseExact(raidListFileTimeStamp, Constants.RaidListFileNameTimeFormat, CultureInfo.InvariantCulture);
    }

    public List<PlayerCharacter> CharacterNames { get; private set; }

    public DateTime FileDateTime { get; }

    public string FileName { get; private set; }

    private string DebugDisplay
        => $"{FileDateTime:HH:mm:ss} {CharacterNames.Count}";
}

