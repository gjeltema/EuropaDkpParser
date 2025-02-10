// -----------------------------------------------------------------------
// CollectionLogger.cs Copyright 2025 Craig Gjeltema
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
    private readonly ConcurrentDictionary<string, ILogTarget> _logTargets = new();
    private ILogTarget _defaultLogTarget;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CollectionLogger()
    {
        _defaultLogTarget = new EmptyLogTarget();
        this["Default"] = _defaultLogTarget;
    }

    /// <inheritdoc/>
    public ILogTarget this[string name]
    {
        get
        {
            if (_logTargets.TryGetValue(name, out ILogTarget logTarget))
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
            _logTargets[name] = value;
        }
    }

    /// <inheritdoc/>
    public ILogTarget Default
    {
        get => _defaultLogTarget;
        set
        {
            if (value == null)
                return;
            _defaultLogTarget = value;
            this["Default"] = _defaultLogTarget;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, ILogTarget> AllLogTargets()
        => new ReadOnlyDictionary<string, ILogTarget>(_logTargets);

    /// <inheritdoc/>
    public bool RemoveLogTarget(string logTargetName)
        => _logTargets.TryRemove(logTargetName, out ILogTarget _);
}
