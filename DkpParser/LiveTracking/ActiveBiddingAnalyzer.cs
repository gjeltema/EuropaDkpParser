// -----------------------------------------------------------------------
// ActiveBiddingAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

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

    public LiveBidInfo GetBidInformation(string messageFromPlayer, EqChannel channel, DateTime timestamp, string messageSenderName, IEnumerable<LiveAuctionInfo> activeAuctions)
    {
        // Jeplante tells the raid,  'Left Eye of Xygoz Jeplante 1 dkp'
        // Futtrup tells the raid,  'Left Eye of Xygoz - psychoblast - 50 dkp'
        // Zygon tells the raid,  'Left Eye of Xygoz zygon 60 dkp'
        // Aknok tells the raid,  'Eye of Xygoz Aknok 10 DKP'
        // Futtrup tells the raid,  'Left Eye of Xygoz - psychoblast - 70 dkp'
        // Tepla tells the raid,  ':::Left Eye of Xygoz::: Psychoblast 70dkp 15sec '
        // Tepla tells the raid,  ':::Left Eye of Xygoz::: Psychoblast 70 SPENT '
        // Tepla tells the raid,  ':::Eye of Xygoz::: Aknok 10dkp 15sec '
        // Tepla tells the raid,  ':::Eye of Xygoz::: Aknok 10 SPENT '

        // OrderByDescending to handle the (Left) Eye of Xygoz issue, where people bidding on Left Eye of Xygoz would have
        // their bids get lumped under the Eye of Xygoz auction if both items dropped.
        LiveAuctionInfo relatedAuction = activeAuctions
            .OrderByDescending(x => x.ItemName.Length)
            .FirstOrDefault(x => messageFromPlayer.Contains(x.ItemName));

        if (relatedAuction == null)
            return null;

        // Only way to differentiate between a status call and an actual bid at this point
        if (messageFromPlayer.Contains(Constants.PossibleErrorDelimiter)
            && (messageFromPlayer.Contains("60s") || messageFromPlayer.Contains("30s") || messageFromPlayer.Contains("10s") || messageFromPlayer.Contains("COMPLETED")))
        {
            relatedAuction.SetStatusUpdate(messageSenderName, timestamp);
            return null;
        }

        string itemName = relatedAuction.ItemName;
        if (itemName == null)
            return null;

        int dkpValue = GetDigits(messageFromPlayer);
        if (dkpValue < 1)
            return null;

        int endIndexOfItem = messageFromPlayer.IndexOf(itemName) + itemName.Length;
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
        relatedAuction.HasNewBidsAdded = true;
        relatedAuction.HasBids = true;

        LiveBidInfo newBid = new()
        {
            Timestamp = timestamp,
            Channel = channel,
            ParentAuctionId = relatedAuction.Id,
            CharacterBeingBidFor = characterBeingBidFor,
            CharacterPlacingBid = messageSenderName,
            ItemName = itemName,
            BidAmount = dkpValue,
            CharacterNotOnDkpServer = characterNotOnDkpServer
        };
        Log.Debug($"{LogPrefix} New bid: {newBid}");
        return newBid;
    }

    public LiveBidInfo GetRollInfo(string logLineNoTimestamp, DateTime timestamp, IEnumerable<LiveAuctionInfo> activeAuctions)
    {
        // **A Magic Die is rolled by Marak.
        // **It could have been any number from 0 to 100, but this time it turned up a 97.

        if (_currentMagicMessage != null && _currentMagicMessage.Timestamp < timestamp.AddSeconds(-2))
            _currentMagicMessage = null;

        bool isMagicMessage = logLineNoTimestamp.StartsWith(MagicDieMessage);
        if (isMagicMessage)
        {
            string characterName = logLineNoTimestamp[MagicDieMessage.Length..^1];
            _currentMagicMessage = new MagicDieRollMessage { CharacterName = characterName, Timestamp = timestamp };
            Log.Debug($"{LogPrefix} Magic Message: {logLineNoTimestamp}");
            return null;
        }
        else if (_currentMagicMessage != null && logLineNoTimestamp.StartsWith(RollResultMessage))
        {
            string messageAfterPreamble = logLineNoTimestamp[RollResultMessage.Length..^1];
            string[] messageParts = messageAfterPreamble.Split(' ');
            if (messageParts.Length != 11)
                return null;

            string randNumberRaw = messageParts[2].Replace(",", string.Empty);
            if (!int.TryParse(randNumberRaw, out int randNumber))
                return null;

            string rollResultRaw = messageParts[^1];
            if (!int.TryParse(rollResultRaw, out int rollResult))
                return null;

            LiveAuctionInfo parentAuction = activeAuctions.FirstOrDefault(x => x.IsRoll && x.RandValue == randNumber);
            if (parentAuction == null)
                return null;

            parentAuction.HasNewBidsAdded = true;
            parentAuction.HasBids = true;

            LiveBidInfo newRoll = new()
            {
                Timestamp = timestamp,
                Channel = parentAuction.Channel,
                ParentAuctionId = parentAuction.Id,
                CharacterPlacingBid = _currentMagicMessage.CharacterName,
                CharacterBeingBidFor = _currentMagicMessage.CharacterName,
                ItemName = parentAuction.ItemName,
                BidAmount = rollResult,
                IsRoll = true
            };
            Log.Debug($"{LogPrefix} Roll performed: {newRoll}");
            return newRoll;
        }

        return null;
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
