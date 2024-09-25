// -----------------------------------------------------------------------
// CharacterJoinRaidEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugDisplay}")]
public sealed class CharacterJoinRaidEntry
{
    public LogEntryType EntryType { get; set; }

    public string CharacterName { get; set; }

    public DateTime Timestamp { get; set; }

    private string DebugDisplay
        => ToDisplayString();

    public string ToDisplayString()
        => $"{CharacterName} {(EntryType == LogEntryType.JoinedRaid ? "Join" : "Leave")} {Timestamp:HH:mm:ss}";

    public override string ToString()
        => $"[{Timestamp:HH:mm:ss}] {CharacterName} has {(EntryType == LogEntryType.JoinedRaid ? "joined" : "left")} the raid.";
}
