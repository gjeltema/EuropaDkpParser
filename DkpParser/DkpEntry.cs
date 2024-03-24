// -----------------------------------------------------------------------
// DkpEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{PlayerName,nq}, {Item,nq} {DkpSpent,nq}")]
public sealed class DkpEntry
{
    public int DkpSpent { get; set; }

    public string Item { get; set; }

    public string PlayerName { get; set; }

    public PossibleError PossibleError { get; set; } = PossibleError.None;

    public DateTime Timestamp { get; set; }

    public override string ToString()
        => $"{PlayerName}\t{Item}  {DkpSpent}";
}
