// -----------------------------------------------------------------------
// RaidDumpFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.Globalization;
using System.IO;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class RaidDumpFile
{
    public RaidDumpFile(string fileName)
    {
        FileName = fileName;
        Characters = [];

        FileInfo dumpFileInfo = new(fileName);
        fileName = dumpFileInfo.Name;

        // RaidRoster-20240312-161830.txt
        string dumpFileTimeStamp = fileName[Constants.RaidDumpFileNameStart.Length..^4];
        FileDateTime = DateTime.ParseExact(dumpFileTimeStamp, Constants.RaidDumpFileNameTimeFormat, CultureInfo.InvariantCulture);
    }

    public ICollection<PlayerCharacter> Characters { get; private set; }

    public DateTime FileDateTime { get; }

    public string FileName { get; private set; }

    private string DebugDisplay
        => $"{FileDateTime:HH:mm:ss} {Characters.Count}";
}
