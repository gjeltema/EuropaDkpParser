// -----------------------------------------------------------------------
// ActiveAuctionStartAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Text.RegularExpressions;

internal sealed partial class ActiveAuctionStartAnalyzer
{
    private readonly Regex _findMultipleItemsMarker = MultipleItemsAuctionedRegex();
    private readonly Regex _findNumbers = NumbersRegex();
    private readonly DelimiterStringSanitizer _sanitizer = new();

    public ICollection<LiveAuctionInfo> GetAuctionStart(string playerMessage, EqChannel channel, DateTime timeStamp, string messageSender)
    {
        if (playerMessage.Contains("OPEN"))
        {
            return HandleOpen(playerMessage, channel, timeStamp, messageSender);
        }
        else if (playerMessage.Contains("ROLL"))
        {
            return HandleRoll(playerMessage, channel, timeStamp, messageSender);
        }

        return [];
    }

    [GeneratedRegex("x\\d", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MultipleItemsAuctionedRegex();

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex NumbersRegex();

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

    private int GetRandNumber(string logLine)
    {
        Match m = _findNumbers.Match(logLine);
        string fullMatch = m.Value;
        if (int.TryParse(fullMatch, out int randNumber))
        {
            return randNumber;
        }

        return -1;
    }

    private ICollection<LiveAuctionInfo> HandleOpen(string playerMessage, EqChannel channel, DateTime timeStamp, string messageSender)
    {
        int multiplier = GetMultiplier(playerMessage);

        if (playerMessage.Contains(Constants.PossibleErrorDelimiter))
        {
            string auctionLine = _sanitizer.SanitizeDelimiterString(playerMessage);
            string[] lineParts = auctionLine.Split(Constants.AttendanceDelimiter, StringSplitOptions.RemoveEmptyEntries);
            if (lineParts.Length > 1)
            {
                string itemName = lineParts[0].Trim();
                return [new LiveAuctionInfo
                {
                    Timestamp = timeStamp,
                    Channel = channel,
                    Auctioneer = messageSender,
                    ItemName = itemName,
                    TotalNumberOfItems = multiplier,
                }];
            }
        }
        else
        {
            int endIndex = playerMessage.Length - 1;
            int indexOfOpen = playerMessage.IndexOf("OPEN");
            int indexOfBids = playerMessage.IndexOf("BIDS");
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

            string itemsString = playerMessage[0..endIndex];

            string[] itemNames = itemsString.Split(',', StringSplitOptions.RemoveEmptyEntries);

            List<LiveAuctionInfo> auctions = new(itemNames.Length);
            foreach (string itemName in itemNames)
            {
                auctions.Add(new LiveAuctionInfo
                {
                    Timestamp = timeStamp,
                    Channel = channel,
                    Auctioneer = messageSender,
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

    private ICollection<LiveAuctionInfo> HandleRoll(string logLine, EqChannel channel, DateTime timeStamp, string messageSender)
    {
        // You tell your raid, 'Runed Bolster Belt 333 ROLL'
        int randNumber = GetRandNumber(logLine);
        if (randNumber < 1)
            return [];

        int endIndex = logLine.IndexOf(randNumber.ToString());

        // Item or reward name is at least 3 chars, 7 is the length of ROLL + space + at least 1 digit + space
        if (endIndex < 3 || endIndex > (logLine.Length - 7))
            return [];

        string randName = logLine[..endIndex].Trim();

        return [new LiveAuctionInfo
            {
                Timestamp = timeStamp,
                Channel = channel,
                Auctioneer = messageSender,
                ItemName = randName,
                TotalNumberOfItems = randNumber,
                IsRoll = true
            }];
    }
}
