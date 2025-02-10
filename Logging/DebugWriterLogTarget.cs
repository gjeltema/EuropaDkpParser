// -----------------------------------------------------------------------
// DebugWriterLogTarget.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System;
using System.Diagnostics;

/// <summary>
/// Writes log entries to <c>Debug.WriteLine()</c>.
/// </summary>
public sealed class DebugWriterLogTarget : ILogTarget
{
    /// <inheritdoc/>
    public LogLevel LoggingLevel { get; set; } = LogLevel.Info;

    /// <inheritdoc/>
    public void Log(LogLevel level, string message)
    {
        if (LoggingLevel.IsLevelLoggable(level))
            Debug.WriteLine(CreateDisplayMessage(level, message));
    }

    private static string CreateDisplayMessage(LogLevel level, string message)
        => $"{DateTime.Now:HH:mm:ss:fff} {level} {message}";
}
