// -----------------------------------------------------------------------
// RaidDumpFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class RaidDumpFile
{
    public const string RaidDumpFileNameStart = "RaidRoster-";

    public RaidDumpFile(string fileName)
    {
        FileName = fileName;
        CharacterNames = [];

        string dumpFileWithoutExtension = fileName[0..^4];
        string[] dateTimeComponents = dumpFileWithoutExtension.Split('-');
        FileDateTime = DateTime.Parse($"{dateTimeComponents[1]}T{dateTimeComponents[2]}");
    }

    public DateTime FileDateTime { get; }
    public string FileName { get; private set; }

    public List<string> CharacterNames { get; private set; }
}
