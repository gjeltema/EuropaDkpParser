// -----------------------------------------------------------------------
// PlayerJoinRaidEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class PlayerJoinRaidEntry
{
    public LogEntryType EntryType { get; set; }

    public string PlayerName { get; set; }

    public DateTime Timestamp { get; set; }

    private string DebugDisplay
        => ToDisplayString();

    public string ToDisplayString()
        => $"{PlayerName} {(EntryType == LogEntryType.JoinedRaid ? "Join" : "Leave")} {Timestamp:HH:mm:ss}";

    public override string ToString()
        => $"[{Timestamp:HH:mm:ss}] {PlayerName} has {(EntryType == LogEntryType.JoinedRaid ? "joined" : "left")} the raid.";
}
