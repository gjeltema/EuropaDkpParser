// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class FileOutputGenerator : IOutputGenerator
{
    public ICollection<string> GenerateOutput(RaidEntries raidEntries)
    {
        List<string> outputContents = [];

        GenerateAttendanceCalls(raidEntries, outputContents);
        GenerateDkpCalls(raidEntries, outputContents);

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
        string dateStampText = call.Timestamp.ToString(Constants.LogDateTimeFormat, Constants.UsCulture);
        string header = call.AttendanceCallType == AttendanceCallType.Time
            ? $"{dateStampText} You tell your raid, '{Constants.AttendanceDelimiter}{Constants.RaidAttendanceTaken}{Constants.AttendanceDelimiter}{Constants.Attendance}{Constants.AttendanceDelimiter}{call.RaidName}{Constants.AttendanceDelimiter}'"
            : $"{dateStampText} You tell your raid, '{Constants.AttendanceDelimiter}{Constants.RaidAttendanceTaken}{Constants.AttendanceDelimiter}{call.RaidName}{Constants.AttendanceDelimiter}{Constants.KillCall}{Constants.AttendanceDelimiter}'";

        yield return header;
        yield return Constants.PlayersOnEverquest;
        yield return Constants.Dashes;

        foreach (PlayerCharacter player in call.Players.OrderBy(x => x.PlayerName))
        {
            yield return $"{dateStampText} {player.ToLogString()}";
        }

        yield return $"{dateStampText} There are {call.Players.Count} players in {call.ZoneName}.";
    }

    private void GenerateAttendanceCalls(RaidEntries raidEntries, List<string> outputContents)
    {
        foreach (AttendanceEntry attendanceCall in raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp))
        {
            IEnumerable<string> attendanceEntry = CreateAttendanceEntry(attendanceCall);
            outputContents.AddRange(attendanceEntry);
        }
    }

    private void GenerateDkpCalls(RaidEntries raidEntries, List<string> outputContents)
    {
        // [Thu Feb 22 23:27:00 2024] Genoo tells the raid,  '::: Belt of the Pine ::: huggin 3 DKPSPENT'
        // [Sun Mar 17 21:40:50 2024] You tell your raid, ':::High Quality Raiment::: Coyote 1 DKPSPENT'
        foreach (DkpEntry call in raidEntries.DkpEntries.OrderBy(x => x.Timestamp))
        {
            string dateStampText = call.Timestamp.ToString(Constants.LogDateTimeFormat, Constants.UsCulture);
            string dkpEntry = $"{dateStampText} {call.ToLogString()}";
            outputContents.Add(dkpEntry);
        }
    }
}

public interface IOutputGenerator
{
    ICollection<string> GenerateOutput(RaidEntries raidEntries);
}
