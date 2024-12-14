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

        bool isRot = logLine.Contains(Constants.Rot);
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
            Winner = dkpEntry.PlayerName,
            IsRemoveCall = logLine.Contains(" " + Constants.Remove)
        };
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveSpentCall
{
    public string Auctioneer { get; init; }

    public LiveAuctionInfo AuctionStart { get; set; }

    public EqChannel Channel { get; init; }

    public string CharacterPlacingBid { get; set; }

    public int DkpSpent { get; init; }

    public bool IsRemoveCall { get; init; }

    public string ItemName { get; init; }

    public DateTime Timestamp { get; init; }

    public string Winner { get; init; }

    private string DebugText
        => $"{Timestamp:HH:mm} {ItemName} {Winner} {DkpSpent}";

    public override string ToString()
        => $"{Channel} {Constants.AttendanceDelimiter}{ItemName}{Constants.AttendanceDelimiter} {Winner} {DkpSpent} {Constants.DkpSpent}";
}
