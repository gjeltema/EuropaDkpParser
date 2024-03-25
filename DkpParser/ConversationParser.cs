namespace DkpParser;

using System;
using System.Globalization;
using System.IO;

public sealed class ConversationParser : IConversationParser, ISetParser
{
    private readonly IDkpParserSettings _settings;
    private IParseEntry _currentEntryParser;

    public ConversationParser(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
    {
        List<EqLogFile> logFiles = [];
        foreach (string logFileName in _settings.SelectedLogFiles)
        {
            EqLogFile parsedFile = ParseLogFile(logFileName, startTime, endTime);
            if (parsedFile.LogEntries.Count > 0)
                logFiles.Add(parsedFile);
        }

        return logFiles;
    }

    public EqLogFile ParseLogFile(string filename, DateTime startTime, DateTime endTime)
    {
        EqLogFile logFile = new() { LogFile = filename };

        //LogEverythingParser logEverything = new(logFile);
        //FindStartTimeParser findStartParser = new(this, startTime, logEverything);

        //SetParser(findStartParser);

        //foreach (string logLine in File.ReadLines(filename))
        //{
        //    if (!GetTimeStamp(logLine, out DateTime entryTimeStamp))
        //    {
        //        continue;
        //    }

        //    if (entryTimeStamp > endTime)
        //        break;

        //    _currentEntryParser.ParseEntry(logLine, entryTimeStamp);
        //}

        return logFile;
    }

    private bool GetTimeStamp(string logLine, out DateTime result)
    {
        if (logLine.Length < Constants.TypicalTimestamp.Length || string.IsNullOrWhiteSpace(logLine))
        {
            result = DateTime.MinValue;
            return false;
        }

        string timeEntry = logLine[1..25];
        return DateTime.TryParseExact(timeEntry, Constants.LogDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    public void SetParser(IParseEntry parseEntry)
        => _currentEntryParser = parseEntry;
}

public interface IConversationParser : ILogParser
{

}
