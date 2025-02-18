// -----------------------------------------------------------------------
// SimpleLogTarget.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System;
using System.IO;

/// <summary>
/// Delegate that is used by the <see cref="SimpleLogTarget"/> to format the output log message.
/// </summary>
/// <param name="logLevel">The level of logging message to output.</param>
/// <param name="message">The message to be output to the log file, pre-formatted.</param>
/// <returns></returns>
public delegate string LogFormatter(LogLevel logLevel, string message);

/// <summary>
///     A convenient default implementation of <see cref="ILogTarget"/> for writing simple log message to a specified file.
/// </summary>
public sealed class SimpleLogTarget : ILogTarget, IDisposable
{
    private static readonly LogFormatter DefaultLogFormatter =
         (logLevel, message) => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:ffff}\t{logLevel.ToString().ToUpper()}\t{message}";
    private readonly LogFormatter LogFormatting;
    private TextWriter _logTextWriter;

    /// <summary>
    /// Creates a <see cref="SimpleLogTarget"/>.
    /// </summary>
    /// <param name="logFile">The full file path, including file name, to write the log entries to.</param>
    /// <param name="logFormatter">The formatting delegate to apply to the log messages.  If one is not supplied, a default delegate is used.<br/>
    /// The default delegate outputs the log message in the following format: yyyy-MM-dd HH:mm:ss:ffff [LOGLEVEL] message<br/>
    /// e.g.: 2020-09-10 08:15:23:1234  [INFO]    The application started.</param>
    public SimpleLogTarget(string logFile, LogFormatter logFormatter = null)
        : this(OpenLogfile(logFile), logFormatter)
    { }

    /// <summary>
    /// Creates a <see cref="SimpleLogTarget"/>.
    /// </summary>
    /// <param name="logTextTextWriter">The <see cref="TextWriter"/> to be used to write the log entries to..</param>
    /// <param name="logFormatter">The formatting delegate to apply to the log messages.  If one is not supplied, a default delegate is used.<br/>
    /// The default delegate outputs the log message in the following format: yyyy-MM-dd HH:mm:ss:ffff [LOGLEVEL] message<br/>
    /// e.g.: 2020-09-10 08:15:23:1234  [INFO]    The application started.</param>
    public SimpleLogTarget(TextWriter logTextTextWriter, LogFormatter logFormatter = null)
    {
        _logTextWriter = logTextTextWriter ?? throw new ArgumentNullException(nameof(logTextTextWriter));
        LogFormatting = logFormatter ?? DefaultLogFormatter;
    }

    /// <inheritdoc/>
    public LogLevel LoggingLevel { get; set; } = LogLevel.Info;

    /// <inheritdoc/>
    public void Dispose()
        => Dispose(true);

    /// <inheritdoc/>
    public void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logTextWriter?.Dispose();
            _logTextWriter = null;
        }
    }

    /// <inheritdoc/>
    public void Log(LogLevel level, string message)
    {
        if (LoggingLevel.IsLevelLoggable(level))
            WriteToLog(level, message);
    }

    private static StreamWriter OpenLogfile(string logFile)
    {
        if (string.IsNullOrWhiteSpace(logFile))
            throw new ArgumentException("Log file name cannot be null, empty, or only whitespace.", nameof(logFile));
        return File.AppendText(logFile);
    }

    private void WriteToLog(LogLevel logLevel, string message)
    {
        _logTextWriter.WriteLine(LogFormatting(logLevel, message));
        _logTextWriter.Flush();
    }
}
