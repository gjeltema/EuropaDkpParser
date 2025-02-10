// -----------------------------------------------------------------------
// BackgroundLogTarget.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// Wraps an <see cref="ILogTarget"/> and spawns a background thread to write to it. 
/// Incoming messages are placed in a thread-safe queue and dequeued by the background thread and 
/// sent to the passed-in <see cref="ILogTarget" />.
/// </summary>
public sealed class BackgroundLogTarget : ILogTarget, IDisposable
{
    /// <summary>
    /// Raised whenever an exception is thrown by the wrapped LogTarget when logging a message.
    /// </summary>
    public event EventHandler<BackgroundLogErrorEventArgs> ErrorLoggingMessage;

    private const int Timeout = 250;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BlockingCollection<LogInformation> MessageQueue = new(3000);
    private Thread _loggingThread;
    private ILogTarget _wrappedLogTarget;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="wrappedTarget">The log target to be invoked asynchronously.</param>
    public BackgroundLogTarget(ILogTarget wrappedTarget)
    {
        _wrappedLogTarget = wrappedTarget ?? throw new NullReferenceException(nameof(wrappedTarget) + " cannot be null.");

        _loggingThread = new(() => ProcessLogMessage(_cancellationTokenSource.Token));
        _loggingThread.IsBackground = true;
        _loggingThread.Start();
    }

    /// <inheritdoc/>
    public LogLevel LoggingLevel
    {
        get => _wrappedLogTarget.LoggingLevel;
        set => _wrappedLogTarget.LoggingLevel = value;
    }

    /// <inheritdoc/>
    public void Dispose()
        => Dispose(true);

    /// <inheritdoc/>
    public void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopProcessing();
        }
    }

    /// <inheritdoc/>
    public void Log(LogLevel level, string message)
    {
        if (LoggingLevel.IsLevelLoggable(level))
            AddToQueue(level, message);
    }

    /// <summary>
    /// Stop processing log messages on the background thread.<br/>
    /// Do not use this instance anymore after calling this.
    /// </summary>
    public void StopProcessing()
    {
        _cancellationTokenSource.Cancel();
        _loggingThread = null;
        if (_wrappedLogTarget is IDisposable disposable)
            disposable.Dispose();
        _wrappedLogTarget = null;
    }

    private void AddToQueue(LogLevel level, string message)
    {
        var logInfo = new LogInformation(level, message);
        MessageQueue.Add(logInfo);
    }

    private void ProcessLogMessage(CancellationToken cancelToken)
    {
        while (!cancelToken.IsCancellationRequested)
        {
            if (MessageQueue.TryTake(out LogInformation loggingInfo, Timeout, cancelToken))
            {
                try
                {
                    _wrappedLogTarget.Log(loggingInfo.LoggingLevel, loggingInfo.Message);
                }
                catch (Exception e)
                {
                    RaiseErrorLoggingMessageEvent(e);
                }
            }
        }
    }

    private void RaiseErrorLoggingMessageEvent(Exception errorException)
        => ErrorLoggingMessage?.Invoke(this, new BackgroundLogErrorEventArgs(errorException));

    private sealed class LogInformation
    {
        internal LogInformation(LogLevel level, string message)
        {
            LoggingLevel = level;
            Message = message;
        }

        internal LogLevel LoggingLevel { get; private set; }

        internal string Message { get; private set; }
    }
}

/// <summary>
/// Error event args for the <see cref="BackgroundLogTarget"/>.
/// </summary>
public sealed class BackgroundLogErrorEventArgs : EventArgs
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logException">The error exception.</param>
    public BackgroundLogErrorEventArgs(Exception logException)
    {
        ErrorException = logException;
    }

    /// <summary>
    /// The exception raised by the internal calls.
    /// </summary>
    public Exception ErrorException { get; }
}
