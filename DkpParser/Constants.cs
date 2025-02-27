// -----------------------------------------------------------------------
// Constants.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Globalization;

public static class Constants
{
    public const string Afk = " AFK ";
    public const string AfkAlternateDelimiter = $"{AlternateDelimiter}{Afk}{AlternateDelimiter}";
    public const string AfkEnd = "AFKEND";
    public const string AfkEndAlternateDelimiter = $"{AlternateDelimiter}{AfkEnd}{AlternateDelimiter}";
    public const string AfkEndWithDelimiter = $"{AttendanceDelimiter}{AfkEnd}{AttendanceDelimiter}";
    public const string AfkWithDelimiter = $"{AttendanceDelimiter}{Afk}{AttendanceDelimiter}";
    public const string AlternateDelimiter = "+";
    public const string AnonWithBrackets = "[ANONYMOUS]";
    /// <summary>
    /// Used in the file-archiving functionality.
    /// </summary>
    public const string ArchiveFileNameTimeFormat = "yyyyMMdd-HHmmss";
    public const string Attendance = "Attendance";
    public const string AttendanceDelimiter = ":::";
    // [Wed Mar 13 20:37:58 2024] Musse auctions, 'xxx - CH - Eorderth'
    public const string AuctionOther = " auctions, '";
    public const string AuctionYou = "You auction, '";
    public const string CommunicationFileNamePrefix = "CommunicationOutput-";
    public const string ConversationFileNamePrefix = "ConversationOutput-";
    public const string Crashed = "CRASHED";
    public const string CrashedAlternateDelimiter = $"{AlternateDelimiter}{Crashed}{AlternateDelimiter}";
    public const string CrashedWithDelimiter = $"{AttendanceDelimiter}{Crashed}{AttendanceDelimiter}";
    public const string Dashes = "---------------------------";
    public const string DkpSpent = "SPENT";  // Used to be DKPSPENT, but was changed to SPENT due to typos by users
    public const string DoubleDash = "--";
    public const string EndLootedDashes = ".--";
    public const string EqLogDateTimeFormat = "[ddd MMM dd HH:mm:ss yyyy]";
    public const string EqLogSearchPattern = "*eqlog*.txt";
    public const string EqProcessName = "eqgame";
    public const string FullGeneratedLogFileNamePrefix = "FullLogOutput-";
    public const string GeneratedLogFileNamePrefix = "GeneratedDkpLog-eqlog-";
    public const string GroupOther = " tells the group,";
    public const string GroupYou = "You tell your party, '";
    public const string GuildName = "Europa";
    // [Tue Mar 19 20:15:09 2024] Aaeien tells the guild, 'we have 7 rangers and 2 mages... if that helps with the decision-making :)'
    public const string GuildOther = " tells the guild, '";
    public const string GuildTag = $"<{GuildName}>";
    public const string GuildYou = "You say to your guild, '";
    // [Wed Mar 13 20:32:16 2024] You say to your guild, 'Yes, a number of us are LFG at the book for a raid invite.'
    public const string GuildYouSearch = $"] {GuildYou}";
    public const string JoinedRaid = " joined the raid.";
    public const string KillCall = "KILL";
    public const string LeftRaid = " has left the raid.";
    public const string LootedA = " looted a ";
    public const int MinimumRaidNameLength = 5;
    public const string NotReady = "NOTREADY";
    public const string NotReadyAlternateDelimiter = $"{AlternateDelimiter}{NotReady}{AlternateDelimiter}";
    public const string NotReadyWithDelimiter = $"{AttendanceDelimiter}{NotReady}{AttendanceDelimiter}";
    // [Tue Mar 19 20:13:39 2024] Slebog says out of character, '>> an azarack has been SLOWED  <<'
    public const string OocOther = " says out of character, '";
    public const string OocYou = "You say out of character, '";
    // [Tue Mar 19 20:13:26 2024] You say out of character, 'Attempting to Mezz ----> an azarack <----'
    public const string OocYouSearch = $"] {OocYou}";
    public const string PlayerIn = " player in ";
    /// <summary>
    /// Entry for noting the number of players and zone.  If there is one player returned, it will say "... player in...".<br/>
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
    // When someone else does an /rs message, it has 2 spaces after the comma (clearly a cosmetic bug in the game itself).
    // Dont add the single quote to this string in case it is "fixed" at some point.
    public const string RaidOther = " tells the raid, ";
    // [Sun Mar 17 23:18:28 2024] You tell your raid, ':::Raid Attendance Taken:::Sister of the Spire:::Kill:::'
    public const string RaidYou = "You tell your raid, '";
    public const string Ready = "READY";
    public const string ReadyAlternateDelimiter = $"{AlternateDelimiter}{Ready}{AlternateDelimiter}";
    public const string ReadyCheck = "READYCHECK";
    public const string ReadyCheckAlternateDelimiter = $"{AlternateDelimiter}{ReadyCheck}{AlternateDelimiter}";
    public const string ReadyCheckWithDelimiter = $"{AttendanceDelimiter}{ReadyCheck}{AttendanceDelimiter}";
    public const string ReadyWithDelimiter = $"{AttendanceDelimiter}{Ready}{AttendanceDelimiter}";
    public const string Remove = "REMOVE";
    public const string RollWin = "WINS";
    public const string Rot = "ROT";
    public const string SearchTermFileNamePrefix = "SearchTermOutput-";
    public const string StandardDateTimeDisplayFormat = "yyyy-MM-dd HH:mm:ss";
    public const string TellsYou = " tells you,";
    public const string TimePickerDisplayDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string TooLongDelimiter = "::::";
    public const string Transfer = "TRANSFER";
    public const string Undo = "UNDO";
    public const string UploadDebugInfoFileNamePrefix = "DEBUGUploadInfo-";
    public const string WhoZonePrefixPlural = "There are ";
    public const string WhoZonePrefixSingle = "There is ";
    public const string YouTold = "You told ";
    public const string YouToldSearch = $"] {YouTold}";
    public const string ZealAttendanceBasedFileName = "ZealRaidAttendance_";
    public const string ZealAttendanceBasedFileNameFormat = ZealAttendanceBasedFileName + "{0}.txt";
    public const int ZealPipeBufferSize = 32768;
    /// <summary>
    /// One argument for format: Process ID
    /// </summary>
    public const string ZealPipeNameFormat = ZealPipeNamePrefix + "_{0}";
    public const string ZealPipeNamePrefix = "zeal";
    public const string ZealRaidAttendanceFileNameTimeFormat = "yyyy-MM-dd_HH-mm-ss";
    public static readonly TimeSpan DurationOfSearch = TimeSpan.FromSeconds(10);
    public static readonly int LogDateTimeLength = EqLogDateTimeFormat.Length;
    public static readonly CultureInfo UsCulture = new("en-US");
}
