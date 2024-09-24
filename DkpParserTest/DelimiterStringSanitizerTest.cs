// -----------------------------------------------------------------------
// DelimiterStringSanitizerTest.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParserTest;

using DkpParser;

[TestFixture]
internal sealed class DelimiterStringSanitizerTest
{
    private DelimiterStringSanitizer _systemUnderTest;

    [TestCase("[Thu Mar 07 21:39:57 2024] Undertree tells the raid,  '::;Golden Coffer::Wonder 3 DKPSPENT'",
        "[Thu Mar 07 21:39:57 2024] Undertree tells the raid,  ':::Golden Coffer:::Wonder 3 DKPSPENT'")]
    [TestCase("[Thu Mar 07 21:39:57 2024] Undertree tells the raid,  '::Golden Coffer::Wonder 3 DKPSPENT'",
        "[Thu Mar 07 21:39:57 2024] Undertree tells the raid,  ':::Golden Coffer:::Wonder 3 DKPSPENT'")]
    [TestCase("[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  '::Raid Attendance Taken::Attendance::Fifth Call::'",
        "[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'")]
    [TestCase("[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  '::Raid Attendance Taken::::Attendance::Fifth Call::'",
        "[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'")]
    [TestCase("[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  '::Raid Attendance Taken:::Attendance::Fifth Call::::'",
        "[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'")]
    [TestCase("[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  '::Raid Attendance Taken::Attendance::::Fifth Call::;'",
        "[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'")]
    [TestCase("[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  ':;:Raid Attendance Taken::Attendance::::Fifth Call::;'",
        "[Sun Mar 10 22:00:30 2024] Undertree tells the raid,  ':::Raid Attendance Taken:::Attendance:::Fifth Call:::'")]
    public void SanitizeDelimiterString_SanitizesString(string inputString, string expectedResult)
    {
        string output = _systemUnderTest.SanitizeDelimiterString(inputString);
        Assert.That(output, Is.EqualTo(expectedResult));
    }

    [SetUp]
    public void SetUp()
    {
        _systemUnderTest = new DelimiterStringSanitizer();
    }
}
