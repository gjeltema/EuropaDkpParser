// -----------------------------------------------------------------------
// ActiveAuctionEndAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Diagnostics;

internal sealed class ActiveAuctionEndAnalyzer
{
    private readonly DkpSpentAnalyzer _dkpSpentAnalyzer;

    public ActiveAuctionEndAnalyzer(Action<string> errorMessageHandler)
    {
        _dkpSpentAnalyzer = new(errorMessageHandler);
    }

    public LiveSpentCall GetSpentCall(string logLine, EqChannel channel, DateTime timestamp)
    {
        if (!logLine.Contains(Constants.PossibleErrorDelimiter))
            return null;

        bool isRot = logLine.Contains("ROT");
        bool isSpent = logLine.Contains(Constants.DkpSpent);
        if (!isSpent && !isRot)
            return null;

        DkpEntry dkpEntry = _dkpSpentAnalyzer.ExtractDkpSpentInfo(logLine, channel, timestamp);
        if (dkpEntry == null)
            return null;

        return new LiveSpentCall
        {
            Timestamp = timestamp,
            Channel = channel,
            Auctioneer = dkpEntry.Auctioneer,
            ItemName = dkpEntry.Item,
            DkpSpent = dkpEntry.DkpSpent,
            Winner = dkpEntry.PlayerName
        };
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveSpentCall
{
    public string Auctioneer { get; set; }

    public LiveAuctionInfo AuctionStart { get; set; }

    public EqChannel Channel { get; set; }

    public int DkpSpent { get; set; }

    public string ItemName { get; set; }

    public DateTime Timestamp { get; set; }

    public string Winner { get; set; }

    private string DebugText
        => $"{Timestamp:HH:mm} {ItemName} {Winner} {DkpSpent}";

    public override string ToString()
        => $"{Channel} {Constants.AttendanceDelimiter}{ItemName}{Constants.AttendanceDelimiter} {Winner} {DkpSpent} {Constants.DkpSpent}";
}
