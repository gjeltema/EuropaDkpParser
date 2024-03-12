// -----------------------------------------------------------------------
// LogResultsAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class LogResultsAnalyzer : ILogResultsAnalyzer
{
    public IPotentialLogErrors PotentialErrors { get; }

    public void AnalyzeResults(ILogParseResults results)
    {

    }
}

public interface ILogResultsAnalyzer
{
    IPotentialLogErrors PotentialErrors { get; }

    void AnalyzeResults(ILogParseResults results);
}
