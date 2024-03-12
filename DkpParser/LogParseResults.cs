﻿// -----------------------------------------------------------------------
// LogParseResults.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class LogParseResults
{
    public LogParseResults(IList<EqLogFile> eqLogFiles, IList<RaidDumpFile> raidDumpFiles)
    {
        EqLogFiles = eqLogFiles;
        RaidDumpFiles = raidDumpFiles;
    }

    public IList<RaidDumpFile> RaidDumpFiles { get; }
    public IList<EqLogFile> EqLogFiles { get; }
}