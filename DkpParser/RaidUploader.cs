// -----------------------------------------------------------------------
// RaidUploader.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

public sealed class RaidUploader : IRaidUpload
{
    private readonly IDkpServer _dkpServer;

    public RaidUploader(IDkpParserSettings settings)
    {
        _dkpServer = new DkpServer(settings);
    }

    public async Task<RaidUploadResults> UploadRaid(RaidEntries raidEntries)
    {
        RaidUploadResults results = new();

        IEnumerable<string> allPlayerNames = raidEntries.AllPlayersInRaid
            .Select(x => x.PlayerName)
            .Union(raidEntries.DkpEntries.Select(x => x.PlayerName));

        IEnumerable<string> zoneNames = raidEntries.AttendanceEntries.Select(x => x.ZoneName).Distinct();

        await _dkpServer.InitializeIdentifiers(allPlayerNames, zoneNames, results);

        if (results.HasInitializationError)
            return results;

        await UploadAttendances(raidEntries.AttendanceEntries, results);
        if (results.AttendanceError != null)
            return results;

        await UploadDkpSpendings(raidEntries.DkpEntries, results);

        return results;
    }

    private async Task UploadAttendances(IEnumerable<AttendanceEntry> attendanceEntries, RaidUploadResults results)
    {
        foreach (AttendanceEntry attendance in attendanceEntries)
        {
            if (attendance.Players.Count > 1)
            {
                try
                {
                    await _dkpServer.UploadAttendance(attendance);
                }
                catch (Exception ex)
                {
                    AttendanceUploadFailure error = new()
                    {
                        Attendance = attendance,
                        Error = ex
                    };
                    results.AttendanceError = error;
                    return;
                }
            }
        }
    }

    private async Task UploadDkpSpendings(IEnumerable<DkpEntry> dkpEntries, RaidUploadResults results)
    {
        foreach (DkpEntry dkpEntry in dkpEntries)
        {
            try
            {
                await _dkpServer.UploadDkpSpent(dkpEntry);
            }
            catch (Exception ex)
            {
                DkpUploadFailure error = new()
                {
                    Dkp = dkpEntry,
                    Error = ex
                };
                results.DkpFailure = error;
                return;
            }
        }
    }
}

public sealed class RaidUploadResults
{
    public AttendanceUploadFailure AttendanceError { get; set; }

    public DkpUploadFailure DkpFailure { get; set; }

    public Exception EventIdCallFailure { get; set; }

    public ICollection<EventIdNotFoundFailure> EventIdNotFoundErrors { get; } = [];

    public ICollection<CharacterIdFailure> FailedCharacterIdRetrievals { get; } = [];

    public bool HasInitializationError
        => EventIdCallFailure != null || FailedCharacterIdRetrievals.Count > 0 || EventIdNotFoundErrors.Count > 0;
}

[DebuggerDisplay("{Attendance}")]
public sealed class AttendanceUploadFailure
{
    public AttendanceEntry Attendance { get; set; }

    public Exception Error { get; set; }
}

[DebuggerDisplay("{Dkp}")]
public sealed class DkpUploadFailure
{
    public DkpEntry Dkp { get; set; }

    public Exception Error { get; set; }
}

[DebuggerDisplay("{PlayerName}")]
public sealed class CharacterIdFailure
{
    public Exception Error { get; set; }

    public string PlayerName { get; set; }
}

[DebuggerDisplay("{DebuggerDisplay}")]
public sealed class EventIdNotFoundFailure
{
    public enum EventIdError : byte
    {
        ZoneNotConfigured,
        ZoneNotFoundOnDkpServer,
        InvalidZoneValue
    }

    public EventIdError ErrorType { get; init; }

    public string IdValue { get; init; }

    public string ZoneAlias { get; init; }

    public string ZoneName { get; init; }

    private string DebuggerDisplay
        => $"Zone:{ZoneName}, Alias:{ZoneAlias}, Error:{ErrorType}, ID:{IdValue}";
}

public interface IRaidUpload
{
    Task<RaidUploadResults> UploadRaid(RaidEntries raidEntries);
}
