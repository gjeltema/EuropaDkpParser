// -----------------------------------------------------------------------
// RaidParticipationFilesParser.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class RaidParticipationFilesParser
{
    public IList<RaidDumpFile> GetParsedRelevantRaidDumpFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        List<RaidDumpFile> relevantRaidDumpFiles = GetRelevantRaidDumpFiles(sourceDirectory, startTime, endTime).ToList();
        foreach (RaidDumpFile raidDumpFile in relevantRaidDumpFiles)
        {
            ParseRaidDump(raidDumpFile);
        }

        return relevantRaidDumpFiles;
    }

    public IList<RaidListFile> GetParsedRelevantRaidListFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        List<RaidListFile> relevantRaidListFiles = GetRelevantRaidListFiles(sourceDirectory, startTime, endTime).ToList();
        foreach (RaidListFile raidListFile in relevantRaidListFiles)
        {
            ParseRaidList(raidListFile);
        }

        return relevantRaidListFiles;
    }

    public IList<ZealRaidAttendanceFile> GetParsedZealRaidAttendanceFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        List<ZealRaidAttendanceFile> relevantZealAttendanceFiles = GetRelevantZealRaidAttendanceFiles(sourceDirectory, startTime, endTime).ToList();
        foreach (ZealRaidAttendanceFile zealAttendanceFile in relevantZealAttendanceFiles)
        {
            ParseZealAttendance(zealAttendanceFile);
        }

        return relevantZealAttendanceFiles.Where(x => x.CharacterNames.Count > 0).ToList();
    }

    public IEnumerable<RaidDumpFile> GetRelevantRaidDumpFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.RaidDumpFileNameStart + "*.txt";
        return Directory.EnumerateFiles(sourceDirectory, fileNameSearchString)
            .Select(RaidDumpFile.CreateRaidDumpFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    public IEnumerable<RaidListFile> GetRelevantRaidListFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.RaidListFileNameStart + "*.txt";
        return Directory.EnumerateFiles(sourceDirectory, fileNameSearchString)
            .Select(RaidListFile.CreateRaidListFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    public IEnumerable<ZealRaidAttendanceFile> GetRelevantZealRaidAttendanceFiles(string sourceDirectory, DateTime startTime, DateTime endTime)
    {
        string fileNameSearchString = Constants.ZealAttendanceBasedFileName + "*.txt";
        return Directory.EnumerateFiles(sourceDirectory, fileNameSearchString)
            .Select(ZealRaidAttendanceFile.CreateZealRaidAttendanceFile)
            .Where(x => startTime <= x.FileDateTime && x.FileDateTime <= endTime);
    }

    private void ParseRaidDump(RaidDumpFile dumpFile)
    {
        string[] contents = File.ReadAllLines(dumpFile.FullFilePath);
        dumpFile.ParseContents(contents);
    }

    private void ParseRaidList(RaidListFile raidListFile)
    {
        string[] contents = File.ReadAllLines(raidListFile.FullFilePath);
        raidListFile.ParseContents(contents);
    }

    private void ParseZealAttendance(ZealRaidAttendanceFile zealAttendanceFile)
    {
        string[] fileContents = File.ReadAllLines(zealAttendanceFile.FullFilePath);
        if (fileContents.Length < 2)
            return;

        zealAttendanceFile.ParseZealAttendance(fileContents);
    }
}
