// -----------------------------------------------------------------------
// CompositeLogTarget.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Enables writing to 2 or more <see cref="ILogTarget" />s using a single <see cref="ILogger" /> entry.
/// </summary>
public sealed class CompositeLogTarget : ILogTarget
{
    private readonly IList<ILogTarget> WrappedLogTargets;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logTargets">The log targets to be aggregated.</param>
    public CompositeLogTarget(IEnumerable<ILogTarget> logTargets)
    {
        WrappedLogTargets = new List<ILogTarget>(logTargets);
        if (WrappedLogTargets.Count == 0)
            throw new ArgumentException("At least one logTarget must be specified.", nameof(logTargets));
    }

    /// <inheritdoc/>
    public LogLevel LoggingLevel { get; set; }

    /// <summary>
    /// The log targets that this <see cref="CompositeLogTarget"/> instance was initialized with.
    /// This property exists to allow settings changes to those log targets after initialization.
    /// </summary>
    public IReadOnlyCollection<ILogTarget> LogTargets
        => new ReadOnlyCollection<ILogTarget>(WrappedLogTargets);

    /// <inheritdoc/>
    public void Log(LogLevel level, string message)
    {
        for (int i = 0; i < WrappedLogTargets.Count; i++)
            WrappedLogTargets[i].Log(level, message);
    }
}
