// -----------------------------------------------------------------------
// RaidUploader.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

using System.Diagnostics;
using DkpParser;
using Gjeltema.Logging;

public sealed class RaidUploader : IRaidUpload
{
    private const string LogPrefix = $"[{nameof(RaidUploader)}]";
    private readonly IDkpServer _dkpServer;
    private readonly IMusterDkpServer _musterDkpServer;

    public RaidUploader(IDkpParserSettings settings)
    {
        _dkpServer = new DkpServer(settings);
        _musterDkpServer = new MusterDkpServer(settings);
    }

    public async Task<EqDkpRaidUploadResults> UploadEqDkpRaidAsync(UploadRaidInfo uploadRaidInfo)
    {
        Log.Debug($"{LogPrefix} =========== Beginning Upload Process ===========");

        EqDkpRaidUploadResults results = new();

        if (uploadRaidInfo.AttendanceInfo.Count == 0)
        {
            results.NoRaidAttendancesFoundError = true;
            return results;
        }

        IEnumerable<string> zoneNames = uploadRaidInfo.AttendanceInfo.Select(x => x.ZoneName).Distinct();

        await _dkpServer.InitializeIdentifiersAsync(uploadRaidInfo.CharacterNames, zoneNames, results);

        if (results.HasInitializationError)
        {
            Log.Debug($"{LogPrefix} =========== Errors encountered retriving IDs, ending upload process ===========");
            return results;
        }

        Log.Debug($"{LogPrefix} ===== Beginning Attendances Uploads =====");

        await UploadAttendancesAsync(uploadRaidInfo.AttendanceInfo, results);
        if (results.AttendanceError != null)
        {
            Log.Debug($"{LogPrefix} =========== Errors encountered uploading attendances, ending upload process ===========");
            return results;
        }

        await UploadDkpSpendingsAsync(uploadRaidInfo.DkpInfo, results);

        Log.Debug($"{LogPrefix} =========== Completed Upload Process =========== ");

        return results;
    }

    public async Task<MusterDkpRaidUploadResults> UploadMusterRaidAsync(UploadRaidInfo uploadRaidInfo)
    {
        Log.Debug($"{LogPrefix} =========== Beginning Upload Process ===========");

        ICollection<MusterPreCheckError> charactersDontExist = await GetCharactersNotExisting(uploadRaidInfo);
        if (charactersDontExist.Count > 0)
        {
            MusterDkpRaidUploadResults preResults = new()
            {
                CharactersDontExistPreCheck = charactersDontExist,
                DryRun = uploadRaidInfo.IsTestUpload,
            };

            Log.Debug($"{LogPrefix} Pre-check for characters existing found missing chars: {string.Join(',', charactersDontExist)}");

            return preResults;
        }

        Log.Debug($"{LogPrefix} Starting Upload.");
        MusterDkpRaidUploadResults results = await _musterDkpServer.UploadRaidAsync(uploadRaidInfo);
        Log.Debug($"{LogPrefix} =========== Completed Upload Process =========== ");

        return results;
    }

    private async Task<ICollection<MusterPreCheckError>> GetCharactersNotExisting(UploadRaidInfo uploadRaidInfo)
    {
        ICollection<CharacterServerInfo> recentChars = await _musterDkpServer.GetAllActiveCharacterAttendancesAsync();
        List<string> recentCharNames = recentChars.Select(x => x.CharacterName).ToList();

        // Attendance check
        IEnumerable<string> attendanceChars = (from attendance in uploadRaidInfo.AttendanceInfo
                                               from character in attendance.Characters
                                               select character.CharacterName).Distinct();

        List<MusterPreCheckError> errors = [];
        IEnumerable<string> missingChars = attendanceChars.Except(recentCharNames);
        foreach (string missingChar in missingChars)
        {
            CharacterServerInfo singleCharCheck = await _musterDkpServer.GetCharacterAttendanceAsync(missingChar);
            if (singleCharCheck == null)
                errors.Add(new MusterPreCheckError { CharacterName = missingChar });
        }

        // DKP Spent check
        foreach (DkpUploadInfo dkpEntry in uploadRaidInfo.DkpInfo)
        {
            if (!recentCharNames.Contains(dkpEntry.CharacterName))
            {
                CharacterServerInfo singleCharCheck = await _musterDkpServer.GetCharacterAttendanceAsync(dkpEntry.CharacterName);
                if (singleCharCheck == null)
                    errors.Add(new MusterPreCheckError { CharacterName = dkpEntry.CharacterName, ItemBought = dkpEntry });
            }
        }

        return errors;
    }

    private async Task UploadAttendancesAsync(IEnumerable<AttendanceUploadInfo> attendanceEntries, EqDkpRaidUploadResults results)
    {
        foreach (AttendanceUploadInfo attendance in attendanceEntries)
        {
            if (attendance.Characters.Count > 1)
            {
                Log.Debug($"{LogPrefix} ----- Beginning upload process of {attendance}.");

                try
                {
                    await _dkpServer.UploadAttendanceAsync(attendance);
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

    private async Task UploadDkpSpendingsAsync(IEnumerable<DkpUploadInfo> dkpEntries, EqDkpRaidUploadResults results)
    {
        foreach (DkpUploadInfo dkpEntry in dkpEntries)
        {
            try
            {
                Log.Debug($"{LogPrefix} ----- Beginning upload process of: {dkpEntry}.");
                await _dkpServer.UploadDkpSpentAsync(dkpEntry);
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

public sealed class EqDkpRaidUploadResults
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
    Task<EqDkpRaidUploadResults> UploadEqDkpRaidAsync(UploadRaidInfo uploadRaidInfo);

    Task<MusterDkpRaidUploadResults> UploadMusterRaidAsync(UploadRaidInfo uploadRaidInfo);
}
