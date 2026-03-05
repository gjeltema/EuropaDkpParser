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
    ///     Logs a debug message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Debug(string message)
        => Log(LogLevel.Debug, message);

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Error(string message)
        => Log(LogLevel.Error, message);

    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Info(string message)
        => Log(LogLevel.Info, message);

    /// <summary>
    ///     Logs a trace message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Trace(string message)
        => Log(LogLevel.Trace, message);

    /// <summary>
    ///     Logs a warning message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Warning(string message)
        => Log(LogLevel.Warning, message);

    /// <summary>Writes a message to the log.</summary>
    /// <param name="level">Level at which to log.</param>
    /// <param name="message">The message to be logged.</param>
    void Log(LogLevel level, string message);
}
