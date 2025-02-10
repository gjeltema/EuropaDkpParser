// -----------------------------------------------------------------------
// LoggerExtensions.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System;
using System.Text;

/// <summary>
/// Extension methods for the logging abstractions.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    ///     Logs a critical error message.
    /// </summary>
    /// <param name="logTarget">The target to send the log to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Critical(this ILogTarget logTarget, string message)
        => logTarget.Log(LogLevel.Critical, message);

    /// <summary>
    ///     Logs a debug message.
    /// </summary>
    /// <param name="logTarget">The target to send the log to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Debug(this ILogTarget logTarget, string message)
        => logTarget.Log(LogLevel.Debug, message);

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="logTarget">The target to send the log to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Error(this ILogTarget logTarget, string message)
        => logTarget.Log(LogLevel.Error, message);

    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="logTarget">The target to send the log to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Info(this ILogTarget logTarget, string message)
        => logTarget.Log(LogLevel.Info, message);

    /// <summary>
    ///     Compares the set level to the message's logging level to determine if the log message should be logged.
    /// </summary>
    /// <param name="setLevel">The logging level set for filtering messages sent to the log.</param>
    /// <param name="messageLevel">The logging level for the current message to be logged.</param>
    /// <returns>True if the logging level is in the range set to be logged, False if not.</returns>
    public static bool IsLevelLoggable(this LogLevel setLevel, LogLevel messageLevel)
        => setLevel >= messageLevel;

    /// <summary>
    ///     Creates a log message out of the AggregateException data, including stack trace and contained exceptions and the inner exceptions of those contained exceptions.<br/>
    ///     Note: All contained exceptions will be added, but the number of inner exceptions of those contained exceptions can be limited by the innerExceptionLimit argument.
    /// </summary>
    /// <param name="aex">The <see cref="AggregateException"/> to be used to create the log message.</param>
    /// <param name="innerExceptionLimit">Maximum number of inner exceptions data of the contained exceptions to include in the message.</param>
    public static string ToLogMessage(this AggregateException aex, int innerExceptionLimit = 20)
    {
        var logMessage = new StringBuilder();
        logMessage.AppendLine("AggregateException error message: " + aex.Message);
        logMessage.Append("STACK: ").AppendLine(aex.StackTrace);
        logMessage.AppendLine("Contained exceptions of the AggregateException:");
        logMessage.AppendLine("==================================");

        int innerExceptionCounter = 1;
        foreach (Exception innerException in aex.InnerExceptions)
        {
            logMessage.Append("Contained exception ").Append(innerExceptionCounter).AppendLine(" of the AggregateException:");
            string innerExceptionMessage = innerException.ToLogMessage(innerExceptionLimit);
            logMessage.AppendLine(innerExceptionMessage).AppendLine("==================================");
            innerExceptionCounter++;
        }

        logMessage.Append("End of contained exceptions of AggregateException.");
        return logMessage.ToString();
    }

    /// <summary>
    ///     Creates a log message out of the Exception data, including stack trace and inner exceptions.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to be used to create the log message.</param>
    /// <param name="innerExceptionLimit">Maximum number of inner exceptions data to include in the message.</param>
    public static string ToLogMessage(this Exception ex, int innerExceptionLimit = 20)
    {
        if (ex is AggregateException aex)
        {
            return aex.ToLogMessage();
        }

        var logMessage = new StringBuilder();
        logMessage.Append(ex.GetType()).Append(": ");
        logMessage.Append(ex.Message);
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            logMessage.AppendLine().Append("STACK: ").Append(ex.StackTrace);
        }

        Exception innerException = ex.InnerException;
        int innerExceptionCounter = 0;
        while (innerException != null && innerExceptionCounter < innerExceptionLimit)
        {
            logMessage.AppendLine().Append("INNER EXCEPTION: ").Append(innerException.GetType()).Append(": ");
            logMessage.Append(innerException.Message);
            logMessage.AppendLine().Append("STACK: ").Append(innerException.StackTrace);
            innerException = innerException.InnerException;
            innerExceptionCounter++;
        }

        if (innerException != null)
        {
            logMessage.AppendLine().Append("NOTE: There are more inner exceptions than the specified limit of ").Append(innerExceptionLimit)
                .Append(" inner exceptions, which are not output here.");
        }

        return logMessage.ToString();
    }

    /// <summary>
    ///     Logs a trace message.
    /// </summary>
    /// <param name="logTarget">The target to send the log to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Trace(this ILogTarget logTarget, string message)
        => logTarget.Log(LogLevel.Trace, message);

    /// <summary>
    ///     Logs a warning message.
    /// </summary>
    /// <param name="logTarget">The target to send the log to.</param>
    /// <param name="message">The message to be logged.</param>
    public static void Warning(this ILogTarget logTarget, string message)
        => logTarget.Log(LogLevel.Warning, message);
}
