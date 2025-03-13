// -----------------------------------------------------------------------
// Log.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Primary entry point for the logging abstractions.
/// </summary>
public static class Log
{
    private const string CriticalText = "CRITICAL";
    private const string DebugText = "DEBUG";
    private const string ErrorText = "ERROR";
    private const string InfoText = "INFO";
    private const string TraceText = "TRACE";
    private const string WarningText = "WARNING";
    private static readonly Dictionary<LogLevel, string> LogLevelToTextMapping = new Dictionary<LogLevel, string>
    {
        { LogLevel.Critical, CriticalText },
        { LogLevel.Error, ErrorText },
        { LogLevel.Warning , WarningText },
        { LogLevel.Info , InfoText },
        { LogLevel.Debug , DebugText },
        { LogLevel.Trace , TraceText },
    };
    private static readonly Dictionary<string, LogLevel> TextToLogLevelMapping = new Dictionary<string, LogLevel>
    {
        { CriticalText, LogLevel.Critical },
        { ErrorText, LogLevel.Error },
        { WarningText, LogLevel.Warning },
        { InfoText, LogLevel.Info },
        { DebugText, LogLevel.Debug },
        { TraceText, LogLevel.Trace },
    };

    static Log()
    {
        Logger = new CollectionLogger();
        LogTargetFactory = new LogTargetFactory();
    }

    /// <summary>Gets or sets the logger to use.</summary>
    public static ILogger Logger { get; set; }

    /// <summary>Gets or sets a factory to create LogTargets.  By default, provides a factory to create LogTargets provided by this library.</summary>
    public static ILogTargetFactory LogTargetFactory { get; set; }

    /// <summary>
    /// Converts the string into the associated <see cref="LogLevel" />, or the <paramref name="defaultLogLevel" /> if there is no matching level.
    /// </summary>
    /// <param name="logLevelText">The string name of the <see cref="LogLevel" /> to be converted.</param>
    /// <param name="defaultLogLevel">The <see cref="LogLevel" /> to return if the <paramref name="logLevelText" /> does not map to a <see cref="LogLevel" /> value.</param>
    public static LogLevel ConvertToLogLevel(string logLevelText, LogLevel defaultLogLevel = LogLevel.Info)
    {
        if (string.IsNullOrWhiteSpace(logLevelText))
            return defaultLogLevel;

        string logLevelUpperText = logLevelText.ToUpper();

        if (TextToLogLevelMapping.TryGetValue(logLevelUpperText, out LogLevel mappedLogLevel))
            return mappedLogLevel;

        return defaultLogLevel;
    }

    /// <summary>
    ///     Logs a critical error to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Critical(string message)
        => Logger.Default.Critical(message);

    /// <summary>
    ///     Logs a critical error to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Critical(LoggerCriticalInterpolator message)
    {
        if (message.CreatedLogMessage)
            Logger.Default.Log(LogLevel.Critical, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a critical error to the default log target.
    /// </summary>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to apply this extension method to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Critical(this ILogTarget logTarget, [InterpolatedStringHandlerArgument("logTarget")] LoggerCriticalInterpolator message)
    {
        if (message.CreatedLogMessage)
            logTarget.Log(LogLevel.Critical, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a debug message to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Debug(string message)
        => Logger.Default.Debug(message);

    /// <summary>
    ///     Logs a debug message to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Debug(LoggerDebugInterpolator message)
    {
        if (message.CreatedLogMessage)
            Logger.Default.Log(LogLevel.Debug, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a debug message to the default log target.
    /// </summary>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to apply this extension method to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Debug(this ILogTarget logTarget, [InterpolatedStringHandlerArgument("logTarget")] LoggerDebugInterpolator message)
    {
        if (message.CreatedLogMessage)
            logTarget.Log(LogLevel.Debug, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs an error to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Error(string message)
        => Logger.Default.Error(message);

    /// <summary>
    ///     Logs an error to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Error(LoggerErrorInterpolator message)
    {
        if (message.CreatedLogMessage)
            Logger.Default.Log(LogLevel.Error, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs an error to the default log target.
    /// </summary>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to apply this extension method to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Error(this ILogTarget logTarget, [InterpolatedStringHandlerArgument("logTarget")] LoggerErrorInterpolator message)
    {
        if (message.CreatedLogMessage)
            logTarget.Log(LogLevel.Error, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs an informational message to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Info(string message)
        => Logger.Default.Info(message);

    /// <summary>
    ///     Logs an informational message to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Info(LoggerInfoInterpolator message)
    {
        if (message.CreatedLogMessage)
            Logger.Default.Log(LogLevel.Info, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs an informational message to the default log target.
    /// </summary>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to apply this extension method to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Info(this ILogTarget logTarget, [InterpolatedStringHandlerArgument("logTarget")] LoggerInfoInterpolator message)
    {
        if (message.CreatedLogMessage)
            logTarget.Log(LogLevel.Info, message.ToStringAndClear());
    }

    public static string ToUpperString(this LogLevel logLevel)
        => LogLevelToTextMapping[logLevel];

    /// <summary>
    ///     Logs a trace message to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Trace(string message)
        => Logger.Default.Trace(message);

    /// <summary>
    ///     Logs a trace message to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Trace(LoggerTraceInterpolator message)
    {
        if (message.CreatedLogMessage)
            Logger.Default.Log(LogLevel.Trace, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a trace message to the default log target.
    /// </summary>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to apply this extension method to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Trace(this ILogTarget logTarget, [InterpolatedStringHandlerArgument("logTarget")] LoggerTraceInterpolator message)
    {
        if (message.CreatedLogMessage)
            logTarget.Log(LogLevel.Trace, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a warning to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Warning(string message)
        => Logger.Default.Warning(message);

    /// <summary>
    ///     Logs a warning to the default log target.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public static void Warning(LoggerWarningInterpolator message)
    {
        if (message.CreatedLogMessage)
            Logger.Default.Log(LogLevel.Warning, message.ToStringAndClear());
    }

    /// <summary>
    ///     Logs a warning to the default log target.
    /// </summary>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to apply this extension method to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Warning(this ILogTarget logTarget, [InterpolatedStringHandlerArgument("logTarget")] LoggerWarningInterpolator message)
    {
        if (message.CreatedLogMessage)
            logTarget.Log(LogLevel.Warning, message.ToStringAndClear());
    }
}
