// -----------------------------------------------------------------------
// UploadRaidInfo.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

public sealed class UploadRaidInfo
{
    private UploadRaidInfo()
    { }

    public ICollection<AttendanceUploadInfo> AttendanceInfo { get; init; }

    public ICollection<string> CharacterNames { get; init; }

    public ICollection<DkpUploadInfo> DkpInfo { get; init; }

    public static UploadRaidInfo Create(RaidEntries raidEntries, Func<string, string> getZoneRaidAlias)
    {
        ICollection<AttendanceUploadInfo> attendanceUploadInfo = raidEntries.AttendanceEntries.Select(x => new AttendanceUploadInfo
        {
            Timestamp = x.Timestamp,
            CallName = x.CallName,
            ZoneName = x.ZoneName,
            AttendanceCallType = x.AttendanceCallType,
            Characters = x.Characters,
        }).ToList();

        ICollection<RaidInfo> raidInfo = raidEntries.GetRaidInfo(getZoneRaidAlias);
        ICollection<DkpUploadInfo> dkpUploadInfo = raidEntries.DkpEntries.Select(x => new DkpUploadInfo
        {
            Timestamp = x.Timestamp,
            CharacterName = x.PlayerName,
            Item = x.Item,
            DkpSpent = x.DkpSpent,
            AssociatedAttendanceCall = GetAssociatedAttendance(x, raidInfo)
        }).ToList();

        ICollection<string> charactersToBeUploaded = raidEntries.AllCharactersInRaid
            .Select(x => x.CharacterName)
            .Union(raidEntries.DkpEntries.Select(x => x.PlayerName))
            .ToList();

        return new UploadRaidInfo
        {
            AttendanceInfo = attendanceUploadInfo,
            DkpInfo = dkpUploadInfo,
            CharacterNames = charactersToBeUploaded
        };
    }

    public static UploadRaidInfo Create(IEnumerable<AttendanceEntry> attendances,  IEnumerable<string> charactersToBeUploaded)
    {
        return new UploadRaidInfo
        {
            AttendanceInfo = attendances.Select(x => new AttendanceUploadInfo 
            { 
                AttendanceCallType =  x.AttendanceCallType,
                CallName = x.CallName,
                Characters = x.Characters,
                Timestamp = x.Timestamp,
                ZoneName = x.ZoneName,
            }).ToList(),
            DkpInfo = [],
            CharacterNames = charactersToBeUploaded.ToList()
        };
    }

    private static AttendanceEntry GetAssociatedAttendance(DkpEntry dkpEntry, ICollection<RaidInfo> raidInfo)
    {
        if (raidInfo == null || raidInfo.Count == 0)
            return null;

        RaidInfo associatedRaid = raidInfo
            .FirstOrDefault(x => x.StartTime <= dkpEntry.Timestamp && dkpEntry.Timestamp <= x.EndTime);

        if (associatedRaid == null)
        {
            return raidInfo.Last().LastAttendanceCall;
        }

        return associatedRaid.LastAttendanceCall;
    }
}
