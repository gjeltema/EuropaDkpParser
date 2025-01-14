// -----------------------------------------------------------------------
// ActiveAuctionEndAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

internal sealed class ActiveAuctionEndAnalyzer
{
    private readonly DkpSpentAnalyzer _dkpSpentAnalyzer;

    public ActiveAuctionEndAnalyzer(Action<string> errorMessageHandler)
    {
        _dkpSpentAnalyzer = new(errorMessageHandler);
    }

    public LiveSpentCall GetSpentCall(string logLineNoTimestamp, EqChannel channel, DateTime timestamp)
    {
        if (!logLineNoTimestamp.Contains(Constants.PossibleErrorDelimiter))
            return null;

        if (logLineNoTimestamp.EndsWith(Constants.RollWin + "'"))
        {
            // $"{channel} :::{itemLink}::: {spentCall.Winner} rolled {spentCall.DkpSpent} WINS"
            string[] parts = logLineNoTimestamp.Split(Constants.AttendanceDelimiter);
            if (parts.Length != 3)
                return null;

            string itemName = parts[1];
            string[] remainingParts = parts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (remainingParts.Length != 4)
                return null;

            string winner = remainingParts[0];
            string winningRollString = remainingParts[2];
            if (!int.TryParse(winningRollString, out int winningRoll))
                return null;

            return new LiveSpentCall
            {
                Timestamp = timestamp,
                Channel = channel,
                ItemName = itemName,
                DkpSpent = winningRoll,
                Winner = winner,
                IsRemoveCall = false
            };
        }

        bool isRot = logLineNoTimestamp.Contains(Constants.Rot);
        bool isSpent = logLineNoTimestamp.Contains(Constants.DkpSpent);
        if (!isSpent && !isRot)
            return null;

        DkpEntry dkpEntry = _dkpSpentAnalyzer.ExtractDkpSpentInfo(logLineNoTimestamp, channel, timestamp);
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
            IsRemoveCall = logLineNoTimestamp.Contains(" " + Constants.Remove)
        };
    }
}
