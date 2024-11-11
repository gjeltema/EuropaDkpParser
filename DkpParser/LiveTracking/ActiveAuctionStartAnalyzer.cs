// -----------------------------------------------------------------------
// ActiveAuctionStartAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Diagnostics;
using System.Text.RegularExpressions;

internal sealed partial class ActiveAuctionStartAnalyzer
{
    private readonly Regex _findMultipleItemsMarker = MultipleItemsAuctionedRegex();
    private readonly DelimiterStringSanitizer _sanitizer = new();

    public ICollection<LiveAuctionInfo> GetAuctionStart(string logLine, EqChannel channel, DateTime timeStamp)
    {
        if (!logLine.Contains("OPEN"))
            return [];

        int multiplier = GetMultiplier(logLine);
        string auctioneerName = GetAuctioneerName(logLine);

        if (logLine.Contains(Constants.PossibleErrorDelimiter))
        {
            string auctionLine = _sanitizer.SanitizeDelimiterString(logLine);
            string[] lineParts = auctionLine.Split(Constants.AttendanceDelimiter);
            if (lineParts.Length > 2)
            {
                string itemName = lineParts[1].Trim();
                return [new LiveAuctionInfo
                {
                    Timestamp = timeStamp,
                    Channel = channel,
                    Auctioneer = auctioneerName,
                    ItemName = itemName,
                    TotalNumberOfItems = multiplier,
                }];
            }
        }
        else
        {
            int startIndex = GetIndexOfEndOfMessagePreamble(logLine, channel);
            if (startIndex < 0)
                return [];

            int endIndex = logLine.Length - 2;
            int indexOfOpen = logLine.IndexOf("OPEN");
            int indexOfBids = logLine.IndexOf("BIDS", StringComparison.OrdinalIgnoreCase);
            if (indexOfBids < indexOfOpen)
            {
                int possibleEndIndex = indexOfBids < 1 ? indexOfOpen : indexOfBids;
                if (possibleEndIndex > 0)
                    endIndex = possibleEndIndex;
            }
            else
            {
                int possibleEndIndex = indexOfOpen < 1 ? indexOfBids : indexOfOpen;
                if (possibleEndIndex > 0)
                    endIndex = possibleEndIndex;
            }

            string itemsString = logLine[startIndex..endIndex];

            string[] itemNames = itemsString.Split(',', StringSplitOptions.TrimEntries);

            List<LiveAuctionInfo> auctions = new(itemNames.Length);
            foreach (string itemName in itemNames)
            {
                auctions.Add(new LiveAuctionInfo
                {
                    Timestamp = timeStamp,
                    Channel = channel,
                    Auctioneer = auctioneerName,
                    ItemName = itemName.Trim(),
                    TotalNumberOfItems = multiplier,
                });
            }

            var auctionGrouping = auctions.GroupBy(x => x.ItemName)
                .Select(a => new { ItemCount = a.Count(), Auction = a.First() })
                .ToList();

            auctions.Clear();
            foreach (var auctionGroup in auctionGrouping)
            {
                auctionGroup.Auction.TotalNumberOfItems = auctionGroup.ItemCount;
                auctions.Add(auctionGroup.Auction);
            }

            return auctions;
        }

        return [];
    }

    [GeneratedRegex("x\\d", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MultipleItemsAuctionedRegex();

    private string GetAuctioneerName(string logLine)
    {
        int indexOfSpace = logLine.IndexOf(' ');

        string auctioneerName = logLine[0..indexOfSpace].Trim();
        return auctioneerName;
    }

    private int GetEndIndex(string logLine, string searchString)
    {
        int indexOfStart = logLine.IndexOf(searchString);
        if (indexOfStart < 0)
            return -1;

        return indexOfStart + searchString.Length;
    }

    private int GetEndIndexOfChannelMessage(string logLine, string youTold, string otherTold)
    {
        int indexOfEnd = GetEndIndex(logLine, youTold);
        if (indexOfEnd >= 0)
            return indexOfEnd;

        indexOfEnd = GetEndIndex(logLine, otherTold);
        if (indexOfEnd >= 0)
            return indexOfEnd;

        return -1;
    }

    private int GetIndexOfEndOfMessagePreamble(string logLine, EqChannel channel)
        => channel switch
        {
            EqChannel.Raid => GetEndIndexOfChannelMessage(logLine, Constants.RaidYou, Constants.RaidOtherFull),
            EqChannel.Guild => GetEndIndexOfChannelMessage(logLine, Constants.GuildYou, Constants.GuildOther),
            EqChannel.Ooc => GetEndIndexOfChannelMessage(logLine, Constants.OocYou, Constants.OocOther),
            EqChannel.Auction => GetEndIndexOfChannelMessage(logLine, Constants.AuctionYou, Constants.AuctionOther),
            _ => -1,
        };

    private int GetMultiplier(string logLine)
    {
        Match m = _findMultipleItemsMarker.Match(logLine);
        string fullMatch = m.Value;
        string multiplierAsText = fullMatch.Replace("x", "").Replace("X", "");
        if (int.TryParse(multiplierAsText, out int multiplier))
        {
            return multiplier;
        }

        return 1;
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveAuctionInfo : IEquatable<LiveAuctionInfo>
{
    private static int currentId = 1;

    public LiveAuctionInfo()
    {
        Id = currentId++;
    }

    public string Auctioneer { get; set; }

    public EqChannel Channel { get; set; }

    public int Id { get; }

    public string ItemName { get; set; }

    public DateTime Timestamp { get; set; }

    public int TotalNumberOfItems { get; set; }

    private string DebugText
        => $"{Timestamp:HH:mm} {Id} {ItemName} {Auctioneer} {TotalNumberOfItems} items";

    public static bool operator ==(LiveAuctionInfo a, LiveAuctionInfo b)
        => Equals(a, b);

    public static bool operator !=(LiveAuctionInfo a, LiveAuctionInfo b)
        => !Equals(a, b);

    public static bool Equals(LiveAuctionInfo a, LiveAuctionInfo b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        if (a.ItemName != b.ItemName)
            return false;

        return true;
    }

    public override bool Equals(object obj)
        => Equals(obj as LiveAuctionInfo);

    public bool Equals(LiveAuctionInfo other)
        => Equals(this, other);

    public override int GetHashCode()
        => ItemName.GetHashCode();

    public override string ToString()
        => $"{Timestamp:HH:mm} {ItemName} {Auctioneer} {(TotalNumberOfItems > 1 ? "x" + TotalNumberOfItems.ToString() : "")}";
}
