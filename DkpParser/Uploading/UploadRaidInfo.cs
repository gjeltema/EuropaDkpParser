// -----------------------------------------------------------------------
// UploadRaidInfo.cs Copyright 2025 Craig Gjeltema
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
        ICollection<AttendanceUploadInfo> attendanceUploadInfo = raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).Select(x => new AttendanceUploadInfo
        {
            Timestamp = x.Timestamp,
            CallName = x.CallName,
            ZoneName = x.ZoneName,
            AttendanceCallType = x.AttendanceCallType,
            Characters = ConvertTransfers(x.Characters, raidEntries.Transfers),
        }).ToList();

        ICollection<DkpUploadInfo> dkpUploadInfo = raidEntries.DkpEntries.OrderBy(x => x.Timestamp).Select(x => new DkpUploadInfo
        {
            Timestamp = x.Timestamp,
            CharacterName = x.PlayerName,
            Item = x.Item,
            DkpSpent = x.DkpSpent,
            AssociatedAttendanceCall = raidEntries.GetAssociatedAttendance(x)
        }).ToList();

        ICollection<string> allCharacterNames = raidEntries.AllCharactersInRaid
            .Select(x => x.CharacterName)
            .Union(raidEntries.DkpEntries.Select(x => x.PlayerName))
            .Union(raidEntries.Transfers.Select(x => x.ToCharacterName))
            .Order()
            .ToList();

        return new UploadRaidInfo
        {
            AttendanceInfo = attendanceUploadInfo,
            DkpInfo = dkpUploadInfo,
            CharacterNames = allCharacterNames
        };
    }

    public static UploadRaidInfo Create(IEnumerable<AttendanceEntry> attendances, RaidEntries raidEntries)
    {
        IEnumerable<PlayerCharacter> charactersToBeUploaded = ConvertTransfers(raidEntries.AllCharactersInRaid, raidEntries.Transfers);

        return new UploadRaidInfo
        {
            AttendanceInfo = attendances.OrderBy(x => x.Timestamp).Select(x => new AttendanceUploadInfo
            {
                AttendanceCallType = x.AttendanceCallType,
                CallName = x.CallName,
                Characters = ConvertTransfers(x.Characters, raidEntries.Transfers),
                Timestamp = x.Timestamp,
                ZoneName = x.ZoneName,
            }).ToList(),
            DkpInfo = [],
            CharacterNames = charactersToBeUploaded.Select(x => x.CharacterName).ToList()
        };
    }

    private static PlayerCharacter ConvertTransfer(PlayerCharacter character, ICollection<DkpTransfer> transfers)
    {
        DkpTransfer transferCharacter = transfers.FirstOrDefault(x => x.FromCharacter == character);
        return transferCharacter == null ? character : new PlayerCharacter { CharacterName = transferCharacter.ToCharacterName };
    }

    private static ICollection<PlayerCharacter> ConvertTransfers(IEnumerable<PlayerCharacter> characterList, ICollection<DkpTransfer> transfers)
    {
        if (transfers.Count == 0)
            return characterList.ToList();

        List<PlayerCharacter> newList = [];
        foreach (PlayerCharacter playerCharacter in characterList)
        {
            PlayerCharacter charToAdd = ConvertTransfer(playerCharacter, transfers);
            newList.Add(charToAdd);
        }

        return newList.OrderBy(x => x.CharacterName).ToList();
    }
}
