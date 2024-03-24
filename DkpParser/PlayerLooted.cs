// -----------------------------------------------------------------------
// PlayerLooted.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{PlayerName,nq}, {ItemLooted,nq}")]
public sealed class PlayerLooted
{
    public string ItemLooted { get; init; }

    public string PlayerName { get; init; }

    public DateTime Timestamp { get; init; }

    public override string ToString()
        => $"{Timestamp:HH:mm:ss} {PlayerName,-25} {ItemLooted}";
}
