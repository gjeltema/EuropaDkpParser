// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class FileOutputGenerator : IOutputGenerator
{
    public ICollection<string> GenerateOutput(RaidEntries raidEntries, IEnumerable<RaidInfo> raids)
    {
        List<string> outputContents = [];

        foreach (RaidInfo raid in raids)
        {
            DateTime dateStamp = raid.StartTime;
            if (dateStamp == DateTime.MinValue)
            {
                dateStamp = raid.FirstAttendanceCall.Timestamp.AddMinutes(-10);
            }

            string message = EqLogLine.LogMessage(dateStamp, $"=========================== {raid.RaidZone} ===========================");
            outputContents.Add(message);

            IEnumerable<AttendanceEntry> attendanceCalls = raidEntries.AttendanceEntries
                .Where(x => raid.StartTime <= x.Timestamp && x.Timestamp <= raid.EndTime)
                .OrderBy(x => x.Timestamp);

            foreach (AttendanceEntry attendanceEntry in attendanceCalls)
            {
                IEnumerable<string> attendanceEntryLines = CreateAttendanceEntry(attendanceEntry);
                outputContents.AddRange(attendanceEntryLines);
            }

            CreateDkpEntries(raidEntries, outputContents, raid);

            outputContents.Add(Environment.NewLine);
            outputContents.Add(Environment.NewLine);
        }

        return outputContents;
    }

    private IEnumerable<string> CreateAttendanceEntry(AttendanceEntry call)
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

    private void CreateDkpEntries(RaidEntries raidEntries, List<string> outputContents, RaidInfo raid)
    {
        IEnumerable<string> dkpEntriesText = raidEntries.GetDkpspentEntriesForRaid(raid);
        outputContents.AddRange(dkpEntriesText);
    }
}

public interface IOutputGenerator
{
    ICollection<string> GenerateOutput(RaidEntries raidEntries, IEnumerable<RaidInfo> raids);
}
