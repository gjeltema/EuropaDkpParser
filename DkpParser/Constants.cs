// -----------------------------------------------------------------------
// Constants.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public static class Constants
{
    public const string Attendance = "Attendance";
    public const string AttendanceDelimiter = ":::";
    public const string Dashes = "---------------------------";
    public const string DkpSpent = "DKPSPENT";
    public const string DoubleDash = "--";
    public const string EndLootedDashes = ".--";
    public const string EuropaGuildTag = "<Europa>";
    public const string KillCall = "KILL";
    public const string LogDateTimeFormat = "[ddd MMM dd HH:mm:ss yyyy]";
    public const string Looted = "looted";
    public const string LootedA = " looted a ";
    /// <summary>
    /// Entry for noting the number of players and zone.  Assuming for a raid call, it will always be plural.  If there is one player returned, it will say "... player in...".<br/>
    /// [Tue Mar 19 23:24:25 2024] There are 43 players in Plane of Sky.
    /// </summary>
    public const string PlayersIn = " players in ";
    public const string PlayersOnEverquest = "Players on EverQuest:";
    public const string PossibleErrorDelimiter = "::";
    public const string RaidAttendanceTaken = "Raid Attendance Taken";
    public const string RaidDumpFileNameTimeFormat = "yyyyMMdd-HHmmss";
    public const string RaidListFileNameTimeFormat = "yyyy-MM-dd_HH-mm-ss";
    public const string Remove = "REMOVE";
    public const string TellsYou = "tells you";
    public const string TooLongDelimiter = "::::";
    //public const string TypicalTimestamp = "[Tue Mar 19 23:24:25 2024]";
    public const string Undo = "UNDO";
    public const string WhoZonePrefixPlural = "There are ";
    public const string WhoZonePrefixSingle = "There is ";
    public const string YouTold = "You told";
    public static readonly TimeSpan DurationOfSearch = TimeSpan.FromSeconds(2);
    public static readonly int LogDateTimeLength = LogDateTimeFormat.Length;
}
