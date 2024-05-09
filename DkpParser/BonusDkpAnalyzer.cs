// -----------------------------------------------------------------------
// BonusDkpAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class BonusDkpAnalyzer : IBonusDkpAnalyzer
{
    private const int MinimumThresholdOfTimeCalls = 8;
    private const int NumberOfCallsCanMiss = 2;
    private readonly IDkpParserSettings _settings;

    public BonusDkpAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public void AddBonusAttendance(RaidEntries raidEntries)
    {
        if (!_settings.AddBonusDkpRaid)
            return;

        string zoneName = null;
        foreach (string zone in raidEntries.Raids.Select(x => x.RaidZone))
        {
            if (_settings.RaidValue.IsBonusZone(zone))
                zoneName = zone;
        }

        if (zoneName == null)
            return;

        int totalAttendanceCalls = raidEntries.AttendanceEntries.Count(x => x.AttendanceCallType == AttendanceCallType.Time);
        if (totalAttendanceCalls < MinimumThresholdOfTimeCalls)
            return;

        int numberOfRaidsToGetBonus = totalAttendanceCalls - NumberOfCallsCanMiss;

        DateTime lastTimestamp = raidEntries.AttendanceEntries.Max(x => x.Timestamp);

        AttendanceEntry bonusEntry = new()
        {
            AttendanceCallType = AttendanceCallType.Time,
            CallName = "Bonus for " + zoneName,
            ZoneName = zoneName,
            Timestamp = lastTimestamp.AddMinutes(10),
        };

        foreach (PlayerCharacter player in raidEntries.AllPlayersInRaid)
        {
            int numberOfAttendances = raidEntries.AttendanceEntries.Count(x => x.Players.Contains(player));
            if (numberOfAttendances >= numberOfRaidsToGetBonus)
            {
                bonusEntry.AddOrMergeInPlayerCharacter(player);
            }
        }

        if (bonusEntry.Players.Count > 1)
        {
            raidEntries.AttendanceEntries.Add(bonusEntry);
        }
    }
}

public interface IBonusDkpAnalyzer
{
    void AddBonusAttendance(RaidEntries raidEntries);
}
