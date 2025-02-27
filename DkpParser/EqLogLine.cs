// -----------------------------------------------------------------------
// EqLogLine.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public static class EqLogLine
{
    public static string CharacterListing(DateTime timeStamp, string characterName, string characterRace, int level, string className, bool isAnonymous)
    {
        string race = string.IsNullOrEmpty(characterRace) ? "Unknown" : characterRace;

        // Two spaces before the guild name are intentional when anonymous, it's how the game does it
        return isAnonymous
            ? $"{ToEqlogTimestamp(timeStamp)} {Constants.AnonWithBrackets} {characterName}  <Europa>"
            : $"{ToEqlogTimestamp(timeStamp)} [{level} {className}] {characterName} ({race}) <Europa>";
    }

    public static string LogMessage(DateTime timestamp, string message)
        => $"{ToEqlogTimestamp(timestamp)} {message}";

    public static string OtherTellsAuction(DateTime timestamp, string characterName, string message)
        => $"{ToEqlogTimestamp(timestamp)} {characterName}{Constants.AuctionOther}{message}'";

    public static string OtherTellsGuild(DateTime timestamp, string characterName, string message)
        => $"{ToEqlogTimestamp(timestamp)} {characterName}{Constants.GuildOther}{message}'";

    public static string OtherTellsOoc(DateTime timestamp, string characterName, string message)
        => $"{ToEqlogTimestamp(timestamp)} {characterName}{Constants.OocOther}{message}'";

    // Adding a space and ' here after Constants.RaidOther.  Currently the game has a bug where it has 2 spaces afterwards.
    // Keeping the constant with one space in case they ever fix it.  If they do ever fix it, need to remove this space.
    /// <summary>
    /// [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  ':::Belt of the Pine::: huggin 3 DKPSPENT'
    /// </summary>
    public static string OtherTellsRaid(DateTime timestamp, string characterName, string message)
        => $"{ToEqlogTimestamp(timestamp)} {characterName}{Constants.RaidOther} '{message}'";

    public static string YouTellAuction(DateTime timestamp, string message)
        => $"{ToEqlogTimestamp(timestamp)} {Constants.AuctionYou}{message}'";

    public static string YouTellGuild(DateTime timestamp, string message)
        => $"{ToEqlogTimestamp(timestamp)} {Constants.GuildYou}{message}'";

    public static string YouTellOoc(DateTime timestamp, string message)
        => $"{ToEqlogTimestamp(timestamp)} {Constants.OocYou}{message}'";

    /// <summary>
    /// [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
    /// </summary>
    public static string YouTellRaid(DateTime timestamp, string message)
        => $"{ToEqlogTimestamp(timestamp)} {Constants.RaidYou}{message}'";

    /// <summary>
    /// [Tue Mar 19 21:35:23 2024] There are 51 players in Plane of Sky.
    /// </summary>
    public static string ZonePlayers(DateTime timestamp, int playerCount, string zoneName)
        => $"{ToEqlogTimestamp(timestamp)} There are {playerCount} players in {zoneName}.";

    private static string ToEqlogTimestamp(DateTime timeStamp)
        => timeStamp.ToString(Constants.EqLogDateTimeFormat, Constants.UsCulture);
}
