// -----------------------------------------------------------------------
// EqLogParserBase.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

using System.Globalization;
using System.IO;

public abstract class EqLogParserBase : IEqLogParser
{
    private IParseEntry _currentEntryParser;

    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        // This essentially does a half-assed state machine, switching around among other 'EntryParsers',
        // having the parser calls be polymorphic to avoid 'if-hell'.  Could/should just create an actual
        // state machine controller and inject that into the parsers, but this works well enough and
        // is pretty easy to follow.

        EqLogFile logFile = new() { LogFile = filename };

        InitializeEntryParsers(logFile, startTime, endTime);

        foreach (string logLine in File.ReadLines(filename))
        {
            if (!TryExtractEqLogTimeStamp(logLine, out DateTime entryTimeStamp))
                continue;

            if (entryTimeStamp > endTime)
                break;

            _currentEntryParser.ParseEntry(logLine, entryTimeStamp);
        }

        return logFile;
    }

    public void SetEntryParser(IParseEntry parseEntry)
        => _currentEntryParser = parseEntry;

    internal static bool TryExtractEqLogTimeStamp(string logLine, out DateTime result)
    {
        // [Wed Feb 21 16:34:07 2024] ...

        if (logLine.Length < Constants.LogDateTimeLength)
        {
            result = DateTime.MinValue;
            return false;
        }

        ReadOnlySpan<char> timeEntry = logLine.AsSpan()[0..Constants.LogDateTimeLength];
        return DateTime.TryParseExact(timeEntry, Constants.EqLogDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    protected ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime, IEnumerable<string> selectedLogFileNames)
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
