// -----------------------------------------------------------------------
// RaidUploader.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

public sealed class RaidUploader : IRaidUpload
{
    private readonly IUploadDebugInfo _debugInfo;
    private readonly IDkpServer _dkpServer;

    public RaidUploader(IDkpParserSettings settings, IUploadDebugInfo debugInfo)
    {
        _dkpServer = new DkpServer(settings, debugInfo);
        _debugInfo = debugInfo;
    }

    public async Task<RaidUploadResults> UploadRaid(RaidEntries raidEntries)
    {
        _debugInfo.AddDebugMessage("=========== Beginning Upload Process ===========");

        RaidUploadResults results = new();

        if (raidEntries.AttendanceEntries.Count == 0)
        {
            results.NoRaidAttendancesFoundError = true;
            return results;
        }

        IEnumerable<string> allPlayerNames = raidEntries.AllCharactersInRaid
            .Select(x => x.CharacterName)
            .Union(raidEntries.DkpEntries.Select(x => x.PlayerName));

        IEnumerable<string> zoneNames = raidEntries.AttendanceEntries.Select(x => x.ZoneName).Distinct();

        await _dkpServer.InitializeIdentifiers(allPlayerNames, zoneNames, results);

        if (results.HasInitializationError)
        {
            _debugInfo.AddDebugMessage("=========== Errors encountered retriving IDs, ending upload process ===========");
            return results;
        }

        _debugInfo.AddDebugMessage("===== Beginning Attendances Uploads =====");

        await UploadAttendances(raidEntries.AttendanceEntries, results);
        if (results.AttendanceError != null)
        {
            _debugInfo.AddDebugMessage("=========== Errors encountered uploading attendances, ending upload process ===========");
            return results;
        }

        await UploadDkpSpendings(raidEntries.DkpEntries, results);

        _debugInfo.AddDebugMessage("=========== Completed Upload Process =========== ");

        return results;
    }

    private async Task UploadAttendances(IEnumerable<AttendanceEntry> attendanceEntries, RaidUploadResults results)
    {
        foreach (AttendanceEntry attendance in attendanceEntries)
        {
            if (attendance.Characters.Count > 1)
            {
                _debugInfo.AddDebugMessage($"----- Beginning upload process of {attendance}.");

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

                    _debugInfo.AddDebugMessage($"Error encountered when uploading {attendance}: {ex}");

                    return;
                }
            }
            else
            {
                _debugInfo.AddDebugMessage($"Attendance {attendance} has no players in attendance.  Not uploading.");
            }
        }

        _debugInfo.AddDebugMessage("----- Completed uploading raid attendances.");
    }

    private async Task UploadDkpSpendings(IEnumerable<DkpEntry> dkpEntries, RaidUploadResults results)
    {
        foreach (DkpEntry dkpEntry in dkpEntries)
        {
            try
            {
                _debugInfo.AddDebugMessage($"----- Beginning upload process of: {dkpEntry.ToLogString()}.");
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

                _debugInfo.AddDebugMessage($"Error encountered when uploading {dkpEntry.ToLogString()}: {ex}");

                return;
            }
        }

        _debugInfo.AddDebugMessage("----- Completed uploading DKSPENT calls.");
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

    public bool NoRaidAttendancesFoundError { get; set; }
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
