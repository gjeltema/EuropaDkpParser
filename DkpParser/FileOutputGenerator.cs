// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class FileOutputGenerator : IOutputGenerator
{
    private readonly string _outputFileName;

    public FileOutputGenerator(string outputFileName)
    {
        _outputFileName = outputFileName;
    }

    public async Task GenerateOutput(RaidEntries raidEntries)
    {
        List<string> outputContents = [];

        GenerateAttendanceCalls(raidEntries, outputContents);
        GenerateDkpCalls(raidEntries, outputContents);
        await WriteToFile(outputContents);
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
        string dateStampText = call.Timestamp.ToString(Constants.LogDateTimeFormat);
        string header = call.AttendanceCallType == AttendanceCallType.Time
            ? $"[{dateStampText}] You tell your raid, '{Constants.AttendanceDelimiter}{Constants.RaidAttendanceTaken}{Constants.AttendanceDelimiter}{Constants.Attendance}{Constants.AttendanceDelimiter}{call.RaidName}{Constants.AttendanceDelimiter}'"
            : $"[{dateStampText}] You tell your raid, '{Constants.AttendanceDelimiter}Raid Attendance Taken{Constants.AttendanceDelimiter}{call.RaidName}{Constants.AttendanceDelimiter}{Constants.KillCall}{Constants.AttendanceDelimiter}'";

        yield return header;
        yield return Constants.PlayersOnEverquest;
        yield return Constants.Dashes;

        foreach (string player in call.PlayerNames)
        {
            yield return $"[{dateStampText}] [ANONYMOUS] {player}  <Europa>";
        }

        yield return $"[{dateStampText}] There are {call.PlayerNames.Count} players in {call.ZoneName}.";
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
        // [Tue Mar 19 22:45:32 2024] You tell your raid, ':::Djinni War Blade::: ZCalie 1 DKPSPENT'
        foreach (DkpEntry call in raidEntries.DkpEntries.OrderBy(x => x.Timestamp))
        {
            string dateStampText = call.Timestamp.ToString(Constants.LogDateTimeFormat);
            string dkpEntry =
                $"[{dateStampText}] You tell your raid, '{Constants.AttendanceDelimiter}{call.Item}{Constants.AttendanceDelimiter} {call.PlayerName} {call.DkpSpent} {Constants.DkpSpent}'";
            outputContents.Add(dkpEntry);
        }
    }

    private async Task WriteToFile(IEnumerable<string> outputContents)
    {
        await File.WriteAllLinesAsync(_outputFileName, outputContents);
    }
}

public interface IOutputGenerator
{
    Task GenerateOutput(RaidEntries raidEntries);
}
