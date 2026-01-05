// -----------------------------------------------------------------------
// ActiveAuctionStartAnalyzerTests.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParserTest;

using DkpParser;
using DkpParser.LiveTracking;

[TestFixture]
internal sealed class ActiveAuctionStartAnalyzerTests
{
    private ActiveAuctionStartAnalyzer _systemUnderTest;

    [TestCase(" Sprinkler of Suffering, Star of the Guardian OPEN for bids'",
        "Voronus", EqChannel.Raid, 2, "Sprinkler of Suffering", "Star of the Guardian")]
    [TestCase("Sprinkler of Suffering, Star of the Guardian, Crystalline Spear OPEN for bids'",
        "You", EqChannel.Raid, 3, "Sprinkler of Suffering", "Star of the Guardian", "Crystalline Spear")]
    public void GetAuctionStart_WithMultipleItems_ReturnsExpectedValue(string logLine, string auctioneer, EqChannel channel, int numberOfItems, params string[] itemNames)
    {
        DateTime timeStamp = DateTime.Now;
        ICollection<LiveAuctionInfo> rawResults = _systemUnderTest.GetAuctionStart(logLine, channel, timeStamp, auctioneer);
        List<LiveAuctionInfo> results = rawResults.ToList();

        Assert.That(results, Has.Count.EqualTo(numberOfItems));

        for (int i = 0; i < numberOfItems; i++)
        {
            LiveAuctionInfo result = results[i];
            Assert.Multiple(() =>
            {
                Assert.That(result.Auctioneer, Is.EqualTo(auctioneer));
                Assert.That(result.Channel, Is.EqualTo(channel));
                Assert.That(result.Timestamp, Is.EqualTo(timeStamp));
                Assert.That(result.ItemName, Is.EqualTo(itemNames[i]));
                Assert.That(result.TotalNumberOfItems, Is.EqualTo(1));
            });
        }
    }

    [TestCase("Sprinkler of Suffering, Star of the Guardian, Sprinkler of Suffering OPEN for bids'",
        "Voronus", EqChannel.Raid, 2, "Sprinkler of Suffering", "Star of the Guardian")]
    [TestCase(" Sprinkler of Suffering, Star of the Guardian, Crystalline Spear, Sprinkler of Suffering OPEN'",
        "You", EqChannel.Raid, 3, "Sprinkler of Suffering", "Star of the Guardian", "Crystalline Spear")]
    public void GetAuctionStart_WithMultipleItemsDuplicated_ReturnsExpectedValue(string logLine, string auctioneer, EqChannel channel, int numberOfItems, params string[] itemNames)
    {
        // hardcoding the amounts for now
        List<int> counts = [2, 1, 1];

        DateTime timeStamp = DateTime.Now;
        ICollection<LiveAuctionInfo> rawResults = _systemUnderTest.GetAuctionStart(logLine, channel, timeStamp, auctioneer);
        List<LiveAuctionInfo> results = rawResults.ToList();

        Assert.That(results, Has.Count.EqualTo(numberOfItems));

        for (int i = 0; i < numberOfItems; i++)
        {
            LiveAuctionInfo result = results[i];
            Assert.Multiple(() =>
            {
                Assert.That(result.Auctioneer, Is.EqualTo(auctioneer));
                Assert.That(result.Channel, Is.EqualTo(channel));
                Assert.That(result.Timestamp, Is.EqualTo(timeStamp));
                Assert.That(result.ItemName, Is.EqualTo(itemNames[i]));
                Assert.That(result.TotalNumberOfItems, Is.EqualTo(counts[i]));
            });
        }
    }

    [TestCase("A Glowing Orb of Luclinite (6) | Brilliant Stone Ring | Ring of Living Ore x2 | Ring of the Depths OPEN'",
        "Kassandra", EqChannel.Raid, 4, "A Glowing Orb of Luclinite", "Brilliant Stone Ring", "Ring of Living Ore", "Ring of the Depths")]
    public void GetAuctionStart_WithMultipleItemsWithMultiplier_ReturnsExpectedValue(string logLine, string auctioneer, EqChannel channel, int numberOfItems, params string[] itemNames)
    {
        // hardcoding the amounts for now
        List<int> counts = [6, 1, 2, 1];

        DateTime timeStamp = DateTime.Now;
        ICollection<LiveAuctionInfo> rawResults = _systemUnderTest.GetAuctionStart(logLine, channel, timeStamp, auctioneer);
        List<LiveAuctionInfo> results = rawResults.ToList();

        Assert.That(results, Has.Count.EqualTo(numberOfItems));

        for (int i = 0; i < numberOfItems; i++)
        {
            LiveAuctionInfo result = results[i];
            Assert.Multiple(() =>
            {
                Assert.That(result.Auctioneer, Is.EqualTo(auctioneer));
                Assert.That(result.Channel, Is.EqualTo(channel));
                Assert.That(result.Timestamp, Is.EqualTo(timeStamp));
                Assert.That(result.ItemName, Is.EqualTo(itemNames[i]));
                Assert.That(result.TotalNumberOfItems, Is.EqualTo(counts[i]));
            });
        }
    }

    [TestCase(":::Crystalline Spear::: OPEN'", "You", EqChannel.Raid, "Crystalline Spear", 1)]
    [TestCase("::: Robe of Primal Force ::: OPEN X2'", "Ghalone", EqChannel.Raid, "Robe of Primal Force", 2)]
    [TestCase(":::Lyran's Mystical Lute::: OPEN x3'", "You", EqChannel.Raid, "Lyran's Mystical Lute", 3)]
    [TestCase("Sprinkler of Suffering OPEN for bids'", "Voronus", EqChannel.Raid, "Sprinkler of Suffering", 1)]
    [TestCase("Sprinkler of Suffering OPEN for bids'", "Voronus", EqChannel.Guild, "Sprinkler of Suffering", 1)]
    [TestCase("Sprinkler of Suffering OPEN for bids'", "You", EqChannel.Raid, "Sprinkler of Suffering", 1)]
    [TestCase("Sprinkler of Suffering OPEN for bids'", "You", EqChannel.Guild, "Sprinkler of Suffering", 1)]
    public void GetAuctionStart_WithSingleItem_ReturnsExpectedValue(string logLine, string auctioneer, EqChannel channel, string itemName, int numberOfItems)
    {
        DateTime timeStamp = DateTime.Now;
        ICollection<LiveAuctionInfo> results = _systemUnderTest.GetAuctionStart(logLine, channel, timeStamp, auctioneer);

        Assert.That(results, Has.Count.EqualTo(1));
        LiveAuctionInfo result = results.First();

        Assert.Multiple(() =>
        {
            Assert.That(result.Auctioneer, Is.EqualTo(auctioneer));
            Assert.That(result.Channel, Is.EqualTo(channel));
            Assert.That(result.Timestamp, Is.EqualTo(timeStamp));
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
