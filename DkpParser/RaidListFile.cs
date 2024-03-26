// -----------------------------------------------------------------------
// RaidListFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.Globalization;
using System.IO;

[DebuggerDisplay("{DebugDisplay,nq")]
public sealed class RaidListFile
{
    public const string RaidListFileNameStart = "RaidTick-";

    public RaidListFile(string fileName)
    {
        FileName = fileName;
        CharacterNames = [];

        FileInfo raidListFileInfo = new(fileName);
        fileName = raidListFileInfo.Name;
        string raidListFileTimeStamp = fileName[RaidListFileNameStart.Length..^4];
        FileDateTime = DateTime.ParseExact(raidListFileTimeStamp, Constants.RaidListFileNameTimeFormat, CultureInfo.InvariantCulture);
    }

    public List<string> CharacterNames { get; private set; }

    public DateTime FileDateTime { get; }

    public string FileName { get; private set; }

    private string DebugDisplay
        => $"{FileName} {CharacterNames.Count}";
}

