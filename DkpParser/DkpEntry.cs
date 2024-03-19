// -----------------------------------------------------------------------
// DkpEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpEntry
{
    public int DkpSpent { get; set; }

    public string Item { get; set; }

    public string PlayerName { get; set; }

    public DateTime Timestamp { get; set; }
}
