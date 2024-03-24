// -----------------------------------------------------------------------
// LogParseResults.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class LogParseResults
{
    public LogParseResults(IList<EqLogFile> eqLogFiles, IList<RaidDumpFile> raidDumpFiles, IList<RaidListFile> raidListFiles)
    {
        EqLogFiles = eqLogFiles;
        RaidDumpFiles = raidDumpFiles;
        RaidListFiles = raidListFiles;
    }

    public IList<EqLogFile> EqLogFiles { get; }

    public IList<RaidDumpFile> RaidDumpFiles { get; }

    public IList<RaidListFile> RaidListFiles { get; }
}
