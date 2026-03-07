// -----------------------------------------------------------------------
// SingleLogger.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// A Logger to use when it is known that only a single <see cref="ILogTarget"/> will be used, to improve performance.
/// </summary>
public sealed class SingleLogger : ILogger
{
    public ILogTarget this[string name]
    {
        get => Default;
        set => Default = value ?? new EmptyLogTarget();
    }

    public ILogTarget Default { get; set; } = new EmptyLogTarget();

    public IReadOnlyDictionary<string, ILogTarget> AllLogTargets()
        => new ReadOnlyDictionary<string, ILogTarget>(new Dictionary<string, ILogTarget>() { { "Default", Default } });

    public bool RemoveLogTarget(string logTargetName)
        => true;
}
