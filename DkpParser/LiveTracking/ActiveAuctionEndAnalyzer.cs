// -----------------------------------------------------------------------
// ActiveAuctionEndAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using Gjeltema.Logging;

internal sealed class ActiveAuctionEndAnalyzer
{
    private readonly DkpSpentAnalyzer _dkpSpentAnalyzer;
    private const string LogPrefix = $"[{nameof(ActiveAuctionEndAnalyzer)}]";

    public ActiveAuctionEndAnalyzer()
    {
        _dkpSpentAnalyzer = new();
    }

    public LiveSpentCall GetSpentCall(string messageFromPlayer, EqChannel channel, DateTime timestamp, string messageSenderName)
    {
        if (!messageFromPlayer.Contains(Constants.PossibleErrorDelimiter))
            return null;

        if (messageFromPlayer.EndsWith(Constants.RollWin))
        {
            // $"{channel} :::{itemLink}::: {spentCall.Winner} rolled {spentCall.DkpSpent} WINS"
            string[] parts = messageFromPlayer.Split(Constants.AttendanceDelimiter, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                Log.Warning($"{LogPrefix} {nameof(messageFromPlayer)} is not properly formatted, it has more than 2 ::: separated parts: {messageFromPlayer}");
                return null;
            }

            string itemName = parts[0];
            string[] remainingParts = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (remainingParts.Length != 4)
            {
                Log.Warning($"{LogPrefix} {nameof(messageFromPlayer)} is not properly formatted, it has more than 4 ' ' separated parts: {messageFromPlayer}");
                return null;
            }

            string winner = remainingParts[0];
            string winningRollString = remainingParts[2];
            if (!int.TryParse(winningRollString, out int winningRoll))
            {
                Log.Warning($"{LogPrefix} Unable to get the winning roll from WINS call from: {messageFromPlayer}");
                return null;
            }

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

        bool isRot = messageFromPlayer.Contains(Constants.Rot);
        bool isSpent = messageFromPlayer.Contains(Constants.DkpSpent);
        if (!isSpent && !isRot)
            return null;

        DkpEntry dkpEntry = _dkpSpentAnalyzer.ExtractDkpSpentInfo(messageFromPlayer, channel, timestamp, messageSenderName);
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
            IsRemoveCall = messageFromPlayer.Contains(" " + Constants.Remove)
        };
    }
}
