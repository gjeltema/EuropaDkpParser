// -----------------------------------------------------------------------
// LogParseProcessor.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class LogParseProcessor : ILogParseProcessor
{
    public Task<ILogParseResults> ParseLogs(IDkpParserSettings settings, DateTime startTime, DateTime endTime)
    {


        return null;
    }
}

public interface ILogParseProcessor
{
    Task<ILogParseResults> ParseLogs(IDkpParserSettings settings, DateTime startTime, DateTime endTime);
}


public interface ILogParseResults
{

}
