﻿// -----------------------------------------------------------------------
// Exceptions.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class EuropaDkpParserException : Exception
{
    public EuropaDkpParserException() : base() { }

    public EuropaDkpParserException(string message) : base(message) { }

    public EuropaDkpParserException(string message, string logLine)
        : base(message)
    {
        LogLine = logLine;
    }

    public EuropaDkpParserException(string message, string logLine, Exception innerException)
        : base(message, innerException)
    {
        LogLine = logLine;
    }

    public string LogLine { get; } = string.Empty;
}
