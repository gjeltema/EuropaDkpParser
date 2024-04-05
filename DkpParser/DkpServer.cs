// -----------------------------------------------------------------------
// DkpServer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpServer : IDkpServer
{
    private readonly DkpParserSettings _settings;

    public DkpServer(DkpParserSettings settings)
    {
        _settings = settings;
    }

    public DkpServerMessageResult UploadAttendance(AttendanceEntry attendanceEntry)
    {
        DkpServerMessageResult result = new();

        return result;
    }

    public DkpServerMessageResult UploadDkpSpent(DkpEntry attendanceEntry)
    {
        DkpServerMessageResult result = new();

        return result;
    }
}

public sealed class DkpServerMessageResult
{

}

public interface IDkpServer
{
    DkpServerMessageResult UploadAttendance(AttendanceEntry attendanceEntry);

    DkpServerMessageResult UploadDkpSpent(DkpEntry dkpEntry);
}
