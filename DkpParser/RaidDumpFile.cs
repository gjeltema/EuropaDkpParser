// -----------------------------------------------------------------------
// RaidDumpFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class RaidDumpFile
{
    public const string RaidDumpFileNameStart = "RaidRoster-";

    internal RaidDumpFile(string fileName)
    {
        FileName = fileName;
        CharacterNames = [];

        string dumpFileWithoutExtension = fileName[0..^4];
        string[] dateTimeComponents = dumpFileWithoutExtension.Split('-');
        FileDateTime = DateTime.Parse($"{dateTimeComponents[1]}T{dateTimeComponents[2]}");
    }

    internal DateTime FileDateTime { get; }
    internal string FileName { get; private set; }

    internal List<string> CharacterNames { get; private set; }
}
