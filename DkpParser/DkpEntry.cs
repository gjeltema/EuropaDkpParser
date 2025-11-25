// -----------------------------------------------------------------------
// DkpEntry.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{PlayerName,nq}, {Item,nq} {DkpSpent,nq}")]
public sealed class DkpEntry
{
    private const string You = "You";

    public AttendanceEntry AssociatedAttendanceCall { get; set; }

    public string Auctioneer { get; set; } = string.Empty;

    public EqChannel Channel { get; set; }

    public string CharacterName { get; set; } = string.Empty;

    public int DkpSpent { get; set; } = 0;

    public string Item { get; set; } = string.Empty;

    public PossibleError PossibleError { get; set; } = PossibleError.None;

    public string RawLogLine { get; set; }

    public DateTime Timestamp { get; set; }

    public string ToDebugString()
        => $"Extracted info: {Timestamp:HH:mm:ss} {Channel} {CharacterName}  {Item}  {DkpSpent} DKP, Error:{PossibleError}; Raw log line: {RawLogLine}";

    public string ToLogString()
    {
        // Remove backticks so that the output doesnt screw up displays that use markdown, such as Discord
        string itemName = Item.Replace('`', '\'');
        string message = $"{Constants.AttendanceDelimiter}{itemName}{Constants.AttendanceDelimiter} {CharacterName} {DkpSpent} {Constants.DkpSpent}";

        if (Channel == EqChannel.Raid)
            return Auctioneer == You
                ? EqLogLine.YouTellRaid(Timestamp, message)
                : EqLogLine.OtherTellsRaid(Timestamp, Auctioneer, message);
        if (Channel == EqChannel.Guild)
            return Auctioneer == You
                ? EqLogLine.YouTellGuild(Timestamp, message)
                : EqLogLine.OtherTellsGuild(Timestamp, Auctioneer, message);

        // Default if nothing else matches for some reason
        return Auctioneer == You
            ? EqLogLine.YouTellRaid(Timestamp, message)
            : EqLogLine.OtherTellsRaid(Timestamp, Auctioneer, message);
    }

    public override string ToString()
        => $"{Timestamp:HH:mm:ss} {CharacterName}  {Item}  {DkpSpent} DKP";

    public string ToSummaryDisplay()
    {
        string itemName = Item.Replace('`', '\'');
        return $"{Timestamp:HH:mm:ss} {CharacterName} {itemName} {DkpSpent} DKP";
    }
}
