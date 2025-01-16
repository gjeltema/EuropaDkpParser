// -----------------------------------------------------------------------
// ActiveBiddingAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Text.RegularExpressions;

internal sealed partial class ActiveBiddingAnalyzer
{
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

    public LiveBidInfo GetBidInformation(string logLine, EqChannel channel, DateTime timestamp, IEnumerable<LiveAuctionInfo> activeAuctions)
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

        // Only way to differentiate between a status call and an actual bid at this point
        if (logLine.Contains(Constants.PossibleErrorDelimiter)
            && (logLine.Contains("60s") || logLine.Contains("30s") || logLine.Contains("10s") || logLine.Contains("COMPLETED")))
            return null;

        // OrderByDescending to handle the (Left) Eye of Xygoz issue, where people bidding on Left Eye of Xygoz would have
        // their bids get lumped under the Eye of Xygoz auction if both items dropped.
        LiveAuctionInfo relatedAuction = activeAuctions
            .OrderByDescending(x => x.ItemName.Length)
            .FirstOrDefault(x => logLine.Contains(x.ItemName));

        if (relatedAuction == null)
            return null;

        int indexOfFirstSpace = logLine.IndexOf(' ');
        string bidderName = logLine[0..indexOfFirstSpace].NormalizeName();

        string itemName = relatedAuction.ItemName;
        if (itemName == null)
            return null;

        int dkpValue = GetDigits(logLine);
        if (dkpValue < 1)
            return null;

        int endIndexOfItem = logLine.IndexOf(itemName) + itemName.Length;
        string lineAfterItem = logLine[endIndexOfItem..^1];
        lineAfterItem = lineAfterItem
            .Replace(dkpValue.ToString(), "")
            .Replace("DKP", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" MAIN ", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ALT ", "", StringComparison.OrdinalIgnoreCase)
            .Replace(":", "")
            .Replace("-", "")
            .Trim();

        string characterBeingBidFor = bidderName;
        string[] lineParts = lineAfterItem.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (lineParts.Length > 0)
        {
            string characterName = lineParts[0].Trim();
            if (characterName.Length >= MinimumCharacterNameLength)
            {
                characterBeingBidFor = characterName.NormalizeName();
            }
        }

        bool characterNotOnDkpServer = _settings.CharactersOnDkpServer.CharacterConfirmedNotOnDkpServer(characterBeingBidFor);
        relatedAuction.HasNewBidsAdded = true;
        relatedAuction.HasBids = true;

        return new LiveBidInfo
        {
            Timestamp = timestamp,
            Channel = channel,
            ParentAuctionId = relatedAuction.Id,
            CharacterBeingBidFor = characterBeingBidFor,
            CharacterPlacingBid = bidderName,
            ItemName = itemName,
            BidAmount = dkpValue,
            CharacterNotOnDkpServer = characterNotOnDkpServer
        };
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

            string rollResultRaw = messageParts[messageParts.Length - 1];
            if (!int.TryParse(rollResultRaw, out int rollResult))
                return null;

            LiveAuctionInfo parentAuction = activeAuctions.FirstOrDefault(x => x.IsRoll && x.TotalNumberOfItems == randNumber);
            if (parentAuction == null)
                return null;

            parentAuction.HasNewBidsAdded = true;
            parentAuction.HasBids = true;

            return new LiveBidInfo
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

    private sealed class MagicDieRollMessage
    {
        public string CharacterName { get; init; }

        public DateTime Timestamp { get; init; }
    }
}
