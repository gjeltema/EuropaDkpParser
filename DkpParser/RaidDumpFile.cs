﻿// -----------------------------------------------------------------------
// RaidDumpFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Globalization;
using System.IO;

public sealed class RaidDumpFile
{
    public const string RaidDumpFileNameStart = "RaidRoster-";

    public RaidDumpFile(string fileName)
    {
        FileName = fileName;
        CharacterNames = [];

        FileInfo dumpFileInfo = new (fileName);
        fileName = dumpFileInfo.Name;
        string dumpFileTimeStamp = fileName[11..^4];
        FileDateTime = DateTime.ParseExact(dumpFileTimeStamp, Constants.RaidDumpFileNameTimeFormat, CultureInfo.InvariantCulture);
    }

    public DateTime FileDateTime { get; }
    public string FileName { get; private set; }

    public List<string> CharacterNames { get; private set; }
}
