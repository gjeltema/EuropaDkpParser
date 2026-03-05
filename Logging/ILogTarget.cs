// -----------------------------------------------------------------------
// ILogTarget.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

/// <summary>
/// Interface that describes a logging target.<br/>
/// </summary>
public interface ILogTarget
{
    /// <summary>Gets or sets the level of logs to be added to the log output.</summary>
    LogLevel LoggingLevel { get; set; }

    /// <summary>
    ///     Logs a critical error message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Critical(string message)
        => Log(LogLevel.Critical, message);

    /// <summary>
    ///     Logs a critical error.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Critical(LoggerCriticalInterpolator message)
    {
        if (message.CreatedLogMessage)
            Log(LogLevel.Critical, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a debug message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Debug(string message)
        => Log(LogLevel.Debug, message);

    /// <summary>
    ///     Logs a debug message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Debug(LoggerDebugInterpolator message)
    {
        if (message.CreatedLogMessage)
            Log(LogLevel.Debug, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Error(string message)
        => Log(LogLevel.Error, message);

    /// <summary>
    ///     Logs an error.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Error(LoggerErrorInterpolator message)
    {
        if (message.CreatedLogMessage)
            Log(LogLevel.Error, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Info(string message)
        => Log(LogLevel.Info, message);

    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Info(LoggerInfoInterpolator message)
    {
        if (message.CreatedLogMessage)
            Log(LogLevel.Info, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a trace message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Trace(string message)
        => Log(LogLevel.Trace, message);

    /// <summary>
    ///     Logs a trace message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Trace(LoggerTraceInterpolator message)
    {
        if (message.CreatedLogMessage)
            Log(LogLevel.Trace, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a warning message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Warning(string message)
        => Log(LogLevel.Warning, message);

    /// <summary>
    ///     Logs a warning.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Warning(LoggerWarningInterpolator message)
    {
        if (message.CreatedLogMessage)
            Log(LogLevel.Warning, message.ToStringAndClear());
    }

    /// <summary>Writes a message to the log.</summary>
    /// <param name="level">Level at which to log.</param>
    /// <param name="message">The message to be logged.</param>
    void Log(LogLevel level, string message);
}
