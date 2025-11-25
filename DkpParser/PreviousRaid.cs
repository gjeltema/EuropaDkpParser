// -----------------------------------------------------------------------
// PreviousRaid.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class PreviousRaid
{
    public ICollection<int> CharacterIds { get; init; }

    public DateTime RaidTime { get; init; }
}
