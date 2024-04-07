// -----------------------------------------------------------------------
// RaidUploader.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class RaidUploader : IRaidUpload
{
    private readonly IDkpServer _dkpServer;
    private readonly DkpParserSettings _settings;

    public RaidUploader(DkpParserSettings settings)
    {
        _settings = settings;
        _dkpServer = new DkpServer(settings);
    }

    public async Task<RaidUploadResults> UploadRaid(RaidEntries raidEntries)
    {
        RaidUploadResults results = new();

        await _dkpServer.InitializeCharacterIds(raidEntries.AllPlayersInRaid, raidEntries.DkpEntries, results);

        if (results.FailedCharacterIdRetrievals.Count > 0)
            return results;

        try
        {
            await UploadAttendances(raidEntries.AttendanceEntries, results);
        }
        catch (Exception ex)
        {
            //** Add exception to results, need raid info
            return results;
        }

        await UploadDkpSpendings(raidEntries.DkpEntries, results);

        return results;
    }

    private async Task UploadAttendances(ICollection<AttendanceEntry> attendanceEntries, RaidUploadResults results)
    {
        foreach (AttendanceEntry attendance in attendanceEntries)
        {
            if (attendance.Players.Count > 1)
            {
                DkpServerMessageResult result = await _dkpServer.UploadAttendance(attendance);
                results.AddAttendanceResult(result);
            }
        }
    }

    private async Task UploadDkpSpendings(ICollection<DkpEntry> dkpEntries, RaidUploadResults results)
    {
        foreach (DkpEntry dkpEntry in dkpEntries)
        {
            DkpServerMessageResult result = await _dkpServer.UploadDkpSpent(dkpEntry);
            results.AddDkpSpentResult(result);
        }
    }
}

public sealed class RaidUploadResults
{
    //** Not sure yet
    public ICollection<DkpServerMessageResult> AttendanceUploadResults { get; } = [];

    public ICollection<DkpServerMessageResult> DkpSpentUploadResults { get; } = [];

    public ICollection<CharacterIdFailure> FailedCharacterIdRetrievals { get; } = [];

    public void AddAttendanceResult(DkpServerMessageResult result)
    {

    }

    public void AddDkpSpentResult(DkpServerMessageResult result)
    {

    }
}

public sealed class CharacterIdFailure
{
    public Exception Error { get; set; }

    public string PlayerName { get; set; }
}

public interface IRaidUpload
{
    Task<RaidUploadResults> UploadRaid(RaidEntries raidEntries);
}
