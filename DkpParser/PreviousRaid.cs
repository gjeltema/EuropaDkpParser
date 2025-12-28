// -----------------------------------------------------------------------
// PreviousRaid.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class PreviousRaid
{
    public ICollection<int> CharacterIds { get; init; }

    public string RaidName { get; init; }

    public DateTime RaidTime { get; init; }

    public override string ToString()
        => $"{RaidName} {RaidTime:g} {string.Join(',', CharacterIds.Order())}";
}
