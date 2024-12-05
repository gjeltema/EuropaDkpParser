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
    private readonly IDkpParserSettings _settings;

    public ActiveBiddingAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public LiveBidInfo GetBidInformation(string logLine, EqChannel channel, DateTime timestamp, IEnumerable<LiveAuctionInfo> activeAuctions)
    {
        int indexOfFirstSpace = logLine.IndexOf(' ');
        string bidderName = logLine[0..indexOfFirstSpace].NormalizeName();

        LiveAuctionInfo relatedAuction = activeAuctions.FirstOrDefault(x => logLine.Contains(x.ItemName));
        if (relatedAuction == null)
            return null;

        if (bidderName.Equals(relatedAuction.Auctioneer))
            return null;

        // Only way to differentiate between a status call and an actual bid at this point
        if (logLine.Contains("60s") || logLine.Contains("30s") || logLine.Contains("10s"))
            return null;

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
            .Replace("-", "")
            .Trim();

        string[] lineParts = lineAfterItem.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (lineParts.Length > 0)
        {
            string characterName = lineParts[0].Trim();
            if (characterName.Length >= MinimumCharacterNameLength)
            {
                bidderName = characterName.NormalizeName();
            }
        }

        bool isCharacterOnServer = _settings.CharactersOnDkpServer.DoesCharacterExistOnDkpServer(bidderName);
        relatedAuction.HasNewBidsAdded = true;
        return new LiveBidInfo
        {
            Timestamp = timestamp,
            Channel = channel,
            ParentAuctionId = relatedAuction.Id,
            CharacterName = bidderName,
            ItemName = itemName,
            BidAmount = dkpValue,
            CharacterNotOnDkpServer = !isCharacterOnServer
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
public sealed class LiveBidInfo : IEquatable<LiveBidInfo>
{
    public int BidAmount { get; init; }

    public EqChannel Channel { get; init; }

    public string CharacterName { get; init; }

    public bool CharacterNotOnDkpServer { get; init; }

    public string ItemName { get; init; }

    public int ParentAuctionId { get; init; }

    public DateTime Timestamp { get; init; }

    private string DebugText
        => $"{Timestamp:HH:mm:ss} {ParentAuctionId} {ItemName} {CharacterName} {BidAmount}";

    public static bool operator ==(LiveBidInfo left, LiveBidInfo right)
        => Equals(left, right);

    public static bool operator !=(LiveBidInfo left, LiveBidInfo right)
        => !Equals(left, right);

    public static bool Equals(LiveBidInfo left, LiveBidInfo right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        if (ReferenceEquals(left, right))
            return true;

        if (left.ParentAuctionId != right.ParentAuctionId)
            return false;

        if (left.BidAmount != right.BidAmount)
            return false;

        if (left.ItemName != right.ItemName)
            return false;

        if (!left.CharacterName.Equals(right.CharacterName, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public override bool Equals(object other)
        => Equals(this, other as LiveBidInfo);

    public bool Equals(LiveBidInfo other)
        => Equals(this, other);

    public override int GetHashCode()
        => ParentAuctionId.GetHashCode() ^ BidAmount.GetHashCode() ^ ItemName.GetHashCode() ^ CharacterName.ToUpper().GetHashCode();

    public override string ToString()
        => CharacterNotOnDkpServer
        ? $"{Timestamp:HH:mm:ss} {ItemName} {CharacterName} {BidAmount} NOT ON SERVER"
        : $"{Timestamp:HH:mm:ss} {ItemName} {CharacterName} {BidAmount}";
}
