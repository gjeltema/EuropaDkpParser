// -----------------------------------------------------------------------
// CollectionsLogger.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// An ILogger that is a collection of <see cref="ILogTarget"/>s.
/// </summary>
public sealed class CollectionLogger : ILogger
{
    private readonly ConcurrentDictionary<string, ILogTarget> Loggers = new();
    private ILogTarget defaultLogTarget;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CollectionLogger()
    {
        defaultLogTarget = new EmptyLogTarget();
        this["Default"] = defaultLogTarget;
    }

    /// <inheritdoc/>
    public ILogTarget this[string name]
    {
        get
        {
            if (Loggers.TryGetValue(name, out ILogTarget logTarget))
            {
                return logTarget;
            }

            return Default;
        }
        set
        {
            if (value == null)
                return;
            if (string.IsNullOrWhiteSpace(name))
                return;
            Loggers[name] = value;
        }
    }

    /// <inheritdoc/>
    public ILogTarget Default
    {
        get => defaultLogTarget;
        set
        {
            if (value == null)
                return;
            defaultLogTarget = value;
            this["Default"] = defaultLogTarget;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, ILogTarget> AllLogTargets()
        => new ReadOnlyDictionary<string, ILogTarget>(Loggers);

    /// <inheritdoc/>
    public bool RemoveLogTarget(string logTargetName)
        => Loggers.TryRemove(logTargetName, out ILogTarget _);
}
