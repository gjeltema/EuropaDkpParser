// -----------------------------------------------------------------------
// Enums.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

/// <summary>
///     Specifies the level of the message to log.
/// </summary>
public enum LogLevel
{
    /// <summary>
    ///     Errors that are fatal for the application.
    /// </summary>
    Critical = 1,

    /// <summary>
    ///     Run-time errors and unexpected conditions.
    /// </summary>
    Error = 2,

    /// <summary>
    ///     Unusual conditions and situations to be aware of.
    /// </summary>
    Warning = 3,

    /// <summary>
    ///     Informational messages.
    /// </summary>
    Info = 4,

    /// <summary>
    ///     Information useful for debugging.
    /// </summary>
    Debug = 5,

    /// <summary>
    ///     Extra detailed messages.
    /// </summary>
    Trace = 6
}
