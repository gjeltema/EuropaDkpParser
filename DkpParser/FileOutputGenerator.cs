// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class FileOutputGenerator : IOutputGenerator
{
    private const string Delim = Constants.AttendanceDelimiter;

    public IEnumerable<string> GenerateOutput(RaidEntries raidEntries, Func<string, string> getZoneRaidAlias)
    {
        Dictionary<DateTime, string> outputLines = new();

        string currentRaidZone = string.Empty;
        foreach (AttendanceEntry entry in raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp))
        {
            string attendanceRaidZone = getZoneRaidAlias(entry.ZoneName);
            if (currentRaidZone != attendanceRaidZone)
            {
                currentRaidZone = attendanceRaidZone;
                DateTime timestamp = entry.Timestamp.AddSeconds(-5);
                outputLines.Add(timestamp.AddSeconds(-1), string.Empty);
                string line = EqLogLine.LogMessage(timestamp, $"=========================== {currentRaidZone} ===========================");
                outputLines.Add(timestamp, line);
            }

            string attendanceCallLines = string.Join(Environment.NewLine, CreateAttendanceEntry(entry));
            outputLines.Add(entry.Timestamp, attendanceCallLines);
        }

        foreach (DkpTransfer transfer in raidEntries.Transfers)
        {
            string transferLine = EqLogLine.LogMessage(transfer.Timestamp, transfer.LogLine);
            outputLines.Add(transfer.Timestamp, transferLine);
        }

        foreach (DkpEntry dkpEntry in raidEntries.DkpEntries)
        {
            outputLines.Add(dkpEntry.Timestamp, dkpEntry.ToLogString());
        }

        foreach (DkpAwardOverride setDkpEntry in raidEntries.DkpAwardOverrides)
        {
            outputLines.Add(setDkpEntry.StartTime, setDkpEntry.LogLine);
            if (setDkpEntry.EndTime != DateTime.MaxValue)
                outputLines.Add(setDkpEntry.EndTime, EqLogLine.YouTellRaid(setDkpEntry.EndTime, Constants.SetAwardedDKPEndAlternateDelimiter));
        }

        return outputLines.OrderBy(x => x.Key).Select(x => x.Value);
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

        if (call.IsHitSquad)
        {
            yield return call.RawHeaderLogLine;
            yield break;
        }

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
}

public interface IOutputGenerator
{
    IEnumerable<string> GenerateOutput(RaidEntries raidEntries, Func<string, string> getZoneRaidAlias);
}
