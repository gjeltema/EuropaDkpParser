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

    public async static Task<UploadRaidInfo> Create(IDkpAdjustments dkpAdjustments, RaidEntries raidEntries)
    {
        ICollection<AttendanceUploadInfo> attendanceUploadInfo = raidEntries.AttendanceEntries
            .OrderBy(x => x.Timestamp)
            .Select(x => new AttendanceUploadInfo
            {
                Timestamp = x.Timestamp,
                CallName = x.CallName,
                ZoneName = x.ZoneName,
                AttendanceCallType = x.AttendanceCallType,
                Characters = ConvertTransfers(x.Characters, raidEntries.Transfers),
            }).ToList();

        IOrderedEnumerable<DkpEntry> dkpEntries = raidEntries.DkpEntries.OrderBy(x => x.Timestamp);
        ICollection<DkpUploadInfo> dkpUploadInfo = await GetDkpInfo(dkpEntries, raidEntries, dkpAdjustments);

        ICollection<string> allCharacterNames = raidEntries.AllCharactersInRaid
            .Select(x => x.CharacterName)
            .Union(raidEntries.DkpEntries.Select(x => x.CharacterName))
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

    /// <summary>
    /// Used for uploading only select attendances, assumes zone names are already sanitized.
    /// </summary>
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

    private async static Task<ICollection<DkpUploadInfo>> GetDkpInfo(IEnumerable<DkpEntry> dkpEntries, RaidEntries raidEntries, IDkpAdjustments dkpAdjustments)
    {
        List<DkpUploadInfo> dkpUploadInfos = [];
        foreach (DkpEntry dkpEntry in dkpEntries)
        {
            AttendanceEntry associatedCall = raidEntries.GetAssociatedAttendance(dkpEntry);
            PlayerCharacter character = raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == dkpEntry.CharacterName);
            int dkpAmount = dkpEntry.DkpSpent;
            if (character != null)
            {
                int numberOfAttendances = raidEntries.AttendanceEntries.Where(x => x.Characters.Contains(character)).Take(3).Count();
                if (numberOfAttendances > 2)
                    dkpAmount = await dkpAdjustments.GetDkpDiscountedAmount(dkpEntry, character.ClassName, associatedCall);
            }

            if (dkpAmount != dkpEntry.DkpSpent)
            {
                DiscountApplied discount = new()
                {
                    CharacterName = dkpEntry.CharacterName,
                    Item = dkpEntry.Item,
                    AttendanceName = associatedCall.CallName,
                    AttendanceZone = associatedCall.ZoneName,
                    OriginalSpent = dkpEntry.DkpSpent,
                    AfterDiscountSpent = dkpAmount
                };
                raidEntries.Discounts.Add(discount);
            }

            dkpUploadInfos.Add(new DkpUploadInfo
            {
                AssociatedAttendanceCall = associatedCall,
                CharacterName = dkpEntry.CharacterName,
                DkpSpent = dkpEntry.DkpSpent,
                Item = dkpEntry.Item,
                Timestamp = dkpEntry.Timestamp
            });
        }

        return dkpUploadInfos;
    }
}
