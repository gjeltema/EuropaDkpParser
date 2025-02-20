// -----------------------------------------------------------------------
// ActiveAuctionEndAnalyzerTests.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParserTest;

using DkpParser;
using DkpParser.LiveTracking;

[TestFixture]
internal sealed class ActiveAuctionEndAnalyzerTests
{
    private ActiveAuctionEndAnalyzer _systemUnderTest;

    [TestCase(" :::Runed Bolster Belt::: ROT'", "Runed Bolster Belt", "Balm")]
    [TestCase(":::Shroud of Veeshan::: ROT '", "Shroud of Veeshan", "Galena")]
    public void GetSpentCall_WhenCalledWithRot_ReturnsRotEntry(string logLine, string itemName, string auctioneer)
    {
        DateTime timestamp = DateTime.Now;
        LiveSpentCall actual = _systemUnderTest.GetSpentCall(logLine, EqChannel.Raid, timestamp, auctioneer);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Auctioneer, Is.EqualTo(auctioneer));
            Assert.That(actual.Timestamp, Is.EqualTo(timestamp));
            Assert.That(actual.DkpSpent, Is.Zero);
            Assert.That(actual.IsRemoveCall, Is.False);
            Assert.That(actual.Winner, Is.EqualTo(Constants.Rot));
            Assert.That(actual.ItemName, Is.EqualTo(itemName));
        });
    }

    [SetUp]
    public void SetUp()
    {
        _systemUnderTest = new ActiveAuctionEndAnalyzer();
    }
}
