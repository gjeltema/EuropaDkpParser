// -----------------------------------------------------------------------
// ActiveBiddingAnalyzer.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Gjeltema.Logging;

internal sealed partial class ActiveBiddingAnalyzer
{
    private const string LogPrefix = $"[{nameof(ActiveBiddingAnalyzer)}]";
    private const string MagicDieMessage = "**A Magic Die is rolled by ";
    private const int MinimumCharacterNameLength = 4;
    private const string RollResultMessage = "**It could have been any number from ";
    private readonly Regex _findDigits = FindDigitsRegex();
    private readonly IDkpParserSettings _settings;
    private MagicDieRollMessage _currentMagicMessage = null;

    public ActiveBiddingAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public bool CheckIfRoll(string logLineNoTimestamp, DateTime timestamp, out RawRollInfo rollInfo)
    {
        // **A Magic Die is rolled by Marak.
        // **It could have been any number from 0 to 100, but this time it turned up a 97.

        if (_currentMagicMessage != null && _currentMagicMessage.Timestamp < timestamp.AddSeconds(-2))
            _currentMagicMessage = null;

        rollInfo = null;
        bool isMagicMessage = logLineNoTimestamp.StartsWith(MagicDieMessage);
        if (isMagicMessage)
        {
            string characterName = logLineNoTimestamp[MagicDieMessage.Length..^1];
            _currentMagicMessage = new MagicDieRollMessage { CharacterName = characterName, Timestamp = timestamp };
            Log.Debug($"{LogPrefix} Magic Message: {logLineNoTimestamp}");
            return true;
        }
        else if (_currentMagicMessage != null && logLineNoTimestamp.StartsWith(RollResultMessage))
        {
            string messageAfterPreamble = logLineNoTimestamp[RollResultMessage.Length..^1];
            string[] messageParts = messageAfterPreamble.Split(' ');
            if (messageParts.Length != 11)
                return false;

            string randNumberRaw = messageParts[2].Replace(",", string.Empty);
            if (!int.TryParse(randNumberRaw, out int randNumber))
                return false;

            string rollResultRaw = messageParts[^1];
            if (!int.TryParse(rollResultRaw, out int rollResult))
                return false;

            rollInfo = new()
            {
                Timestamp = timestamp,
                CharacterRolling = _currentMagicMessage.CharacterName,
                RollAmount = rollResult
            };
            Log.Debug($"{LogPrefix} Roll performed: {rollInfo}");
            return true;
        }

        return false;
    }

    public RawBidInfo GetBidInfo(string messageFromPlayer, EqChannel channel, DateTime timestamp, string messageSenderName, IEnumerable<string> auctionItems)
    {
        // Jeplante tells the raid,  'Left Eye of Xygoz Jeplante 1 dkp'
        // Futtrup tells the raid,  'Left Eye of Xygoz - psychoblast - 50 dkp'
        // Zygon tells the raid,  'Left Eye of Xygoz zygon 60 dkp'
        // Aknok tells the raid,  'Eye of Xygoz Aknok 10 DKP'
        // Futtrup tells the raid,  'Left Eye of Xygoz - psychoblast - 70 dkp'
        // Tepla tells the raid,  ':::Left Eye of Xygoz::: Psychoblast 70dkp 10s '
        // Tepla tells the raid,  ':::Left Eye of Xygoz::: Psychoblast 70 SPENT '
        // Tepla tells the raid,  ':::Eye of Xygoz::: Aknok 10dkp 15sec '
        // Tepla tells the raid,  ':::Eye of Xygoz::: Aknok 10 SPENT '

        if (messageFromPlayer.Contains(Constants.DkpSpent))
            return null;

        // OrderByDescending to handle the (Left) Eye of Xygoz issue, where people bidding on Left Eye of Xygoz would have
        // their bids get lumped under the Eye of Xygoz auction if both items dropped.
        string auctionItemName = auctionItems
            .OrderByDescending(x => x.Length)
            .FirstOrDefault(x => messageFromPlayer.Contains(x));

        if (auctionItemName == null)
            return null;

        //** Change to use a Regex to find the status seconds
        // Only way to differentiate between a status call and an actual bid at this point
        if (messageFromPlayer.Contains(Constants.PossibleErrorDelimiter)
            && (messageFromPlayer.Contains("60s") || messageFromPlayer.Contains("30s") || messageFromPlayer.Contains("10s") || messageFromPlayer.Contains("COMPLETED")))
        {
            string updateState = "Unknown";
            if (messageFromPlayer.Contains("10s"))
                updateState = "10s";
            else if (messageFromPlayer.Contains("30s"))
                updateState = "30s";
            else if (messageFromPlayer.Contains("60s"))
                updateState = "60s";
            else if (messageFromPlayer.Contains("COMPLETED"))
                updateState = "COMP";

            RawBidInfo statusUpdate = new()
            {
                Timestamp = timestamp,
                Channel = channel,
                CharacterPlacingBid = messageSenderName,
                ItemName = auctionItemName,
                IsStatusUpdate = true,
                StatusValue = updateState
            };

            return statusUpdate;
        }

        int dkpValue = GetDigits(messageFromPlayer);
        if (dkpValue < 1)
            return null;

        int endIndexOfItem = messageFromPlayer.IndexOf(auctionItemName) + auctionItemName.Length;
        string lineAfterItem = messageFromPlayer[endIndexOfItem..];
        lineAfterItem = lineAfterItem
            .Replace(dkpValue.ToString(), "")
            .Replace("DKP", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" MAIN ", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ALT ", "", StringComparison.OrdinalIgnoreCase)
            .Replace(":", "")
            .Replace("-", "")
            .Trim();

        string characterBeingBidFor = messageSenderName;
        string[] lineParts = lineAfterItem.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (lineParts.Length > 0)
        {
            string characterName = lineParts[0];
            if (characterName.Length >= MinimumCharacterNameLength)
            {
                characterBeingBidFor = characterName.NormalizeName();
            }
        }

        bool characterNotOnDkpServer = _settings.CharactersOnDkpServer.CharacterConfirmedNotOnDkpServer(characterBeingBidFor);

        RawBidInfo newBid = new()
        {
            Timestamp = timestamp,
            Channel = channel,
            CharacterBeingBidFor = characterBeingBidFor,
            CharacterPlacingBid = messageSenderName,
            ItemName = auctionItemName,
            BidAmount = dkpValue,
            CharacterNotOnDkpServer = characterNotOnDkpServer
        };
        Log.Debug($"{LogPrefix} New bid: {newBid}");
        return newBid;
    }

    public LiveBidInfo ProcessBidInfo(RawBidInfo bidInfo, IEnumerable<LiveAuctionInfo> activeAuctions)
    {
        LiveAuctionInfo relatedAuction = activeAuctions.FirstOrDefault(x => x.ItemName == bidInfo.ItemName);

        if (relatedAuction == null)
            return null;

        if (bidInfo.IsStatusUpdate)
        {
            relatedAuction.SetStatusUpdate(bidInfo.CharacterPlacingBid, bidInfo.Timestamp, bidInfo.StatusValue);
            return null;
        }

        relatedAuction.HasNewBidsAdded = true;
        relatedAuction.HasBids = true;

        LiveBidInfo newBid = new()
        {
            Timestamp = bidInfo.Timestamp,
            Channel = bidInfo.Channel,
            ParentAuctionId = relatedAuction.Id,
            CharacterBeingBidFor = bidInfo.CharacterBeingBidFor,
            CharacterPlacingBid = bidInfo.CharacterPlacingBid,
            ItemName = relatedAuction.ItemName,
            BidAmount = bidInfo.BidAmount,
            CharacterNotOnDkpServer = bidInfo.CharacterNotOnDkpServer
        };
        Log.Debug($"{LogPrefix} New bid: {newBid}");
        return newBid;
    }

    public LiveBidInfo ProcessRollInfo(RawRollInfo rollInfo, IEnumerable<LiveAuctionInfo> activeAuctions)
    {
        LiveAuctionInfo parentAuction = activeAuctions.FirstOrDefault(x => x.IsRoll && x.RandValue == rollInfo.RollAmount);
        if (parentAuction == null)
            return null;

        parentAuction.HasNewBidsAdded = true;
        parentAuction.HasBids = true;

        LiveBidInfo newRoll = new()
        {
            Timestamp = rollInfo.Timestamp,
            Channel = parentAuction.Channel,
            ParentAuctionId = parentAuction.Id,
            CharacterPlacingBid = rollInfo.CharacterRolling,
            CharacterBeingBidFor = rollInfo.CharacterRolling,
            ItemName = parentAuction.ItemName,
            BidAmount = rollInfo.RollAmount,
            IsRoll = true
        };
        Log.Debug($"{LogPrefix} Roll performed: {newRoll}");
        return newRoll;
    }

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex FindDigitsRegex();

    private int GetDigits(string text)
    {
        Match m = _findDigits.Match(text);
        return int.TryParse(m.Value, out int result) ? result : 0;
    }

    [DebuggerDisplay("{DebugText,nq}")]
    private sealed class MagicDieRollMessage
    {
        public string CharacterName { get; init; }

        public DateTime Timestamp { get; init; }

        private string DebugText
            => $"{Timestamp:HH:mm:ss} {CharacterName}";
    }
}
