// -----------------------------------------------------------------------
// DkpEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{PlayerName,nq}, {Item,nq} {DkpSpent,nq}")]
public sealed class DkpEntry
{
    public const string You = "You";

    public string Auctioneer { get; set; }

    public int DkpSpent { get; set; }

    public string Item { get; set; }

    public string PlayerName { get; set; }

    public PossibleError PossibleError { get; set; } = PossibleError.None;

    public string RawLogLine { get; set; }

    public DateTime Timestamp { get; set; }

    public string ToLogString()
    {
        // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
        // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
        return Auctioneer == You
            ? $"You tell your raid, '{Constants.AttendanceDelimiter}{Item}{Constants.AttendanceDelimiter} {PlayerName} {DkpSpent} {Constants.DkpSpent}'"
            : $"{Auctioneer} tells the raid,  '{Constants.AttendanceDelimiter}{Item}{Constants.AttendanceDelimiter} {PlayerName} {DkpSpent} {Constants.DkpSpent}'";
    }

    public override string ToString()
        => $"{Timestamp:HH:mm:ss} {PlayerName}\t{Item}  {DkpSpent}";
}
