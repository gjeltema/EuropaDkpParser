// -----------------------------------------------------------------------
// ActiveBiddingAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Diagnostics;
using System.Text.RegularExpressions;

internal sealed partial class ActiveBiddingAnalyzer
{
    private const int MinimumCharacterNameLength = 4;
    private readonly Regex _findDigits = FindDigitsRegex();

    public LiveBidInfo GetBidInformation(string logLine, EqChannel channel, DateTime timestamp, IEnumerable<LiveAuctionInfo> activeAuctions)
    {
        int indexOfFirstSpace = logLine.IndexOf(' ');
        string bidderName = logLine[0..indexOfFirstSpace];

        LiveAuctionInfo relatedAuction = activeAuctions.FirstOrDefault(x => logLine.Contains(x.ItemName));
        if (bidderName.Equals(relatedAuction.Auctioneer))
            return null;

        //** How to differentiate between a bid and a status statement if someone else picks up as Auctioneer?

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
            .Replace("MAIN", "", StringComparison.OrdinalIgnoreCase)
            .Replace("ALT", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-", "")
            .Trim();

        string[] lineParts = lineAfterItem.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (lineParts.Length > 0)
        {
            string characterName = lineParts[0].Trim();
            if (characterName.Length >= MinimumCharacterNameLength)
            {
                return new LiveBidInfo
                {
                    Timestamp = timestamp,
                    Channel = channel,
                    ParentAuctionId = relatedAuction.Id,
                    CharacterName = characterName,
                    ItemName = itemName,
                    BidAmount = dkpValue
                };
            }
        }


        return new LiveBidInfo
        {
            Timestamp = timestamp,
            Channel = channel,
            ParentAuctionId = relatedAuction.Id,
            CharacterName = bidderName,
            ItemName = itemName,
            BidAmount = dkpValue
        };
    }

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex FindDigitsRegex();

    private int GetDigits(string text)
    {
        Match m = _findDigits.Match(text);
        return int.TryParse(m.Value, out int result) ? result : 0;
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveBidInfo
{
    public int BidAmount { get; init; }

    public EqChannel Channel { get; init; }

    public string CharacterName { get; init; }

    public string ItemName { get; init; }

    public int ParentAuctionId { get; init; }

    public DateTime Timestamp { get; init; }

    private string DebugText
        => $"{Timestamp:HH:mm:ss} {ParentAuctionId} {ItemName} {CharacterName} {BidAmount}";

    public override string ToString()
        => $"{Timestamp:HH:mm:ss} {ItemName} {CharacterName} {BidAmount}";
}
