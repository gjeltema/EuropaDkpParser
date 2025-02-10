// -----------------------------------------------------------------------
// ILogger.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging;

using System.Collections.Generic;

/// <summary>Interface that describes a <see cref="ILogTarget"/> collection class.</summary>
public interface ILogger
{
    /// <summary>Gets or sets the log target with the specified name.<br/>
    /// If the provided log target name does not exist, the log target assigned as the Default log target will be returned.</summary>
    /// <param name="name">The name of the target to use.</param>
    ILogTarget this[string name] { get; set; }

    /// <summary>Gets or sets the default log target.</summary>
    /// <remarks>
    ///     The default target is equivalent to the one named "Default".
    ///     It's the one that will be used whenever a different target is
    ///     not specified.
    /// </remarks>
    ILogTarget Default { get; set; }

    /// <summary>
    /// Gets a readonly collection of all the stored <see cref="ILogTarget"/>s.
    /// </summary>
    IReadOnlyDictionary<string, ILogTarget> AllLogTargets();

    /// <summary>
    /// Removes the <see cref="ILogTarget"/> referenced by the <paramref name="logTargetName"/> from the collection.
    /// Does not throw if the log target name does not exist.
    /// </summary>
    /// <param name="logTargetName">The name of the log target used as the input into the indexer to retrieve it.</param>
    /// <returns>True if the <see cref="ILogTarget"/> was successfully removed.  False if not.</returns>
    bool RemoveLogTarget(string logTargetName);
}
