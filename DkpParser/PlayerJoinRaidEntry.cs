namespace DkpParser;

public sealed class PlayerJoinRaidEntry
{
    public DateTime Timestamp { get; set; }

    public string PlayerName { get; set; }

    public LogEntryType EntryType { get; set; }

    public string ToDisplayString()
        => $"{PlayerName} {EntryType} {Timestamp:HH:mm:ss}";
}
