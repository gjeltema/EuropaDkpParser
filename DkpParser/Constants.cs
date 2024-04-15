// -----------------------------------------------------------------------
// Constants.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Globalization;

public static class Constants
{
    public const string Anonymous = "ANONYMOUS";
    /// <summary>
    /// Used in the file-archiving functionality.
    /// </summary>
    public const string ArchiveFileNameTimeFormat = "yyyyMMdd-HHmmss";
    public const string Attendance = "Attendance";
    public const string AttendanceDelimiter = ":::";
    public const string CommunicationFileNamePrefix = "CommunicationOutput-";
    public const string ConversationFileNamePrefix = "ConversationOutput-";
    public const string Dashes = "---------------------------";
    public const string DkpSpent = "DKPSPENT";
    public const string DoubleDash = "--";
    public const string EndLootedDashes = ".--";
    public const string EqLogSearchPattern = "*eqlog*.txt";
    public const string EuropaGuildTag = "<Europa>";
    public const string FullGeneratedLogFileNamePrefix = "FullLogOutput-";
    public const string GeneratedLogFileNamePrefix = "GeneratedDkpLog-";
    public const string JoinedRaid = " joined the raid.";
    public const string KillCall = "KILL";
    public const string LeftRaid = " has left the raid.";
    public const string LogDateTimeFormat = "[ddd MMM dd HH:mm:ss yyyy]";
    public const string LootedA = " looted a ";
    public const int MinimumRaidNameLength = 5;
    /// <summary>
    /// Entry for noting the number of players and zone.  Assuming for a raid call, it will always be plural.  If there is one player returned, it will say "... player in...".<br/>
    /// [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
    /// </summary>
    public const string PlayersIn = " players in ";
    public const string PlayersOnEverquest = "Players on EverQuest:";
    public const string PossibleErrorDelimiter = "::";
    public const string Raid = "raid.";
    public const string RaidAttendanceTaken = "Raid Attendance Taken";
    public const string RaidDumpFileNameStart = "RaidRoster-";
    public const string RaidDumpFileNameTimeFormat = "yyyyMMdd-HHmmss";
    public const string RaidListFileNameStart = "RaidTick-";
    public const string RaidListFileNameTimeFormat = "yyyy-MM-dd_HH-mm-ss";
    public const string Remove = "REMOVE";
    public const string SearchTermFileNamePrefix = "SearchTermOutput-";
    public const string StandardDateTimeDisplayFormat = "yyyy-MM-dd HH:mm:ss";
    public const string TellsYou = "tells you";
    public const string TooLongDelimiter = "::::";
    public const string Undo = "UNDO";
    public const string WhoZonePrefixPlural = "There are ";
    public const string WhoZonePrefixSingle = "There is ";
    public const string YouTold = "You told";
    public static readonly TimeSpan DurationOfSearch = TimeSpan.FromSeconds(2);
    public static readonly int LogDateTimeLength = LogDateTimeFormat.Length;
    public static readonly CultureInfo UsCulture = new("en-US");
}
