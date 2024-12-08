// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class FileOutputGenerator : IOutputGenerator
{
    public IEnumerable<string> GenerateOutput(RaidEntries raidEntries, Func<string, string> getZoneRaidAlias)
    {
        var dkpEntries = raidEntries.DkpEntries.Select(x => new { AssociatedAttendance = raidEntries.GetAssociatedAttendance(x), Dkp = x }).ToList();

        string currentRaidZone = string.Empty;
        foreach (AttendanceEntry attendance in raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp))
        {
            string attendanceRaidZone = getZoneRaidAlias(attendance.ZoneName);
            if (currentRaidZone != attendanceRaidZone)
            {
                currentRaidZone = attendanceRaidZone;
                yield return Environment.NewLine;
                yield return EqLogLine.LogMessage(attendance.Timestamp.AddSeconds(-5), $"=========================== {currentRaidZone} ===========================");
            }

            foreach (string attendanceLine in CreateAttendanceEntry(attendance))
                yield return attendanceLine;

            foreach (var dkpEntryWithAttendance in dkpEntries.Where(x => x.AssociatedAttendance == attendance).OrderBy(x => x.Dkp.Timestamp))
            {
                yield return dkpEntryWithAttendance.Dkp.ToLogString();
            }
        }
    }

    private static IEnumerable<string> CreateAttendanceEntry(AttendanceEntry call)
    {
        /*
    [Tue Mar 19 21:35:36 2024] You tell your raid, ':::Raid Attendance Taken:::Attendance:::Fourth Call:::'
    [Tue Mar 19 21:35:23 2024] You tell your raid, ':::Raid Attendance Taken:::Keeper of Souls:::Kill:::'
    [Tue Mar 19 21:35:23 2024] Players on EverQuest:
    [Tue Mar 19 21:35:23 2024] ---------------------------
    [Tue Mar 19 21:35:23 2024] [50 Wizard] Coyote (Dark Elf) <Europa>
    [Tue Mar 19 21:35:23 2024] [50 Enchanter] Xeres (Dark Elf) <Europa>
    [Tue Mar 19 21:35:23 2024] [ANONYMOUS] Luciania  <Europa>
    [Tue Mar 19 21:35:23 2024] There are 51 players in Plane of Sky.
        */

        string header = call.AttendanceCallType == AttendanceCallType.Time
            ? $"{Constants.AttendanceDelimiter}{Constants.RaidAttendanceTaken}{Constants.AttendanceDelimiter}{Constants.Attendance}{Constants.AttendanceDelimiter}{call.CallName}{Constants.AttendanceDelimiter}"
            : $"{Constants.AttendanceDelimiter}{Constants.RaidAttendanceTaken}{Constants.AttendanceDelimiter}{call.CallName}{Constants.AttendanceDelimiter}{Constants.KillCall}{Constants.AttendanceDelimiter}";

        yield return EqLogLine.YouTellRaid(call.Timestamp, header);
        yield return EqLogLine.LogMessage(call.Timestamp, Constants.PlayersOnEverquest);
        yield return EqLogLine.LogMessage(call.Timestamp, Constants.Dashes);

        foreach (PlayerCharacter player in call.Characters.OrderBy(x => x.CharacterName))
        {
            yield return EqLogLine.CharacterListing(call.Timestamp, player.CharacterName, player.Race, player.Level, player.ClassName, player.IsAnonymous);
        }

        yield return EqLogLine.ZonePlayers(call.Timestamp, call.Characters.Count, call.ZoneName);
    }
}

public interface IOutputGenerator
{
    IEnumerable<string> GenerateOutput(RaidEntries raidEntries, Func<string, string> getZoneRaidAlias);
}
