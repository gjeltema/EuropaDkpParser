// -----------------------------------------------------------------------
// ActiveBidTrackerTests.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParserTest;

using System.Collections.Generic;
using DkpParser;
using DkpParser.LiveTracking;
using Gjeltema.Logging;

[TestFixture]
internal sealed class ActiveBidTrackerTests
{
    private MessageProviderMock _messageProvider;
    private ActiveBidTracker _systemUnderTest;

    [TestCase()]
    public void Tracker_WhenBidCycleWith2ItemsAndRotSpent_HasExpectedState()
    {
        InitializeSystemUnderTest();
        _systemUnderTest.StartTracking("");
        _messageProvider.SendMessage("[Fri Nov 01 20:58:08 2024] Ghalone tells the raid,  '::: Robe of Primal Force ::: BIDS OPEN X2'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions.Count(), Is.EqualTo(1));
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(auction.Channel, Is.EqualTo(EqChannel.Raid));
            Assert.That(auction.Auctioneer, Is.EqualTo("Ghalone"));
            Assert.That(auction.ItemName, Is.EqualTo("Robe of Primal Force"));
            Assert.That(auction.TotalNumberOfItems, Is.EqualTo(2));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:38 2024] Undertree tells the raid,  'Robe of Primal Force uNdertree 20 DKP'");
        Assert.Multiple(() =>
        {
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(1));

            ICollection<LiveBidInfo> highBids = _systemUnderTest.GetHighBids(auction, false);
            Assert.That(highBids.Any(x => x.CharacterBeingBidFor == "Undertree"), Is.True);

            string statusMessage = _systemUnderTest.GetStatusMessage(auction, StatusMarker.SixtySeconds, false);
            Assert.That(statusMessage, Is.EqualTo($"/rs :::\u0012123456: Robe of Primal Force\u0012::: Undertree 20 DKP 60s"));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:41 2024] Ghalone tells the raid,  '::: Robe of Primal Force ::: uNdertree 20 SPENT'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:40 2024] Ghalone tells the raid,  '::: Robe of Primal Force ::: ROT'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions, Is.Empty);
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(1));

            IEnumerable<CompletedAuction> spents = _systemUnderTest.CompletedAuctions;
            Assert.That(spents.Count(), Is.EqualTo(1));

            CompletedAuction spentCalls = spents.First();
            Assert.That(spentCalls.SpentCalls, Has.Count.EqualTo(2));
            Assert.That(spentCalls.ItemName, Is.EqualTo("Robe of Primal Force"));
            Assert.That(spentCalls.SpentCalls.Any(x => x.Winner == Constants.Rot), Is.True);
            Assert.That(spentCalls.SpentCalls.Any(x => x.Winner == "Undertree"), Is.True);

            LiveSpentCall spentCallUndertree = spentCalls.SpentCalls.FirstOrDefault(x => x.Winner == "Undertree");
            string spentMessage = _systemUnderTest.GetSpentMessageWithLink(new SuggestedSpentCall
            {
                Winner = spentCallUndertree.Winner,
                DkpSpent = spentCallUndertree.DkpSpent,
                ItemName = spentCallUndertree.ItemName,
                Channel = spentCallUndertree.Channel,
            });
            Assert.That(spentMessage, Is.EqualTo($"/rs :::\u0012123456: Robe of Primal Force\u0012::: Undertree 20 SPENT"));
        });
    }

    [TestCase()]
    public void Tracker_WhenBidCycleWith2ItemsTranspires_HasExpectedState()
    {
        InitializeSystemUnderTest();
        _systemUnderTest.StartTracking("");
        _messageProvider.SendMessage("[Fri Nov 01 20:58:08 2024] Ghalone tells the raid,  '::: Robe of Primal Force ::: BIDS OPEN X2'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions.Count(), Is.EqualTo(1));
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(auction.Channel, Is.EqualTo(EqChannel.Raid));
            Assert.That(auction.Auctioneer, Is.EqualTo("Ghalone"));
            Assert.That(auction.ItemName, Is.EqualTo("Robe of Primal Force"));
            Assert.That(auction.TotalNumberOfItems, Is.EqualTo(2));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:35 2024] Bootscootin tells the raid,  'Robe of Primal Force kRizzy 10 DKP'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:36 2024] Luciania tells the raid,  'Robe of Primal Force lUciania 15 DKP'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:37 2024] Lebuffer tells the raid,  'Robe of Primal Force aListara 15 DKP'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:38 2024] Undertree tells the raid,  'Robe of Primal Force uNdertree 20 DKP'");
        Assert.Multiple(() =>
        {
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(4));

            ICollection<LiveBidInfo> highBids = _systemUnderTest.GetHighBids(auction, false);
            Assert.That(highBids.Any(x => x.CharacterBeingBidFor == "Luciania"), Is.True);
            Assert.That(highBids.Any(x => x.CharacterBeingBidFor == "Undertree"), Is.True);

            string statusMessage = _systemUnderTest.GetStatusMessage(auction, StatusMarker.SixtySeconds, false);
            Assert.That(statusMessage, Is.EqualTo($"/rs :::\u0012123456: Robe of Primal Force\u0012::: Undertree 20 DKP, Luciania 15 DKP 60s"));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:40 2024] Ghalone tells the raid,  '::: Robe of Primal Force ::: lUciania 15 SPENT'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:41 2024] Ghalone tells the raid,  '::: Robe of Primal Force ::: uNdertree 20 SPENT'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions, Is.Empty);
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(4));

            IEnumerable<CompletedAuction> spents = _systemUnderTest.CompletedAuctions;
            Assert.That(spents.Count(), Is.EqualTo(1));

            CompletedAuction spentCalls = spents.First();
            Assert.That(spentCalls.SpentCalls, Has.Count.EqualTo(2));
            Assert.That(spentCalls.ItemName, Is.EqualTo("Robe of Primal Force"));
            Assert.That(spentCalls.SpentCalls.Any(x => x.Winner == "Luciania"), Is.True);
            Assert.That(spentCalls.SpentCalls.Any(x => x.Winner == "Undertree"), Is.True);

            LiveSpentCall spentCallUndertree = spentCalls.SpentCalls.FirstOrDefault(x => x.Winner == "Undertree");
            string spentMessage = _systemUnderTest.GetSpentMessageWithLink(new SuggestedSpentCall
            {
                Winner = spentCallUndertree.Winner,
                DkpSpent = spentCallUndertree.DkpSpent,
                ItemName = spentCallUndertree.ItemName,
                Channel = spentCallUndertree.Channel,
            });
            Assert.That(spentMessage, Is.EqualTo($"/rs :::\u0012123456: Robe of Primal Force\u0012::: Undertree 20 SPENT"));
        });
    }

    [TestCase()]
    public void Tracker_WhenBidCycleWithMultipleItemsTranspires_HasExpectedState()
    {
        InitializeSystemUnderTest();
        _systemUnderTest.StartTracking("");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:30 2024] Ghalone tells the raid,  'Robe of Primal Force,Crystalline Spear,Robe of Primal Force OPEN'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions.Count(), Is.EqualTo(2));
            LiveAuctionInfo auctionRobe = _systemUnderTest.ActiveAuctions.First(x => x.ItemName == "Robe of Primal Force");
            Assert.That(auctionRobe.Channel, Is.EqualTo(EqChannel.Raid));
            Assert.That(auctionRobe.Auctioneer, Is.EqualTo("Ghalone"));
            Assert.That(auctionRobe.ItemName, Is.EqualTo("Robe of Primal Force"));
            Assert.That(auctionRobe.TotalNumberOfItems, Is.EqualTo(2));

            LiveAuctionInfo auctionSpear = _systemUnderTest.ActiveAuctions.First(x => x.ItemName == "Crystalline Spear");
            Assert.That(auctionSpear.Channel, Is.EqualTo(EqChannel.Raid));
            Assert.That(auctionSpear.Auctioneer, Is.EqualTo("Ghalone"));
            Assert.That(auctionSpear.ItemName, Is.EqualTo("Crystalline Spear"));
            Assert.That(auctionSpear.TotalNumberOfItems, Is.EqualTo(1));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:35 2024] Bootscootin tells the raid,  'Crystalline Spear Krizzy 10 DKP'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:36 2024] Luciania tells the raid,  'Robe of Primal Force Luciania 15 DKP'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:37 2024] Lebuffer tells the raid,  'Robe of Primal Force Alistara 15 DKP'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:38 2024] Undertree tells the raid,  'Robe of Primal Force Undertree 20 DKP'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:40 2024] Coyote tells the raid,  'Crystalline Spear Cyot 15 DKP'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(5));

            LiveAuctionInfo auctionSpear = _systemUnderTest.ActiveAuctions.First(x => x.ItemName == "Crystalline Spear");
            ICollection<LiveBidInfo> highBidsSpear = _systemUnderTest.GetHighBids(auctionSpear, false);
            Assert.That(highBidsSpear, Has.Count.EqualTo(1));
            Assert.That(highBidsSpear.Any(x => x.CharacterBeingBidFor == "Cyot"), Is.True);

            LiveAuctionInfo auctionRobe = _systemUnderTest.ActiveAuctions.First(x => x.ItemName == "Robe of Primal Force");
            ICollection<LiveBidInfo> highBidsRobe = _systemUnderTest.GetHighBids(auctionRobe, false);
            Assert.That(highBidsRobe, Has.Count.EqualTo(2));
            Assert.That(highBidsRobe.Any(x => x.CharacterBeingBidFor == "Undertree"), Is.True);
            Assert.That(highBidsRobe.Any(x => x.CharacterBeingBidFor == "Luciania"), Is.True);
        });
    }

    [TestCase()]
    public void Tracker_WhenBidWithDelimiter_ProcessesBidCorrectly()
    {
        InitializeSystemUnderTest();
        _systemUnderTest.StartTracking("");
        _messageProvider.SendMessage("[Fri Nov 01 20:58:08 2024] Ghalone tells the raid,  ':::Robe of Primal Force::: BIDS OPEN'");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:38 2024] Undertree tells the raid,  ':::Robe of Primal Force::: uNderPaID 20 DKP'");

        Assert.Multiple(() =>
        {
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(1));

            ICollection<LiveBidInfo> highBids = _systemUnderTest.GetHighBids(auction, false);
            Assert.That(highBids, Has.Count.EqualTo(1));
            LiveBidInfo bid = highBids.First();
            Assert.That(bid.CharacterBeingBidFor, Is.EqualTo("Underpaid"));
            Assert.That(bid.ParentAuctionId, Is.EqualTo(auction.Id));
            Assert.That(bid.CharacterPlacingBid, Is.EqualTo("Undertree"));
            Assert.That(bid.BidAmount, Is.EqualTo(20));
        });
    }

    [TestCase("[Mon Oct 28 21:55:00 2024] Druzzil Ro tells the guild, 'Argentia of Europa> has killed Lord Nagafen in Nagafen's Lair!'", "Lord Nagafen")]
    [TestCase("[Mon Oct 28 21:23:46 2024] Druzzil Ro tells the guild, 'Kadenn of Europa> has killed Innoruuk in Plane of Hate Instanced !'", "Innoruuk")]
    [TestCase("[Mon Oct 28 21:40:59 2024] Druzzil Ro tells the guild, 'Noname of Europa> has killed King Tranix in Nagafen's Lair!'", "King Tranix")]
    public void Tracker_WhenBossKillHappens_HasExpectedState(string logLine, string expectedBossName)
    {
        InitializeSystemUnderTest();
        _systemUnderTest.StartTracking("");

        Assert.That(_systemUnderTest.GetBossKilledName(), Is.Null);

        _messageProvider.SendMessage(logLine);

        string actualBossName = _systemUnderTest.GetBossKilledName();
        Assert.Multiple(() =>
        {
            Assert.That(actualBossName, Is.EqualTo(expectedBossName));
            Assert.That(_systemUnderTest.GetBossKilledName(), Is.Null);
        });
    }

    [TestCase()]
    public void Tracker_WhenNormalBidCycleTranspires_HasExpectedState()
    {
        InitializeSystemUnderTest();
        _systemUnderTest.StartTracking("");
        _messageProvider.SendMessage("[Fri Nov 01 23:13:39 2024] You tell your raid, ':::Crystalline Spear::: BIDS OPEN'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions.Count(), Is.EqualTo(1));
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(auction.Channel, Is.EqualTo(EqChannel.Raid));
            Assert.That(auction.Auctioneer, Is.EqualTo("You"));
            Assert.That(auction.ItemName, Is.EqualTo("Crystalline Spear"));
            Assert.That(auction.TotalNumberOfItems, Is.EqualTo(1));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:40 2024] Bootscootin tells the raid,  'Crystalline Spear Krizzy 10 DKP'");
        Assert.Multiple(() =>
        {
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(1));
            LiveBidInfo bid = _systemUnderTest.Bids.First();
            Assert.That(bid.ParentAuctionId, Is.EqualTo(auction.Id));
            Assert.That(bid.BidAmount, Is.EqualTo(10));
            Assert.That(bid.ItemName, Is.EqualTo("Crystalline Spear"));
            Assert.That(bid.CharacterBeingBidFor, Is.EqualTo("Krizzy"));
            Assert.That(bid.Channel, Is.EqualTo(EqChannel.Raid));

            ICollection<LiveBidInfo> highBids = _systemUnderTest.GetHighBids(auction, false);
            Assert.That(highBids, Has.Count.EqualTo(1));
            Assert.That(highBids.First(), Is.SameAs(bid));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:41 2024] You tell your raid, ':::Crystalline Spear::: Krizzy 10 DKP 30s'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions.Count(), Is.EqualTo(1));
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(1));

            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            ICollection<LiveBidInfo> highBids = _systemUnderTest.GetHighBids(auction, false);
            Assert.That(highBids, Has.Count.EqualTo(1));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:42 2024] Coyote tells the raid,  'Crystalline Spear Cyot 15 DKP'");
        Assert.Multiple(() =>
        {
            LiveAuctionInfo auction = _systemUnderTest.ActiveAuctions.First();
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(2));
            LiveBidInfo bid = _systemUnderTest.Bids.Skip(1).First();
            Assert.That(bid.ParentAuctionId, Is.EqualTo(auction.Id));
            Assert.That(bid.BidAmount, Is.EqualTo(15));
            Assert.That(bid.ItemName, Is.EqualTo("Crystalline Spear"));
            Assert.That(bid.CharacterBeingBidFor, Is.EqualTo("Cyot"));
            Assert.That(bid.Channel, Is.EqualTo(EqChannel.Raid));

            ICollection<LiveBidInfo> highBids = _systemUnderTest.GetHighBids(auction, false);
            Assert.That(highBids, Has.Count.EqualTo(1));
            Assert.That(highBids.First(), Is.SameAs(bid));
        });

        _messageProvider.SendMessage("[Fri Nov 01 23:13:43 2024] You tell your raid, ':::Crystalline Spear::: Cyot 15 DKPSPENT'");
        Assert.Multiple(() =>
        {
            Assert.That(_systemUnderTest.ActiveAuctions, Is.Empty);
            Assert.That(_systemUnderTest.Bids.Count(), Is.EqualTo(2));
            Assert.That(_systemUnderTest.CompletedAuctions.Count, Is.EqualTo(1));

            IEnumerable<CompletedAuction> spents = _systemUnderTest.CompletedAuctions;
            Assert.That(spents.Count(), Is.EqualTo(1));

            CompletedAuction spentCalls = spents.First();
            Assert.That(spentCalls.SpentCalls, Has.Count.EqualTo(1));
            Assert.That(spentCalls.ItemName, Is.EqualTo("Crystalline Spear"));

            LiveSpentCall spentCall = spentCalls.SpentCalls.First();
            Assert.That(spentCall.Winner, Is.EqualTo("Cyot"));
            Assert.That(spentCall.Channel, Is.EqualTo(EqChannel.Raid));
            Assert.That(spentCall.DkpSpent, Is.EqualTo(15));
            Assert.That(spentCall.Auctioneer, Is.EqualTo("You"));
            Assert.That(spentCall.IsRemoveCall, Is.False);
        });
    }

    private void InitializeSystemUnderTest()
    {
        ItemLinkValues itemLinkValues = new("");
        itemLinkValues.AddItemId("Robe of Primal Force", "123456");
        SettingsMock settings = new(itemLinkValues);
        _messageProvider = new();
        _systemUnderTest = new ActiveBidTracker(settings, _messageProvider);
    }
}

internal sealed class MessageProviderMock : IMessageProvider
{
    private Action<string> _lineHandler;

    public void SendMessage(string message)
        => _lineHandler(message);

    public void StartMessages(string filePath, Action<string> lineHandler, Action<string> errorMessage)
    {
        _lineHandler = lineHandler;
    }

    public void StopMessages()
    {
    }
}

internal sealed class SettingsMock : IDkpParserSettings
{
    internal SettingsMock(ItemLinkValues itemLinkValues)
    {
        ItemLinkIds = itemLinkValues;
    }

    public bool AddBonusDkpRaid { get; set; }

    public string ApiReadToken { get; set; }

    public string ApiUrl { get; set; }

    public string ApiWriteToken { get; set; }

    public bool ArchiveAllEqLogFiles { get; set; }

    public DkpServerCharacters CharactersOnDkpServer { get; } = new DkpServerCharacters("");

    public bool DkpspentAucEnabled { get; set; }

    public bool DkpspentGuEnabled { get; set; }

    public bool DkpspentOocEnabled { get; set; }

    public bool EnableDebugOptions { get; set; }

    public string EqDirectory { get; set; }

    public int EqLogFileAgeToArchiveInDays { get; set; }

    public string EqLogFileArchiveDirectory { get; set; }

    public int EqLogFileSizeToArchiveInMBs { get; set; }

    public ICollection<string> EqLogFilesToArchive { get; set; }

    public int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    public string GeneratedLogFilesArchiveDirectory { get; set; }

    public bool IncludeTellsInRawLog { get; set; }

    public bool IsApiConfigured { get; }

    public ItemLinkValues ItemLinkIds { get; }

    public string LogFileMatchPattern { get; set; }

    public LogLevel LoggingLevel { get; set; }

    public int MainWindowX { get; set; }

    public int MainWindowY { get; set; }

    public string OutputDirectory { get; set; } = "C:\\";

    public string OverlayFontColor { get; set; }

    public int OverlayFontSize { get; set; }

    public int OverlayLocationX { get; set; }

    public int OverlayLocationY { get; set; }

    public IRaidValues RaidValue { get; }

    public ICollection<string> SelectedLogFiles { get; set; }

    public bool ShowAfkReview { get; set; }

    public bool UseLightMode { get; set; }

    public IDictionary<int, string> ZoneIdMapping { get; }

    public void LoadAllSettings()
        => throw new NotImplementedException();

    public void SaveSettings()
        => throw new NotImplementedException();
}
