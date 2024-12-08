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
            Characters = ConvertTransfers(x.Characters, raidEntries.Transfers),
        }).ToList();

        ICollection<DkpUploadInfo> dkpUploadInfo = raidEntries.DkpEntries.Select(x => new DkpUploadInfo
        {
            Timestamp = x.Timestamp,
            CharacterName = ConvertTransfer(x.PlayerName, raidEntries.Transfers),
            Item = x.Item,
            DkpSpent = x.DkpSpent,
            AssociatedAttendanceCall = raidEntries.GetAssociatedAttendance(x)
        }).ToList();

        IEnumerable<string> allCharacterNames = raidEntries.AllCharactersInRaid
            .Select(x => x.CharacterName)
            .Union(raidEntries.DkpEntries.Select(x => x.PlayerName));

        ICollection<string> charactersToBeUploaded = ConvertTransfers(allCharacterNames, raidEntries.Transfers);

        return new UploadRaidInfo
        {
            AttendanceInfo = attendanceUploadInfo,
            DkpInfo = dkpUploadInfo,
            CharacterNames = charactersToBeUploaded
        };
    }

    public static UploadRaidInfo Create(IEnumerable<AttendanceEntry> attendances, RaidEntries raidEntries)
    {
        IEnumerable<PlayerCharacter> charactersToBeUploaded = ConvertTransfers(raidEntries.AllCharactersInRaid, raidEntries.Transfers);

        return new UploadRaidInfo
        {
            AttendanceInfo = attendances.Select(x => new AttendanceUploadInfo
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

    private static string ConvertTransfer(string characterName, ICollection<DkpTransfer> transfers)
    {
        if (transfers.Count == 0)
            return characterName;

        DkpTransfer transferCharacter = transfers.FirstOrDefault(x => x.FromCharacter.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        return transferCharacter == null ? characterName : transferCharacter.ToCharacter.CharacterName;
    }

    private static PlayerCharacter ConvertTransfer(PlayerCharacter character, ICollection<DkpTransfer> transfers)
    {
        if (transfers.Count == 0)
            return character;

        DkpTransfer transferCharacter = transfers.FirstOrDefault(x => x.FromCharacter == character);
        return transferCharacter == null ? character : transferCharacter.ToCharacter;
    }

    private static ICollection<string> ConvertTransfers(IEnumerable<string> characterNameList, ICollection<DkpTransfer> transfers)
    {
        if (transfers.Count == 0)
            return characterNameList.ToList();

        ICollection<string> newList = [];
        foreach (string playerCharacterName in characterNameList)
        {
            string charToAdd = ConvertTransfer(playerCharacterName, transfers);
            newList.Add(charToAdd);
        }

        return newList;
    }

    private static ICollection<PlayerCharacter> ConvertTransfers(IEnumerable<PlayerCharacter> characterList, ICollection<DkpTransfer> transfers)
    {
        if (transfers.Count == 0)
            return characterList.ToList();

        ICollection<PlayerCharacter> newList = [];
        foreach (PlayerCharacter playerCharacter in characterList)
        {
            PlayerCharacter charToAdd = ConvertTransfer(playerCharacter, transfers);
            newList.Add(charToAdd);
        }

        return newList;
    }
}
