// -----------------------------------------------------------------------
// RaidEntries.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("Att: {AttendanceEntries.Count}, DKP: {DkpEntries.Count}, Players: {AllCharactersInRaid.Count}")]
public sealed class RaidEntries
{
    private static readonly TimeSpan KillCallToClose = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MaxTimeThresholdForKillCall = TimeSpan.FromMinutes(15);

    public ICollection<AfkEntry> AfkEntries { get; } = new List<AfkEntry>();

    public ICollection<PlayerCharacter> AllCharactersInRaid { get; set; } = new HashSet<PlayerCharacter>();

    public ICollection<AttendanceEntry> AttendanceEntries { get; set; } = new List<AttendanceEntry>();

    public ICollection<CharacterJoinRaidEntry> CharacterJoinCalls { get; set; } = new List<CharacterJoinRaidEntry>();

    public ICollection<DiscountApplied> Discounts { get; init; } = new List<DiscountApplied>();

    public ICollection<DkpEntry> DkpEntries { get; set; } = new List<DkpEntry>();

    public ICollection<DkpEntry> DkpUploadErrors { get; set; } = new List<DkpEntry>();

    public ICollection<MultipleCharsOnAttendanceError> MultipleCharsInAttendanceErrors { get; set; } = new List<MultipleCharsOnAttendanceError>();

    public ICollection<PlayerLooted> PlayerLootedEntries { get; set; }

    public ICollection<PlayerPossibleLinkdead> PossibleLinkdeads { get; } = new List<PlayerPossibleLinkdead>();

    public ICollection<DkpEntry> RemovedDkpEntries { get; set; } = new List<DkpEntry>();

    public ICollection<PlayerCharacter> RemovedPlayerCharacters { get; } = new HashSet<PlayerCharacter>();

    public ICollection<DkpTransfer> Transfers { get; set; } = new List<DkpTransfer>();

    public void AddOrMergeInPlayerCharacter(PlayerCharacter playerCharacter)
    {
        PlayerCharacter currentChar = AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == playerCharacter.CharacterName);
        if (currentChar != null)
        {
            currentChar.Merge(playerCharacter);
        }
        else
        {
            AllCharactersInRaid.Add(playerCharacter);
        }
    }

    public IEnumerable<string> GetAllDkpspentEntries(Func<string, string> getZoneRaidAlias)
    {
        string currentZoneName = string.Empty;
        foreach (DkpEntry dkpEntry in DkpEntries.OrderBy(x => x.Timestamp))
        {
            AttendanceEntry associatedAttendance = GetAssociatedAttendance(dkpEntry);
            string attendanceZone = getZoneRaidAlias(associatedAttendance.ZoneName);
            if (attendanceZone != currentZoneName)
            {
                yield return "";

                currentZoneName = attendanceZone;
                yield return $"================= {currentZoneName} =================";
            }

            yield return dkpEntry.ToSummaryDisplay();
        }

        yield return "";
    }

    public IEnumerable<string> GetAllEntries()
    {
        yield return "-------------------- Attendance Entries -------------------";
        foreach (AttendanceEntry attEntry in AttendanceEntries)
        {
            yield return attEntry.ToDebugString();
            foreach (PlayerCharacter player in attEntry.Characters)
            {
                yield return player.ToDisplayString();
            }
        }

        yield return "";

        yield return "-------------------- DKP Entries -------------------";
        foreach (DkpEntry dkpEntry in DkpEntries)
            yield return dkpEntry.ToDebugString();

        yield return "";

        yield return "-------------------- Player Looted Entries -------------------";
        foreach (PlayerLooted playerLootedEntry in PlayerLootedEntries)
            yield return playerLootedEntry.ToString();

        yield return "";

        yield return "-------------------- All Players Found In Raid -------------------";
        foreach (PlayerCharacter playerInRaid in AllCharactersInRaid.OrderBy(x => x.CharacterName))
            yield return playerInRaid.ToDisplayString();

        yield return "";

        yield return "-------------------- Players Joined or Left Raid -------------------";
        foreach (CharacterJoinRaidEntry playerJoinedRaid in CharacterJoinCalls.OrderBy(x => x.Timestamp))
            yield return playerJoinedRaid.ToDisplayString();

        yield return "";

        yield return "-------------------- Players Declared AFK -------------------";
        foreach (AfkEntry afkEntry in AfkEntries.OrderBy(x => x.StartTime))
            yield return afkEntry.ToDisplayString();

        yield return "";

        yield return "-------------------- DKP Transfers -------------------";
        foreach (DkpTransfer transfer in Transfers)
            yield return transfer.ToDisplayString();

        yield return "";

        yield return "-------------------- Multiple Characers from same account in Attendance -------------------";
        foreach (MultipleCharsOnAttendanceError multipleChars in MultipleCharsInAttendanceErrors)
            yield return
                $"{multipleChars.MultipleCharsInAttendance.FirstCharacter} and {multipleChars.MultipleCharsInAttendance.FirstCharacter} in {multipleChars.Attendance.ToDisplayString()}";

        yield return "";
    }

    public AttendanceEntry GetAssociatedAttendance(DkpEntry dkpEntry)
    {
        if (AttendanceEntries == null || AttendanceEntries.Count == 0)
            return null;

        // Find the most prior kill call.  If it's within the time threshold to the SPENT call, check if it's *too* close.
        // If it's very close, search for the kill call prior to that one.  If that one is in range, use it.  If not, just
        // use the most prior kill call.  This should handle multiple boss kills in quick succession.
        // If no kill calls are within range prior, use the next Time call - assume it's a drop from trash.

        DateTime referenceTime = dkpEntry.Timestamp;
        AttendanceEntry killCallPrior = AttendanceEntries
            .Where(x => x.AttendanceCallType == AttendanceCallType.Kill && x.Timestamp < referenceTime)
            .MaxBy(x => x.Timestamp);

        if (killCallPrior != null)
        {
            TimeSpan timeDifference = referenceTime - killCallPrior.Timestamp;
            if (timeDifference <= KillCallToClose)
            {
                AttendanceEntry killCallSecondPrior = AttendanceEntries
                    .Where(x => x.AttendanceCallType == AttendanceCallType.Kill && x.Timestamp < killCallPrior.Timestamp)
                    .MaxBy(x => x.Timestamp);

                if (killCallSecondPrior != null)
                {
                    TimeSpan timeDifferenceToSecond = referenceTime - killCallSecondPrior.Timestamp;
                    if (timeDifferenceToSecond <= MaxTimeThresholdForKillCall)
                    {
                        return killCallSecondPrior;
                    }
                }
            }

            if (timeDifference <= MaxTimeThresholdForKillCall)
            {
                return killCallPrior;
            }
        }

        AttendanceEntry firstTimeCallAfter = AttendanceEntries
            .Where(x => x.AttendanceCallType == AttendanceCallType.Time && dkpEntry.Timestamp < x.Timestamp)
            .MinBy(x => x.Timestamp);

        return firstTimeCallAfter ?? AttendanceEntries.Last();
    }

    public bool IsPlayerAfkFlagged(PlayerCharacter character, DateTime timestamp)
        => AfkEntries
            .Where(x => x.Character == character)
            .Any(x => x.IsTimeWithinAfkPeriod(timestamp));

    public void RemoveAttendance(AttendanceEntry toBeRemoved)
    {
        List<PlayerPossibleLinkdead> possibleLinkdeadsToRemove = PossibleLinkdeads.Where(x => x.AttendanceMissingFrom == toBeRemoved).ToList();
        foreach (PlayerPossibleLinkdead possibleLinkdead in possibleLinkdeadsToRemove)
        {
            PossibleLinkdeads.Remove(possibleLinkdead);
        }

        AttendanceEntries.Remove(toBeRemoved);
    }

    public ICollection<DkpEntry> RemoveCharacter(string characterName)
    {
        ICollection<DkpEntry> dkpSpentsToRemove = DkpEntries.Where(x => x.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (DkpEntry dkpToRemove in dkpSpentsToRemove)
        {
            DkpEntries.Remove(dkpToRemove);
            RemovedDkpEntries.Add(dkpToRemove);
        }

        PlayerCharacter playerChar = AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == characterName);
        if (playerChar == null)
            return dkpSpentsToRemove;

        ICollection<PlayerPossibleLinkdead> possibleLinkdeadsToRemove = PossibleLinkdeads.Where(x => x.Player == playerChar).ToList();
        foreach (PlayerPossibleLinkdead ldChar in possibleLinkdeadsToRemove)
        {
            PossibleLinkdeads.Remove(ldChar);
        }

        IEnumerable<AttendanceEntry> attendancesToRemoveFrom = AttendanceEntries.Where(x => x.Characters.Contains(playerChar));
        foreach (AttendanceEntry attendance in attendancesToRemoveFrom)
        {
            attendance.Characters.Remove(playerChar);
        }

        AllCharactersInRaid.Remove(playerChar);
        RemovedPlayerCharacters.Add(playerChar);

        return dkpSpentsToRemove;
    }
}
