// -----------------------------------------------------------------------
// RaidUploader.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

using System.Diagnostics;
using DkpParser;
using Gjeltema.Logging;

public sealed class RaidUploader : IRaidUpload
{
    private const string LogPrefix = $"[{nameof(RaidUploader)}]";
    private readonly IDkpServer _dkpServer;

    public RaidUploader(IDkpParserSettings settings)
    {
        _dkpServer = new DkpServer(settings);
    }

    public async Task<RaidUploadResults> UploadRaid(UploadRaidInfo uploadRaidInfo)
    {
        Log.Debug($"{LogPrefix} =========== Beginning Upload Process ===========");

        RaidUploadResults results = new();

        if (uploadRaidInfo.AttendanceInfo.Count == 0)
        {
            results.NoRaidAttendancesFoundError = true;
            return results;
        }

        IEnumerable<string> zoneNames = uploadRaidInfo.AttendanceInfo.Select(x => x.ZoneName).Distinct();

        await _dkpServer.InitializeIdentifiers(uploadRaidInfo.CharacterNames, zoneNames, results);

        if (results.HasInitializationError)
        {
            Log.Debug($"{LogPrefix} =========== Errors encountered retriving IDs, ending upload process ===========");
            return results;
        }

        Log.Debug($"{LogPrefix} ===== Beginning Attendances Uploads =====");

        await UploadAttendances(uploadRaidInfo.AttendanceInfo, results);
        if (results.AttendanceError != null)
        {
            Log.Debug($"{LogPrefix} =========== Errors encountered uploading attendances, ending upload process ===========");
            return results;
        }

        await UploadDkpSpendings(uploadRaidInfo.DkpInfo, results);

        Log.Debug($"{LogPrefix} =========== Completed Upload Process =========== ");

        return results;
    }

    private async Task UploadAttendances(IEnumerable<AttendanceUploadInfo> attendanceEntries, RaidUploadResults results)
    {
        foreach (AttendanceUploadInfo attendance in attendanceEntries)
        {
            if (attendance.Characters.Count > 1)
            {
                Log.Debug($"{LogPrefix} ----- Beginning upload process of {attendance}.");

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

                    Log.Error($"{LogPrefix} Error encountered when uploading {attendance}: {ex.ToLogMessage()}");

                    return;
                }
            }
            else
            {
                Log.Debug($"{LogPrefix} Attendance {attendance} has no players in attendance.  Not uploading.");
            }
        }

        Log.Debug($"{LogPrefix} ----- Completed uploading raid attendances.");
    }

    private async Task UploadDkpSpendings(IEnumerable<DkpUploadInfo> dkpEntries, RaidUploadResults results)
    {
        foreach (DkpUploadInfo dkpEntry in dkpEntries)
        {
            try
            {
                Log.Debug($"{LogPrefix} ----- Beginning upload process of: {dkpEntry}.");
                await _dkpServer.UploadDkpSpent(dkpEntry);
            }
            catch (Exception ex)
            {
                DkpUploadFailure error = new()
                {
                    Dkp = dkpEntry,
                    Error = ex
                };
                results.DkpFailures.Add(error);

                Log.Error($"{LogPrefix} Error encountered when uploading {dkpEntry}: {ex.ToLogMessage()}");
            }
        }

        Log.Debug($"{LogPrefix} ----- Completed uploading DKSPENT calls.");
    }
}

public sealed class RaidUploadResults
{
    public AttendanceUploadFailure AttendanceError { get; set; }

    public ICollection<DkpUploadFailure> DkpFailures { get; set; } = new List<DkpUploadFailure>();

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
    public AttendanceUploadInfo Attendance { get; init; }

    public Exception Error { get; init; }
}

[DebuggerDisplay("{Dkp}")]
public sealed class DkpUploadFailure
{
    public DkpUploadInfo Dkp { get; init; }

    public Exception Error { get; init; }
}

[DebuggerDisplay("{PlayerName}")]
public sealed class CharacterIdFailure
{
    public string CharacterName { get; init; }

    public Exception Error { get; init; }
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
    Task<RaidUploadResults> UploadRaid(UploadRaidInfo uploadRaidInfo);
}
