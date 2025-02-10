// -----------------------------------------------------------------------
// SingleLogger.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System.Collections.Generic;
using System.Collections.ObjectModel;

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
