// -----------------------------------------------------------------------
// UploadRaidInfo.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

using Gjeltema.Logging;

public sealed class UploadRaidInfo
{
    private const string LogPrefix = $"[{nameof(UploadRaidInfo)}]";

    private UploadRaidInfo()
    { }

    public ICollection<AttendanceUploadInfo> AttendanceInfo { get; init; }

    public ICollection<string> CharacterNames { get; init; }

    public ICollection<DkpUploadInfo> DkpInfo { get; init; }

    /// <summary>
    /// Used for uploading only select attendances, assumes zone names are already sanitized.
    /// </summary>
    public static UploadRaidInfo Create(IEnumerable<AttendanceEntry> attendances, RaidEntries raidEntries, IDkpParserSettings settings)
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
                DkpAwarded = GetDkpAwarded(x, raidEntries, settings)
            }).ToList(),
            DkpInfo = [],
            CharacterNames = charactersToBeUploaded.Select(x => x.CharacterName).ToList()
        };
    }

    public async static Task<UploadRaidInfo> CreateAsync(IDkpAdjustments dkpAdjustments, RaidEntries raidEntries, IDkpParserSettings settings)
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
                DkpAwarded = GetDkpAwarded(x, raidEntries, settings)
            }).ToList();

        IOrderedEnumerable<DkpEntry> dkpEntries = raidEntries.DkpEntries.OrderBy(x => x.Timestamp);
        ICollection<DkpUploadInfo> dkpUploadInfo = await GetDkpInfoAsync(dkpEntries, raidEntries, dkpAdjustments);

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

    private static int GetDkpAwarded(AttendanceEntry attendance, RaidEntries raidEntries, IDkpParserSettings settings)
    {
        DkpAwardOverride dkpOverride = raidEntries.GetDkpOverride(attendance.Timestamp);
        if (dkpOverride != null)
            return dkpOverride.DkpAmount;

        int dkpValue = settings.RaidValue.GetDkpValueForRaid(attendance);
        return dkpValue;
    }

    private async static Task<ICollection<DkpUploadInfo>> GetDkpInfoAsync(IEnumerable<DkpEntry> dkpEntries, RaidEntries raidEntries, IDkpAdjustments dkpAdjustments)
    {
        // Need to clear this in case of an error in uploading, and the user clears the error and re-uploads.  If not cleared, there may be multiple identical entries.
        raidEntries.Discounts.Clear();

        List<DkpUploadInfo> dkpUploadInfos = [];
        foreach (DkpEntry dkpEntry in dkpEntries)
        {
            AttendanceEntry associatedCall = raidEntries.GetAssociatedAttendance(dkpEntry);
            PlayerCharacter character = raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == dkpEntry.CharacterName);
            int dkpAmount = dkpEntry.DkpSpent;
            if (character != null && !raidEntries.IsHitSquad)
            {
                // Verify that the character was present for more than 2 attendance calls, to ensure they were not just popping in to loot
                int numberOfAttendances = raidEntries.AttendanceEntries.Where(x => x.Characters.Contains(character)).Take(3).Count();
                if (numberOfAttendances > 2)
                    dkpAmount = await dkpAdjustments.GetDkpDiscountedAmountAsync(dkpEntry, character.ClassName, associatedCall);
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

            DkpUploadInfo dkpUpload = new()
            {
                AssociatedAttendanceCall = associatedCall,
                CharacterName = dkpEntry.CharacterName,
                DkpSpent = dkpAmount,
                Item = dkpEntry.Item,
                Timestamp = dkpEntry.Timestamp
            };
            dkpUploadInfos.Add(dkpUpload);

            Log.Debug($"{LogPrefix} DKP Upload: {dkpUpload} {(dkpAmount != dkpEntry.DkpSpent ? "DISCOUNTED" : string.Empty)}");
        }

        return dkpUploadInfos;
    }
}
