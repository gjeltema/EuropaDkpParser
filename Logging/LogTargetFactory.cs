// -----------------------------------------------------------------------
// LogTargetFactory.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System.IO;

/// <summary>
/// Factory for creating log targets provided by this library.
/// </summary>
public sealed class LogTargetFactory : ILogTargetFactory
{
    /// <inheritdoc/>
    public ILogTarget CreateAsyncSimpleLogTarget(string logFile, LogFormatter logFormatter = null)
    {
        var simpleLogTarget = new SimpleLogTarget(logFile, logFormatter);
        return new BackgroundLogTarget(simpleLogTarget);
    }

    /// <inheritdoc/>
    public ILogTarget CreateAsyncSimpleLogTarget(TextWriter logTextTextWriter, LogFormatter logFormatter = null)
    {
        var simpleLogTarget = new SimpleLogTarget(logTextTextWriter, logFormatter);
        return new BackgroundLogTarget(simpleLogTarget);
    }

    /// <inheritdoc/>
    public ILogTarget CreateCompositeLogTarget(params ILogTarget[] logTargets)
        => new CompositeLogTarget(logTargets);

    /// <inheritdoc/>
    public ILogTarget CreateDebugWriterLogTarget()
        => new DebugWriterLogTarget();
}

/// <summary>
/// Factory for creating log targets provided by this library.
/// </summary>
public interface ILogTargetFactory
{
    /// <summary>
    /// Creates a <see cref="SimpleLogTarget" /> wrapped by a <see cref="BackgroundLogTarget" />.
    /// </summary>
    /// <param name="logFile">The full file path, including file name, to write the log entries to.</param>
    /// <param name="logFormatter">The formatting delegate to apply to the log messages.  If one is not supplied, a default delegate is used.<br/>
    /// The default delegate outputs the log message in the following format: yyyy-MM-dd HH:mm:ss:ffff [LOGLEVEL] message<br/>
    /// e.g.: 2020-09-10 08:15:23:1234  [INFO]    The application started.</param>
    ILogTarget CreateAsyncSimpleLogTarget(string logFile, LogFormatter logFormatter = null);

    /// <summary>
    /// Creates a <see cref="SimpleLogTarget" /> wrapped by a <see cref="BackgroundLogTarget"/>.
    /// </summary>
    /// <param name="logTextTextWriter">The <see cref="TextWriter"/> to be used to write the log entries to..</param>
    /// <param name="logFormatter">The formatting delegate to apply to the log messages.  If one is not supplied, a default delegate is used.<br/>
    /// The default delegate outputs the log message in the following format: yyyy-MM-dd HH:mm:ss:ffff [LOGLEVEL] message<br/>
    /// e.g.: 2020-09-10 08:15:23:1234  [INFO]    The application started.</param>
    ILogTarget CreateAsyncSimpleLogTarget(TextWriter logTextTextWriter, LogFormatter logFormatter = null);

    /// <summary>
    /// Creates a <see cref="CompositeLogTarget"/> using the passed in <see cref="ILogTarget"/>s.
    /// </summary>
    /// <param name="logTargets">The <see cref="ILogTarget"/>s to be wrapped by the <see cref="CompositeLogTarget"/>.</param>
    ILogTarget CreateCompositeLogTarget(params ILogTarget[] logTargets);

    /// <summary>
    /// Creates a <see cref="DebugWriterLogTarget"/>.
    /// </summary>
    ILogTarget CreateDebugWriterLogTarget();
}
