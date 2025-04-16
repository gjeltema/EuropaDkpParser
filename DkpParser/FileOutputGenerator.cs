// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class FileOutputGenerator : IOutputGenerator
{
    private const string Delim = Constants.AttendanceDelimiter;

    public IEnumerable<string> GenerateOutput(RaidEntries raidEntries, Func<string, string> getZoneRaidAlias)
    {
        int currentTransferIndex = -1;
        List<DkpTransfer> transfers = [.. raidEntries.Transfers];
        DkpTransfer currentTransfer = GetNextTransfer(transfers, currentTransferIndex);

        var dkpEntries = raidEntries.DkpEntries.Select(x => new { AssociatedAttendance = raidEntries.GetAssociatedAttendance(x), Dkp = x }).ToList();
        string currentRaidZone = string.Empty;
        foreach (AttendanceEntry attendance in raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp))
        {
            if (currentTransfer != null && currentTransfer.Timestamp <= attendance.Timestamp)
            {
                yield return currentTransfer.LogLine;
                currentTransferIndex++;
                currentTransfer = GetNextTransfer(transfers, currentTransferIndex);
            }

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
                if (currentTransfer != null && currentTransfer.Timestamp <= dkpEntryWithAttendance.Dkp.Timestamp)
                {
                    yield return currentTransfer.LogLine;
                    currentTransferIndex++;
                    currentTransfer = GetNextTransfer(transfers, currentTransferIndex);
                }

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
            ? $"{Delim}{Constants.RaidAttendanceTaken}{Delim}{Constants.Attendance}{Delim}{call.CallName}{Delim}"
            : $"{Delim}{Constants.RaidAttendanceTaken}{Delim}{call.CallName}{Delim}{Constants.KillCall}{Delim}";

        yield return EqLogLine.YouTellRaid(call.Timestamp, header);
        yield return EqLogLine.LogMessage(call.Timestamp, Constants.PlayersOnEverquest);
        yield return EqLogLine.LogMessage(call.Timestamp, Constants.Dashes);

        foreach (PlayerCharacter player in call.Characters.OrderBy(x => x.CharacterName))
        {
            yield return EqLogLine.CharacterListing(call.Timestamp, player.CharacterName, player.Race, player.Level, player.ClassName, player.IsAnonymous);
        }

        yield return EqLogLine.ZonePlayers(call.Timestamp, call.Characters.Count, call.ZoneName);
    }

    private static DkpTransfer GetNextTransfer(List<DkpTransfer> transfers, int currentIndex)
    {
        int nextIndex = currentIndex + 1;
        if (nextIndex >= transfers.Count)
            return null;

        return transfers[nextIndex];
    }
}

public interface IOutputGenerator
{
    IEnumerable<string> GenerateOutput(RaidEntries raidEntries, Func<string, string> getZoneRaidAlias);
}
