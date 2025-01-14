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

    public ICollection<LiveAuctionInfo> GetAuctionStart(string logLine, EqChannel channel, DateTime timeStamp)
    {
        if (logLine.Contains("OPEN"))
        {
            return HandleOpen(logLine, channel, timeStamp);
        }
        else if (logLine.Contains("ROLL"))
        {
            return HandleRoll(logLine, channel, timeStamp);
        }

        return [];
    }

    [GeneratedRegex("x\\d", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MultipleItemsAuctionedRegex();

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex NumbersRegex();

    private string GetAuctioneerName(string logLine)
    {
        int indexOfSpace = logLine.IndexOf(' ');
        if (indexOfSpace < 3)
            return string.Empty;

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

    private ICollection<LiveAuctionInfo> HandleOpen(string logLine, EqChannel channel, DateTime timeStamp)
    {
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
            if (startIndex < 10)
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

            string[] itemNames = itemsString.Split(',', StringSplitOptions.RemoveEmptyEntries);

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

    private ICollection<LiveAuctionInfo> HandleRoll(string logLine, EqChannel channel, DateTime timeStamp)
    {
        // You tell your raid, 'Runed Bolster Belt 333 ROLL'
        int randNumber = GetRandNumber(logLine);
        if (randNumber < 1)
            return [];

        string auctioneerName = GetAuctioneerName(logLine);
        int startIndex = GetIndexOfEndOfMessagePreamble(logLine, channel);
        int endIndex = logLine.IndexOf(randNumber.ToString());

        // 10 is at least the length of the preamble message, and 6 is the length of ROLL + space + at least 1 digit + space
        if (startIndex < 10 || endIndex < startIndex || endIndex > (logLine.Length - 7))
            return [];

        string randName = logLine[startIndex..endIndex].Trim();

        return [new LiveAuctionInfo
            {
                Timestamp = timeStamp,
                Channel = channel,
                Auctioneer = auctioneerName,
                ItemName = randName,
                TotalNumberOfItems = randNumber,
                IsRoll = true
            }];
    }
}
