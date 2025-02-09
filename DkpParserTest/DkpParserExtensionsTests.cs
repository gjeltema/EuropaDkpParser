// -----------------------------------------------------------------------
// DkpParserExtensionsTests.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParserTest;

using DkpParser;

[TestFixture]
internal sealed class DkpParserExtensionsTests
{
    [TestCase("", "")]
    [TestCase(": : : \tAFK END : : :", ":::AFKEND:::")]
    [TestCase($": : : AFK END\r\n : : :", ":::AFKEND:::")]
    public void RemoveAllWhitespace_WhenCalled_RemovesAllWhitespace(string testLine, string expectedLine)
    {
        string actualLine = testLine.RemoveAllWhitespace();
        Assert.That(actualLine, Is.EqualTo(expectedLine));
    }
}
