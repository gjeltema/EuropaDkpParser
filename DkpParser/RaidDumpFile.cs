﻿// -----------------------------------------------------------------------
// RaidDumpFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.Globalization;
using System.IO;

[DebuggerDisplay("{DebugDisplay,nq")]
public sealed class RaidDumpFile
{
    public const string RaidDumpFileNameStart = "RaidRoster-";

    public RaidDumpFile(string fileName)
    {
        FileName = fileName;
        CharacterNames = [];

        FileInfo dumpFileInfo = new(fileName);
        fileName = dumpFileInfo.Name;
        string dumpFileTimeStamp = fileName[11..^4];
        FileDateTime = DateTime.ParseExact(dumpFileTimeStamp, Constants.RaidDumpFileNameTimeFormat, CultureInfo.InvariantCulture);
    }

    public List<string> CharacterNames { get; private set; }

    public DateTime FileDateTime { get; }

    public string FileName { get; private set; }

    private string DebugDisplay
        => $"{FileName} {CharacterNames.Count}";
}
