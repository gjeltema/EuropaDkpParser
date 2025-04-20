// -----------------------------------------------------------------------
// EqLogParserBase.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

using System.IO;
using Gjeltema.Logging;

public abstract class EqLogParserBase : IEqLogParser
{
    private const int BufferSize = 16384;
    private const string LogPrefix = $"[{nameof(EqLogParserBase)}]";
    private const long SearchThreshold = BufferSize * 50;
    private IParseEntry _currentEntryParser;

    public EqLogFile ParseLogFile(string fullFilePath, DateTime startTime, DateTime endTime)
    {
        // This essentially does a half-assed state machine, switching around among other 'EntryParsers',
        // having the parser calls be polymorphic to avoid 'if-hell'.  Could/should just create an actual
        // state machine controller and inject that into the parsers, but this works well enough and
        // is pretty easy to follow.
        ReadOnlySpan<char> newLine = Environment.NewLine.AsSpan();

        char[] fileReadBuffer = new char[BufferSize];
        int remainderLength = 0;

        EqLogFile logFile = new() { LogFile = fullFilePath };

        InitializeEntryParsers(logFile, startTime, endTime);

        using FileStream fileStream = new(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize * 2, FileOptions.SequentialScan);

        bool validFile = InitializeStreamReader(fileStream, startTime, endTime, out StreamReader initializedReader);
        if (!validFile)
            return logFile;

        using StreamReader reader = initializedReader;
        int charsRead = reader.Read(fileReadBuffer, 0, BufferSize);

        while (charsRead > 0)
        {
            ReadOnlySpan<char> linesRead = fileReadBuffer.AsSpan(0, charsRead + remainderLength);
            int currentIndex = 0;
            int indexOfNewline = linesRead.IndexOf(newLine);

            // "Split" the buffer by newline, and process each line.  Skip over empty lines.
            // Place the "remainder" of the buffer (an incomplete entry) at the beginning of the buffer for the next read.
            while (true)
            {
                ReadOnlySpan<char> line = linesRead[currentIndex..indexOfNewline];

                if (TryExtractEqLogTimeStamp(line, out DateTime entryTimeStamp))
                {
                    if (entryTimeStamp > endTime)
                        return logFile;

                    ReadOnlySpan<char> logLineNoTimestamp = line[(Constants.EqLogDateTimeLength + 1)..];

                    _currentEntryParser.ParseEntry(logLineNoTimestamp, entryTimeStamp);
                }

                // Cycle through any empty lines, and either continue with the next full line, or discover it's the end of the buffer
                // and copy the remainder of the buffer to the beginning.
                currentIndex = indexOfNewline + newLine.Length;
                line = linesRead[currentIndex..];

                int localIndexOfNewline = line.IndexOf(newLine);
                if (localIndexOfNewline < 0)
                {
                    remainderLength = line.Length;
                    line.CopyTo(fileReadBuffer);
                    break;
                }

                indexOfNewline = currentIndex + localIndexOfNewline;
            }

            charsRead = reader.Read(fileReadBuffer, remainderLength, BufferSize - remainderLength);
        }
        return logFile;
    }

    public void SetEntryParser(IParseEntry parseEntry)
        => _currentEntryParser = parseEntry;

    internal static bool TryExtractEqLogTimeStamp(ReadOnlySpan<char> logLine, out DateTime result)
    {
        // [Wed Feb 21 16:34:07 2024] ...

        if (logLine.Length < Constants.EqLogDateTimeLength)
        {
            result = DateTime.MinValue;
            return false;
        }

        // Hand rolled since the format is known and doesnt change.
        // This runs in <15% of the time it takes DateTime.TryParseExact to run.
        // Benchmarking also showed that parsing the timestamp from every log line was ~80% of the 
        // time to parse a log file, so this is a substantial real-world runtime improvement.

        try
        {
            int month = GetMonth(logLine);
            int day = (logLine[9] - '0') * 10 + (logLine[10] - '0');
            int year = (logLine[23] - '0') * 10 + (logLine[24] - '0') + 2000;
            int hour = (logLine[12] - '0') * 10 + (logLine[13] - '0');
            int min = (logLine[15] - '0') * 10 + (logLine[16] - '0');
            int sec = (logLine[18] - '0') * 10 + (logLine[19] - '0');

            result = new DateTime(year, month, day, hour, min, sec);
            return true;
        }
        catch
        {
            result = DateTime.MinValue;
            return false;
        }
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

    private static int GetMonth(ReadOnlySpan<char> logLine)
    {
        char firstChar = logLine[5];

        if (firstChar == 'F')
            return 2;
        else if (firstChar == 'S')
            return 9;
        else if (firstChar == 'O')
            return 10;
        else if (firstChar == 'N')
            return 11;
        else if (firstChar == 'D')
            return 12;
        else
        {
            ReadOnlySpan<char> monthPiece = logLine[5..8];
            if (MemoryExtensions.Equals(monthPiece, "Jan", StringComparison.Ordinal))
                return 1;
            else if (MemoryExtensions.Equals(monthPiece, "Mar", StringComparison.Ordinal))
                return 3;
            if (MemoryExtensions.Equals(monthPiece, "Apr", StringComparison.Ordinal))
                return 4;
            else if (MemoryExtensions.Equals(monthPiece, "May", StringComparison.Ordinal))
                return 5;
            else if (MemoryExtensions.Equals(monthPiece, "Jun", StringComparison.Ordinal))
                return 6;
            else if (MemoryExtensions.Equals(monthPiece, "Jul", StringComparison.Ordinal))
                return 7;
            else //(MemoryExtensions.Equals(monthPiece, "Aug", StringComparison.Ordinal))
                return 8;
        }
    }

    private DateTime GetTimestamp(StreamReader reader)
    {
        reader.ReadLine();
        for (int i = 0; i < 8; i++)
        {
            string line = reader.ReadLine();
            if (TryExtractEqLogTimeStamp(line, out DateTime timestamp))
            {
                return timestamp;
            }
        }

        return DateTime.MaxValue;
    }

    private bool InitializeStreamReader(FileStream fileStream, DateTime startTime, DateTime endTime, out StreamReader reader)
    {
        reader = new(fileStream, bufferSize: BufferSize * 2);
        long endBound = fileStream.Length;
        if (endBound < BufferSize * 1000)
            return true;

        // Perform binary search to get "close enough" - doesnt need to be exact.  Just set the fileStream position to a point that is 
        // close to the startTime timestamp, skipping most of a file.  Purely a performance optimization for large files.
        try
        {
            DateTime startTimestamp = GetTimestamp(reader);
            if (startTimestamp > endTime)
                return false;

            long currentPosition = endBound - 1500;
            fileStream.Seek(-1000, SeekOrigin.End);
            reader = new(fileStream, bufferSize: BufferSize * 2);

            DateTime currentTimestamp = GetTimestamp(reader);
            if (currentTimestamp < startTime)
                return false;

            long beginBound = 0;
            long offset;

            while (true)
            {
                if (currentTimestamp < startTime)
                {
                    beginBound = currentPosition;
                    offset = (endBound - currentPosition) / 2;
                }
                else
                {
                    endBound = currentPosition;
                    offset = -((currentPosition - beginBound) / 2);
                }

                currentPosition += offset;

                fileStream.Seek(offset, SeekOrigin.Current);
                reader = new(fileStream, bufferSize: BufferSize * 2);

                currentTimestamp = GetTimestamp(reader);

                if (Math.Abs(offset) < SearchThreshold)
                {
                    if (currentTimestamp < startTime)
                    {
                        return true;
                    }
                    else
                    {
                        fileStream.Seek(beginBound, SeekOrigin.Begin);
                        reader = new(fileStream, bufferSize: BufferSize * 2);

                        reader.ReadLine();
                        return true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
            reader = new(fileStream, bufferSize: BufferSize * 2);

            Log.Warning($"{LogPrefix} Error searching for position to start parsing in file {fileStream.Name}; Start Time: {startTime:yyyy-MM-dd HH:mm:ss}, End Time: {endTime:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{e.ToLogMessage()}");

            return true;
        }
    }
}
