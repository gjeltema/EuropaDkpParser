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

    public RaidUploadResults UploadRaid(RaidEntries raidEntries)
    {
        RaidUploadResults results = new();

        UploadAttendances(raidEntries.AttendanceEntries, results);
        UploadDkpSpendings(raidEntries.DkpEntries, results);

        return results;
    }

    private void UploadAttendances(ICollection<AttendanceEntry> attendanceEntries, RaidUploadResults results)
    {
        foreach (AttendanceEntry attendance in attendanceEntries)
        {
            if (attendance.Players.Count > 1)
            {
                DkpServerMessageResult result = _dkpServer.UploadAttendance(attendance);
                results.AddAttendanceResult(result);
            }
        }
    }

    private void UploadDkpSpendings(ICollection<DkpEntry> dkpEntries, RaidUploadResults results)
    {
        foreach (DkpEntry dkpEntry in dkpEntries)
        {
            DkpServerMessageResult result = _dkpServer.UploadDkpSpent(dkpEntry);
            results.AddDkpSpentResult(result);
        }
    }
}

public sealed class RaidUploadResults
{
    public ICollection<DkpServerMessageResult> AttendanceUploadResults { get; } = [];

    public ICollection<DkpServerMessageResult> DkpSpentUploadResults { get; } = [];

    public void AddAttendanceResult(DkpServerMessageResult result)
    {

    }

    public void AddDkpSpentResult(DkpServerMessageResult result)
    {

    }
}

public interface IRaidUpload
{
    RaidUploadResults UploadRaid(RaidEntries raidEntries);
}
