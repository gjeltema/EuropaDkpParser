// -----------------------------------------------------------------------
// ZealMessageProcessorTests.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParserTest;

using DkpParser.Zeal;

[TestFixture]
internal sealed class ZealMessageProcessorTests
{
    private ZealMessageProcessor _systemUnderTest;

    [TestCase(ZealMessageTestStrings.RaidMessageTest, ZealMessageTestStrings.RaidMessageLength, ZealMessageTestStrings.SecondRaidMessageTest, ZealMessageTestStrings.SecondRaidMessageLength, ZealMessageTestStrings.RaidMessageName)]
    public void ProcessMessage_WhenGivenTwoInputsWithDifferentRaidAttendees_UpdatesRaidAttendees(
        string firstMessage,
        int charsInFirstmessage,
        string secondMessage,
        int charsInSecondMessage,
        string currentCharacter)
    {
        InitializeSystemUnderTest();

        _systemUnderTest.ProcessMessage(firstMessage, charsInFirstmessage, currentCharacter);
        ICollection<ZealRaidCharacter> attendees = ZealAttendanceMessageProvider.Instance.RaidInfo.RaidAttendees;
        Assert.Multiple(() =>
        {
            Assert.That(attendees.Count, Is.EqualTo(6));
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Naddin"), Is.Not.Null);
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Naddin").Class, Is.EqualTo("Druid"));
        });

        _systemUnderTest.ProcessMessage(secondMessage, charsInSecondMessage, currentCharacter);
        attendees = ZealAttendanceMessageProvider.Instance.RaidInfo.RaidAttendees;
        Assert.Multiple(() =>
        {
            Assert.That(attendees.Count, Is.EqualTo(6));
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Niddin"), Is.Not.Null);
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Naddin"), Is.Null);
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Niddin").Class, Is.EqualTo("Rogue"));
        });

        _systemUnderTest.ProcessMessage(secondMessage, charsInSecondMessage, currentCharacter);
        attendees = ZealAttendanceMessageProvider.Instance.RaidInfo.RaidAttendees;
        Assert.Multiple(() =>
        {
            Assert.That(attendees.Count, Is.EqualTo(6));
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Niddin"), Is.Not.Null);
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Naddin"), Is.Null);
            Assert.That(attendees.FirstOrDefault(x => x.Name == "Niddin").Class, Is.EqualTo("Rogue"));
        });
    }

    [TestCase("""{"character":"Willam","data":"{"autoattack":false,"heading":4.0,"location":{"x":-9522.0,"y":3502.0,"z":3.859619140625},"zone":96}","data_len":99,"type":3}""",
        154, "Willam")]
    [TestCase(ZealMessageTestStrings.ThreeMessageTest, ZealMessageTestStrings.ThreeMessageLength, ZealMessageTestStrings.ThreeMessageName)]
    [TestCase(ZealMessageTestStrings.RaidMessageTest, ZealMessageTestStrings.RaidMessageLength, ZealMessageTestStrings.RaidMessageName)]
    public void ProcessMessage_WhenProcessingAMessage_HasExpectedValue(string message, int charsInMessage, string currentCharacter)
    {
        InitializeSystemUnderTest();
        _systemUnderTest.ProcessMessage(message, charsInMessage, currentCharacter);
    }

    private void InitializeSystemUnderTest()
    {
        _systemUnderTest = new ZealMessageProcessor(ZealAttendanceMessageProvider.Instance);
    }
}

internal static class ZealMessageTestStrings
{
    public const int RaidMessageLength = 971;
    public const string RaidMessageName = "Willam";
    public const string RaidMessageTest = """{"character":"Willam","data":"[{"class":"Druid","group":"1","heading":178.0,"level":"60","loc":{"x":-9337.017578125,"y":3217.962158203125,"z":10.893704414367676},"name":"Naddin","rank":""},{"class":"Monk","group":"1","heading":208.0,"level":"60","loc":{"x":-9362.0,"y":3208.0,"z":17.08617401123047},"name":"Reiter","rank":"Raid Leader"},{"class":"Magician","group":"10","heading":222.0,"level":"58","loc":{"x":-9312.0,"y":3192.0,"z":10.213265419006348},"name":"Misdabik","rank":""},{"class":"Shaman","group":"11","heading":228.0,"level":"55","loc":{"x":-9318.9765625,"y":3214.953369140625,"z":10.804616928100586},"name":"Slowin","rank":""},{"class":"Warrior","group":"2","heading":228.0,"level":"57","loc":{"x":-9361.0,"y":3216.0,"z":10.231949806213379},"name":"Krogard","rank":"Group Leader"},{"class":"Wizard","group":"3","heading":152.0,"level":"60","loc":{"x":-9356.0,"y":3213.0,"z":16.084571838378906},"name":"Zauki","rank":"Group Leader"}],"data_len":8434,"type":5}""";
    public const int SecondRaidMessageLength = 971;
    public const string SecondRaidMessageTest = """{"character":"Willam","data":"[{"class":"Rogue","group":"1","heading":178.0,"level":"60","loc":{"x":-9337.017578125,"y":3217.962158203125,"z":10.893704414367676},"name":"Niddin","rank":""},{"class":"Monk","group":"1","heading":208.0,"level":"60","loc":{"x":-9362.0,"y":3208.0,"z":17.08617401123047},"name":"Reiter","rank":"Raid Leader"},{"class":"Magician","group":"10","heading":222.0,"level":"58","loc":{"x":-9312.0,"y":3192.0,"z":10.213265419006348},"name":"Misdabik","rank":""},{"class":"Shaman","group":"11","heading":228.0,"level":"55","loc":{"x":-9318.9765625,"y":3214.953369140625,"z":10.804616928100586},"name":"Slowin","rank":""},{"class":"Warrior","group":"2","heading":228.0,"level":"57","loc":{"x":-9361.0,"y":3216.0,"z":10.231949806213379},"name":"Krogard","rank":"Group Leader"},{"class":"Wizard","group":"3","heading":152.0,"level":"60","loc":{"x":-9356.0,"y":3213.0,"z":16.084571838378906},"name":"Zauki","rank":"Group Leader"}],"data_len":8434,"type":5}""";
    public const int ThreeMessageLength = 4226;
    public const string ThreeMessageName = "Willam";
    public const string ThreeMessageTest = """{"character":"Willam","data":"[{"heading":222.0,"loc":{"x":-9312.0,"y":3192.0,"z":10.213265419006348},"name":"Misdabik"}]","data_len":91,"type":6}{"character":"Willam","data":"[{"meta":{},"type":1,"value":"Willam"},{"meta":{},"type":2,"value":"58"},{"meta":{},"type":3,"value":"Defiler"},{"meta":{},"type":4,"value":"Cazic-Thule"},{"meta":{},"type":5,"value":"96"},{"meta":{},"type":6,"value":"153"},{"meta":{},"type":7,"value":"128"},{"meta":{},"type":8,"value":"111"},{"meta":{},"type":9,"value":"147"},{"meta":{},"type":10,"value":"226"},{"meta":{},"type":11,"value":"61"},{"meta":{},"type":12,"value":"116"},{"meta":{},"type":13,"value":"55"},{"meta":{},"type":14,"value":"69"},{"meta":{},"type":15,"value":"40"},{"meta":{},"type":16,"value":"88"},{"meta":{},"type":17,"value":"1797"},{"meta":{},"type":18,"value":"1797"},{"meta":{},"type":19,"value":"100"},{"meta":{},"type":20,"value":"100"},{"meta":{},"type":21,"value":"100"},{"meta":{},"type":22,"value":"639"},{"meta":{},"type":23,"value":"108"},{"meta":{},"type":24,"value":"45"},{"meta":{},"type":25,"value":"96"},{"meta":{},"type":26,"value":"6"},{"meta":{},"type":27,"value":"0"},{"meta":{},"type":28,"value":""},{"meta":{},"type":29,"value":"0"},{"meta":{},"type":30,"value":"Misdabik"},{"meta":{},"type":31,"value":""},{"meta":{},"type":32,"value":""},{"meta":{},"type":33,"value":""},{"meta":{},"type":34,"value":""},{"meta":{},"type":35,"value":"100"},{"meta":{},"type":36,"value":"0"},{"meta":{},"type":37,"value":"0"},{"meta":{},"type":38,"value":"0"},{"meta":{},"type":39,"value":"0"},{"meta":{},"type":40,"value":"0"},{"meta":{},"type":41,"value":"0"},{"meta":{},"type":42,"value":"0"},{"meta":{},"type":43,"value":"0"},{"meta":{},"type":44,"value":"0"},{"meta":{"ticks":89},"type":45,"value":"Spirit of Wolf"},{"meta":{"ticks":157},"type":46,"value":"Dead Man Floating"},{"meta":{"ticks":102},"type":47,"value":"Clarity II"},{"meta":{"ticks":822},"type":48,"value":"Shield of the Magi"},{"meta":{"ticks":0},"type":49,"value":""},{"meta":{"ticks":0},"type":50,"value":""},{"meta":{"ticks":0},"type":51,"value":""},{"meta":{"ticks":0},"type":52,"value":""},{"meta":{"ticks":0},"type":53,"value":""},{"meta":{"ticks":0},"type":54,"value":""},{"meta":{"ticks":0},"type":55,"value":""},{"meta":{"ticks":0},"type":56,"value":""},{"meta":{"ticks":0},"type":57,"value":""},{"meta":{"ticks":0},"type":58,"value":""},{"meta":{"ticks":0},"type":59,"value":""},{"meta":{},"type":60,"value":"Vexing Mordinia"},{"meta":{},"type":61,"value":"Shadowbond"},{"meta":{},"type":62,"value":"Incinerate Bones"},{"meta":{},"type":63,"value":"Screaming Terror"},{"meta":{},"type":64,"value":"Demi Lich"},{"meta":{},"type":65,"value":"Servant of Bones"},{"meta":{},"type":66,"value":"Feign Death"},{"meta":{},"type":67,"value":"Gate"},{"meta":{},"type":68,"value":""},{"meta":{},"type":69,"value":"0"},{"meta":{},"type":70,"value":"1797/1797"},{"meta":{},"type":71,"value":"0"},{"meta":{},"type":72,"value":"0%"},{"meta":{},"type":73,"value":""},{"meta":{},"type":74,"value":" "},{"meta":{},"type":80,"value":"3191/3191"},{"meta":{},"type":81,"value":"0"},{"meta":{},"type":82,"value":""},{"meta":{},"type":124,"value":"3191"},{"meta":{},"type":125,"value":"3191"},{"meta":{},"type":134,"value":""}]","data_len":3071,"type":1}{"character":"Willam","data":"[{"text":"Willam","type":1,"value":999},{"text":"","type":2,"value":1000},{"text":"","type":3,"value":1000},{"text":"","type":4,"value":63},{"text":"","type":5,"value":0},{"text":"","type":6,"value":-1},{"text":"","type":7,"value":-1},{"text":"","type":8,"value":1000},{"text":"","type":9,"value":0},{"text":"","type":10,"value":0},{"text":"Misdabik","type":11,"value":999},{"text":"Xevuz*","type":12,"value":-2},{"text":"","type":13,"value":-1},{"text":"","type":14,"value":-1},{"text":"","type":15,"value":-1},{"text":"","type":16,"value":-3},{"text":"","type":17,"value":-3},{"text":"","type":18,"value":-3},{"text":"","type":19,"value":-3},{"text":"","type":20,"value":-3},{"text":"","type":21,"value":-3},{"text":"","type":23,"value":0}]","data_len":742,"type":2}{"character":"Willam","data":"{"autoattack":false,"heading":4.0,"location":{"x":-9522.0,"y":3502.0,"z":3.859619140625},"zone":96}","data_len":99,"type":3}""";
}
