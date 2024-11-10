// -----------------------------------------------------------------------
// ActiveAuctionStartAnalyzerTests.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParserTest;

using DkpParser;
using DkpParser.LiveTracking;

[TestFixture]
internal sealed class ActiveAuctionStartAnalyzerTests
{
    private ActiveAuctionStartAnalyzer _systemUnderTest;

    [TestCase("[Fri Nov 01 22:22:06 2024] Flubs tells the raid,  'Sprinkler of Suffering, Star of the Guardian OPEN for bids'",
        "Flubs", EqChannel.Raid, 2, "Sprinkler of Suffering", "Star of the Guardian")]
    [TestCase("[Fri Nov 01 22:22:06 2024] You tell your raid, 'Sprinkler of Suffering, Star of the Guardian, Crystalline Spear OPEN for bids'",
        "You", EqChannel.Raid, 3, "Sprinkler of Suffering", "Star of the Guardian", "Crystalline Spear")]
    public void GetAuctionStart_WithMultipleItems_ReturnsExpectedValue(string logLine, string auctioneer, EqChannel channel, int numberOfItems, params string[] itemNames)
    {
        logLine.TryExtractEqLogTimeStamp(out DateTime timestamp);
        string logLineNoTimestamp = logLine[(Constants.LogDateTimeLength + 1)..];
        ICollection<LiveAuctionInfo> rawResults = _systemUnderTest.GetAuctionStart(logLineNoTimestamp, channel, timestamp);
        List<LiveAuctionInfo> results = rawResults.ToList();

        Assert.That(results, Has.Count.EqualTo(numberOfItems));

        for (int i = 0; i < numberOfItems; i++)
        {
            LiveAuctionInfo result = results[i];
            Assert.Multiple(() =>
            {
                Assert.That(result.Auctioneer, Is.EqualTo(auctioneer));
                Assert.That(result.Channel, Is.EqualTo(channel));
                Assert.That(result.Timestamp, Is.EqualTo(timestamp));
                Assert.That(result.ItemName, Is.EqualTo(itemNames[i]));
                Assert.That(result.TotalNumberOfItems, Is.EqualTo(1));
            });
        }
    }

    [TestCase("[Fri Nov 01 22:22:06 2024] Flubs tells the raid,  'Sprinkler of Suffering, Star of the Guardian, Sprinkler of Suffering OPEN for bids'",
        "Flubs", EqChannel.Raid, 2, "Sprinkler of Suffering", "Star of the Guardian")]
    [TestCase("[Fri Nov 01 22:22:06 2024] You tell your raid, 'Sprinkler of Suffering, Star of the Guardian, Crystalline Spear, Sprinkler of Suffering OPEN for bids'",
        "You", EqChannel.Raid, 3, "Sprinkler of Suffering", "Star of the Guardian", "Crystalline Spear")]
    public void GetAuctionStart_WithMultipleItemsDuplicated_ReturnsExpectedValue(string logLine, string auctioneer, EqChannel channel, int numberOfItems, params string[] itemNames)
    {
        // hardcoding the amounts for now
        List<int> counts = [2, 1, 1];

        logLine.TryExtractEqLogTimeStamp(out DateTime timestamp);
        string logLineNoTimestamp = logLine[(Constants.LogDateTimeLength + 1)..];
        ICollection<LiveAuctionInfo> rawResults = _systemUnderTest.GetAuctionStart(logLineNoTimestamp, channel, timestamp);
        List<LiveAuctionInfo> results = rawResults.ToList();

        Assert.That(results, Has.Count.EqualTo(numberOfItems));

        for (int i = 0; i < numberOfItems; i++)
        {
            LiveAuctionInfo result = results[i];
            Assert.Multiple(() =>
            {
                Assert.That(result.Auctioneer, Is.EqualTo(auctioneer));
                Assert.That(result.Channel, Is.EqualTo(channel));
                Assert.That(result.Timestamp, Is.EqualTo(timestamp));
                Assert.That(result.ItemName, Is.EqualTo(itemNames[i]));
                Assert.That(result.TotalNumberOfItems, Is.EqualTo(counts[i]));
            });
        }
    }

    [TestCase("[Fri Nov 01 23:13:39 2024] You tell your raid, ':::Crystalline Spear::: BIDS OPEN'",
        "You", EqChannel.Raid, "Crystalline Spear", 1)]
    [TestCase("[Fri Nov 01 20:58:08 2024] Ghalone tells the raid,  '::: Robe of Primal Force ::: BIDS OPEN X2'",
        "Ghalone", EqChannel.Raid, "Robe of Primal Force", 2)]
    [TestCase("[Fri Nov 01 20:37:59 2024] You tell your raid, ':::Lyran's Mystical Lute::: BIDS OPEN x3'",
        "You", EqChannel.Raid, "Lyran's Mystical Lute", 3)]
    [TestCase("[Fri Nov 01 22:22:06 2024] Flubs tells the raid,  'Sprinkler of Suffering OPEN for bids'",
        "Flubs", EqChannel.Raid, "Sprinkler of Suffering", 1)]
    [TestCase("[Fri Nov 01 22:22:06 2024] Flubs tells the guild, 'Sprinkler of Suffering OPEN for bids'",
        "Flubs", EqChannel.Guild, "Sprinkler of Suffering", 1)]
    [TestCase("[Fri Nov 01 22:22:06 2024] Flubs auctions, 'Sprinkler of Suffering OPEN for bids'",
        "Flubs", EqChannel.Auction, "Sprinkler of Suffering", 1)]
    [TestCase("[Fri Nov 01 22:22:06 2024] Flubs says out of character, 'Sprinkler of Suffering OPEN for bids'",
        "Flubs", EqChannel.Ooc, "Sprinkler of Suffering", 1)]
    [TestCase("[Fri Nov 01 22:22:06 2024] You tell your raid, 'Sprinkler of Suffering OPEN for bids'",
        "You", EqChannel.Raid, "Sprinkler of Suffering", 1)]
    [TestCase("[Fri Nov 01 22:22:06 2024] You say to your guild, 'Sprinkler of Suffering OPEN for bids'",
        "You", EqChannel.Guild, "Sprinkler of Suffering", 1)]
    [TestCase("[Fri Nov 01 22:22:06 2024] You auctions, 'Sprinkler of Suffering OPEN for bids'",
        "You", EqChannel.Auction, "Sprinkler of Suffering", 1)]
    [TestCase("[Fri Nov 01 22:22:06 2024] You say out of character, 'Sprinkler of Suffering OPEN for bids'",
        "You", EqChannel.Ooc, "Sprinkler of Suffering", 1)]
    public void GetAuctionStart_WithSingleItem_ReturnsExpectedValue(string logLine, string auctioneer, EqChannel channel, string itemName, int numberOfItems)
    {
        logLine.TryExtractEqLogTimeStamp(out DateTime timestamp);
        string logLineNoTimestamp = logLine[(Constants.LogDateTimeLength + 1)..];
        ICollection<LiveAuctionInfo> results = _systemUnderTest.GetAuctionStart(logLineNoTimestamp, channel, timestamp);

        Assert.That(results, Has.Count.EqualTo(1));
        LiveAuctionInfo result = results.First();

        Assert.Multiple(() =>
        {
            Assert.That(result.Auctioneer, Is.EqualTo(auctioneer));
            Assert.That(result.Channel, Is.EqualTo(channel));
            Assert.That(result.Timestamp, Is.EqualTo(timestamp));
            Assert.That(result.ItemName, Is.EqualTo(itemName));
            Assert.That(result.TotalNumberOfItems, Is.EqualTo(numberOfItems));
        });
    }

    [SetUp]
    public void SetUp()
    {
        _systemUnderTest = new ActiveAuctionStartAnalyzer();
    }
}
