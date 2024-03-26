// -----------------------------------------------------------------------
// EqLogParserBase.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public abstract class EqLogParserBase : IEqLogParser
{
    private IParseEntry _currentEntryParser;

    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        InitializeEntryParsers(logFile, startTime, endTime);

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!logLine.ExtractEqLogTimeStamp(out DateTime entryTimeStamp))
            {
                continue;
            }

            if (entryTimeStamp > endTime)
                break;

            _currentEntryParser.ParseEntry(logLine, entryTimeStamp);
        }

        return logFile;
    }

    public void SetEntryParser(IParseEntry parseEntry)
        => _currentEntryParser = parseEntry;

    protected ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime, ICollection<string> selectedLogFileNames)
    {
        List<EqLogFile> logFiles = [];
        foreach (string logFileName in selectedLogFileNames)
        {
            EqLogFile parsedFile = ParseLogFile(logFileName, startTime, endTime);
            if (parsedFile.LogEntries.Count > 0)
                logFiles.Add(parsedFile);
        }

        return logFiles;
    }

    protected abstract void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime);
}
