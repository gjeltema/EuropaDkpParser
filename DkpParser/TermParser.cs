﻿// -----------------------------------------------------------------------
// TermParser.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class TermParser : EqLogParserBase, ITermParser
{
    private readonly bool _caseSensitive;
    private readonly string _searchItem;
    private readonly IDkpParserSettings _settings;

    public TermParser(IDkpParserSettings settings, string searchItem, bool caseSensitive)
    {
        _settings = settings;
        _searchItem = searchItem;
        _caseSensitive = caseSensitive;
    }

    public ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime)
        => GetEqLogFiles(startTime, endTime, _settings.SelectedLogFiles);

    protected override void InitializeEntryParsers(EqLogFile logFile, DateTime startTime, DateTime endTime)
    {
        IParseEntry termEntryParser = _caseSensitive
            ? new TermEntryCaseSensitiveParser(logFile, _searchItem)
            : new TermEntryCaseInsensitiveParser(logFile, _searchItem);

        FindStartTimeEntryParser findStartParser = new(this, startTime, termEntryParser);

        SetEntryParser(findStartParser);
    }

    private sealed class TermEntryCaseInsensitiveParser : IParseEntry
    {
        private readonly EqLogFile _logFile;
        private readonly string _searchTerm;

        public TermEntryCaseInsensitiveParser(EqLogFile logFile, string searchTerm)
        {
            _logFile = logFile;
            _searchTerm = searchTerm;
        }

        public void ParseEntry(string logLine, DateTime entryTimeStamp)
        {
            if (!logLine.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                return;

            EqLogEntry logEntry = new()
            {
                EntryType = LogEntryType.Unknown,
                LogLine = logLine,
                Timestamp = entryTimeStamp
            };

            _logFile.LogEntries.Add(logEntry);
        }
    }

    private sealed class TermEntryCaseSensitiveParser : IParseEntry
    {
        private readonly EqLogFile _logFile;
        private readonly string _searchTerm;

        public TermEntryCaseSensitiveParser(EqLogFile logFile, string searchTerm)
        {
            _logFile = logFile;
            _searchTerm = searchTerm;
        }

        public void ParseEntry(string logLine, DateTime entryTimeStamp)
        {
            if (!logLine.Contains(_searchTerm))
                return;

            EqLogEntry logEntry = new()
            {
                EntryType = LogEntryType.Unknown,
                LogLine = logLine,
                Timestamp = entryTimeStamp
            };

            _logFile.LogEntries.Add(logEntry);
        }
    }
}

public interface ITermParser : IEqLogParser
{
    ICollection<EqLogFile> GetEqLogFiles(DateTime startTime, DateTime endTime);
}
