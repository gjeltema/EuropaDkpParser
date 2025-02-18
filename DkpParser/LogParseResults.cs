// -----------------------------------------------------------------------
// LogParseResults.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class LogParseResults
{
    public LogParseResults(IList<EqLogFile> eqLogFiles, IList<RaidDumpFile> raidDumpFiles, IList<RaidListFile> raidListFiles, IList<ZealRaidAttendanceFile> zealRaidFiles)
    {
        EqLogFiles = eqLogFiles;
        RaidDumpFiles = raidDumpFiles;
        RaidListFiles = raidListFiles;
        ZealRaidAttendanceFiles = zealRaidFiles;
    }

    public IList<EqLogFile> EqLogFiles { get; }

    public IList<RaidDumpFile> RaidDumpFiles { get; }

    public IList<RaidListFile> RaidListFiles { get; }

    public IList<ZealRaidAttendanceFile> ZealRaidAttendanceFiles { get; }
}
