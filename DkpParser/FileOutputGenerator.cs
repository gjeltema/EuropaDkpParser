// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class FileOutputGenerator : IOutputGenerator
{
    public ICollection<string> GenerateOutput(RaidEntries raidEntries)
    {
        List<string> outputContents = [];

        foreach (RaidInfo raid in raidEntries.Raids)
        {
            DateTime dateStamp = raid.StartTime;
            if (dateStamp == DateTime.MinValue)
            {
                dateStamp = raid.FirstAttendanceCall.Timestamp.AddMinutes(-10);
            }
            string dateStampText = dateStamp.ToEqLogTimestamp();
            outputContents.Add($"{dateStampText} =========================== {raid.RaidZone} ===========================");

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
        string dateStampText = call.Timestamp.ToEqLogTimestamp();
        string header = call.AttendanceCallType == AttendanceCallType.Time
            ? $"You tell your raid, '{Constants.AttendanceDelimiter}{Constants.RaidAttendanceTaken}{Constants.AttendanceDelimiter}{Constants.Attendance}{Constants.AttendanceDelimiter}{call.RaidName}{Constants.AttendanceDelimiter}'"
            : $"You tell your raid, '{Constants.AttendanceDelimiter}{Constants.RaidAttendanceTaken}{Constants.AttendanceDelimiter}{call.RaidName}{Constants.AttendanceDelimiter}{Constants.KillCall}{Constants.AttendanceDelimiter}'";

        yield return $"{dateStampText} {header}";
        yield return $"{dateStampText} {Constants.PlayersOnEverquest}";
        yield return $"{dateStampText} {Constants.Dashes}";

        foreach (PlayerCharacter player in call.Players.OrderBy(x => x.PlayerName))
        {
            yield return $"{dateStampText} {player.ToLogString()}";
        }

        yield return $"{dateStampText} There are {call.Players.Count} players in {call.ZoneName}.";
    }

    private void CreateDkpEntries(RaidEntries raidEntries, List<string> outputContents, RaidInfo raid)
    {
        IEnumerable<DkpEntry> dkpEntries = raidEntries.DkpEntries
            .Where(x => raid.StartTime <= x.Timestamp && x.Timestamp <= raid.EndTime)
            .OrderBy(x => x.Timestamp);

        // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
        // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
        foreach (DkpEntry dkpEntry in dkpEntries)
        {
            string timestampText = dkpEntry.Timestamp.ToEqLogTimestamp();
            string dkpEntryText = $"{timestampText} {dkpEntry.ToLogString()}";
            outputContents.Add(dkpEntryText);
        }
    }
}

public interface IOutputGenerator
{
    ICollection<string> GenerateOutput(RaidEntries raidEntries);
}
