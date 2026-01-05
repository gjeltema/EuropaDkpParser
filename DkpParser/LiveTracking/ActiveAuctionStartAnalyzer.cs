// -----------------------------------------------------------------------
// ActiveAuctionStartAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System;
using System.Text.RegularExpressions;
using Gjeltema.Logging;

internal sealed partial class ActiveAuctionStartAnalyzer
{
    private const string LogPrefix = $"[{nameof(ActiveBiddingAnalyzer)}]";
    private readonly Regex _findMultipleItemsMarker = MultipleItemsAuctionedRegex();
    private readonly Regex _findMultipleItemsParensMarker = MultipleItemsAuctionedParensRegex();
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

    [GeneratedRegex("\\(\\d\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MultipleItemsAuctionedParensRegex();

    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex NumbersRegex();

    private int GetMultiplier(string itemString, out string multiplierDeclaration)
    {
        // A Glowing Orb of Luclinite (6)
        Match mWithParen = _findMultipleItemsParensMarker.Match(itemString);
        multiplierDeclaration = mWithParen.Value;
        Match numberFromParen = _findNumbers.Match(multiplierDeclaration);
        string multiplierAsTextParen = numberFromParen.Value;
        if (int.TryParse(multiplierAsTextParen, out int multiplierParen))
        {
            return multiplierParen;
        }

        // Ring of the Depths x2
        Match mWithX = _findMultipleItemsMarker.Match(itemString);
        multiplierDeclaration = mWithX.Value;
        string multiplierAsTextX = multiplierDeclaration.Replace("x", "", StringComparison.OrdinalIgnoreCase);
        if (int.TryParse(multiplierAsTextX, out int multiplierX))
        {
            return multiplierX;
        }

        multiplierDeclaration = string.Empty;
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
        // [Thu Jan 01 23:09:16 2026] You tell your raid, 'A Glowing Orb of Luclinite | A Glowing Orb of Luclinite | Brilliant Stone Ring | Ring of Living Ore | Ring of Living Ore | Ring of the Depths OPEN'
        // [Thu Jan 01 23:09:16 2026] You tell your raid, 'A Glowing Orb of Luclinite (6) | Brilliant Stone Ring | Ring of Living Ore (2) | Ring of the Depths OPEN'
        // [Thu Jan 01 23:09:16 2026] You tell your raid, 'Ring of the Depths x2 OPEN'

        if (playerMessage.Contains(Constants.PossibleErrorDelimiter))
        {
            int multiplier = GetMultiplier(playerMessage, out string multiplierDeclaration);
            string auctionLine = _sanitizer.SanitizeDelimiterString(playerMessage);
            string[] lineParts = auctionLine.Split(Constants.AttendanceDelimiter, StringSplitOptions.RemoveEmptyEntries);
            if (lineParts.Length > 1)
            {
                string itemName = lineParts[0].Trim();
                LiveAuctionInfo newAuction = new()
                {
                    Timestamp = timeStamp,
                    Channel = channel,
                    Auctioneer = messageSender,
                    ItemName = itemName,
                    TotalNumberOfItems = multiplier,
                };
                Log.Debug($"{LogPrefix} New auction: {newAuction}");
                return [newAuction];
            }
        }
        else
        {
            int indexOfOpen = playerMessage.IndexOf("OPEN");
            string itemsString = playerMessage[0..(indexOfOpen - 1)];
            char delimiter = itemsString.Contains('|') ? '|' : ',';
            string[] itemNames = itemsString.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            if (itemNames.Length == 0)
                return [];

            List<LiveAuctionInfo> auctions = new(itemNames.Length);
            foreach (string itemName in itemNames)
            {
                int multiplier = GetMultiplier(itemName, out string multiplierDeclaration);
                string itemNameWithoutMultiplier = itemName;
                if (multiplier > 1)
                    itemNameWithoutMultiplier = itemNameWithoutMultiplier.Replace(multiplierDeclaration, "");

                auctions.Add(new LiveAuctionInfo
                {
                    Timestamp = timeStamp,
                    Channel = channel,
                    Auctioneer = messageSender,
                    ItemName = itemNameWithoutMultiplier.Trim(),
                    TotalNumberOfItems = multiplier,
                });
            }

            var auctionGrouping = auctions.GroupBy(x => x.ItemName)
                .Select(a => new { ItemCount = Math.Max(a.Count(), a.Max(x => x.TotalNumberOfItems)), Auction = a.First() })
                .ToList();

            auctions.Clear();
            foreach (var auctionGroup in auctionGrouping)
            {
                //if(auctionGroup.ItemCount > 1)
                    auctionGroup.Auction.TotalNumberOfItems = auctionGroup.ItemCount;
                Log.Debug($"{LogPrefix} New auction: {auctionGroup.Auction}");
                auctions.Add(auctionGroup.Auction);
            }

            return auctions;
        }

        return [];
    }

    private ICollection<LiveAuctionInfo> HandleRoll(string playerMessage, EqChannel channel, DateTime timeStamp, string messageSender)
    {
        // You tell your raid, 'Runed Bolster Belt 333 ROLL'
        // You tell your raid, 'Runed Bolster Belt x2 333 ROLL'

        int multiplier = GetMultiplier(playerMessage, out string multiplierDeclaration);

        string messageWithoutMultiplier = playerMessage;
        if (multiplier > 1)
            messageWithoutMultiplier = messageWithoutMultiplier.Replace(multiplierDeclaration, "");

        int randNumber = GetRandNumber(messageWithoutMultiplier);
        if (randNumber < 1)
            return [];

        int endIndex = messageWithoutMultiplier.IndexOf(randNumber.ToString());

        // Item or reward name is at least 3 chars, 7 is the length of ROLL + space + at least 1 digit + space
        if (endIndex < 3 || endIndex > (messageWithoutMultiplier.Length - 7))
            return [];

        string randName = messageWithoutMultiplier[..endIndex].Trim();

        return [new LiveAuctionInfo
            {
                Timestamp = timeStamp,
                Channel = channel,
                Auctioneer = messageSender,
                ItemName = randName,
                TotalNumberOfItems = multiplier,
                RandValue = randNumber,
                IsRoll = true
            }];
    }
}
