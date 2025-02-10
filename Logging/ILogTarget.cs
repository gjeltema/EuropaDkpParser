// -----------------------------------------------------------------------
// ILogTarget.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

/// <summary>
/// Interface that describes a logging target.<br/>
/// </summary>
public interface ILogTarget
{
    /// <summary>Gets or sets the level of logs to be added to the log output.</summary>
    LogLevel LoggingLevel { get; set; }

    /// <summary>Writes a message to the log.</summary>
    /// <param name="level">Level at which to log.</param>
    /// <param name="message">The message to be logged.</param>
    void Log(LogLevel level, string message);
}
