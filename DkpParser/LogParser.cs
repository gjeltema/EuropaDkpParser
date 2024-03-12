namespace DkpParser;

using System.IO;

internal sealed class LogParser : ILogParser
{
    private readonly IDkpParserSettings _settings;

    public LogParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    // [Sat Mar 02 13:50:52 2024] Schitshow begins to cast a spell.
    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        string logFilePath = Path.Combine(_settings.EqDirectory, filename);
        EqLogFile logFile = new();

        //string initialLogLine = $"[{startTime:ddd MMM dd HH:}";
        bool foundBeginning = false;

        foreach (string logLine in File.ReadLines(logFilePath))
        {
            if (!GetTimeStamp(logLine, out DateTime entryTimeStamp))
            {
                // Log error?
                continue;
            }

            if (!foundBeginning)
            {
                if (entryTimeStamp >= startTime)
                    foundBeginning = true;
                else
                    continue;
            }

            if (entryTimeStamp > endTime)
                break;
            
            if(logLine.Contains(Constants.PossibleErrorDelimiter))
            {

                if(logLine.Contains(Constants.AttendanceDelimiter))
                {

                }
                else // is possible error delimiter
                {

                }
            }
        }

        return null;
    }

    private bool GetTimeStamp(string logLine, out DateTime result)
    {
        string timeEntry = logLine[1..25];
        return DateTime.TryParse(logLine, out result);
    }
}

public interface ILogParser
{
    EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime);
}
