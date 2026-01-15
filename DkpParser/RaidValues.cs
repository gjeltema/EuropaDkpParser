// -----------------------------------------------------------------------
// RaidValues.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.IO;
using DkpParser.Uploading;
using Gjeltema.Logging;

public sealed class RaidValues : IRaidValues
{
    private const string AliasSection = "ZONE_ALIAS_SECTION";
    private const string BonusZonesSection = "BONUS_ZONES_SECTION";
    private const string BossNoDruzzilSection = "BOSS_NO_DRUZZIL_SECTION";
    private const string BossSection = "BOSS_SECTION";
    private const string Comment = "#";
    private const char Delimiter = '\t';
    private const string DkpDiscountsSection = "DKP_DISCOUNTS_SECTION";
    private const string LogPrefix = $"[{nameof(RaidValues)}]";
    private const string SectionEnding = "_END";
    private const string TierSection = "TIER_SECTION";
    private const string ZoneValueSection = "ZONE_SECTION";
    private readonly List<BossKillValue> _bossKillValues = [];
    private readonly string _raidValuesFileName;
    private readonly Dictionary<int, int> _tiers = [];
    private readonly Dictionary<string, string> _zoneRaidAliases = [];
    private readonly List<ZoneValue> _zoneValues = [];
    private List<string> _bonusZones;
    private List<string> _bossesNoDruzzil;

    public RaidValues(string raidValuesFileName)
    {
        _raidValuesFileName = raidValuesFileName;
    }

    public ICollection<string> AllBossMobNames
        => _bossKillValues.Select(x => x.BossName).ToList();

    public ICollection<string> AllValidRaidZoneNames { get; private set; }

    public IEnumerable<string> BossesWithNoDruzzilMessage
        => _bossesNoDruzzil;

    public IEnumerable<DkpDiscountConfiguration> DkpDiscounts { get; private set; }

    public int GetDkpValueForRaid(AttendanceUploadInfo attendanceEntry)
    {
        int dkpValue = attendanceEntry.AttendanceCallType == AttendanceCallType.Time
            ? GetTimeBasedValue(attendanceEntry.ZoneName)
            : GetBossKillValue(attendanceEntry.CallName, attendanceEntry.ZoneName);
        return dkpValue;
    }

    public string GetZoneRaidAlias(string zoneName)
        => _zoneRaidAliases.TryGetValue(zoneName ?? "", out string alias) ? alias : zoneName ?? "";

    public bool IsBonusZone(string zoneName)
    {
        string aliasName = GetZoneRaidAlias(zoneName);
        return _bonusZones.Contains(aliasName);
    }

    public void LoadSettings()
    {
        if (!File.Exists(_raidValuesFileName))
        {
            Log.Error($"{LogPrefix} {_raidValuesFileName} does not exist.");
            FileInfo fi = new(_raidValuesFileName);
            throw new FileNotFoundException($"{_raidValuesFileName} does not exist.", fi.FullName);
        }

        string[] fileContents = File.ReadAllLines(_raidValuesFileName);
        LoadTierSection(fileContents);
        LoadBossSection(fileContents);
        LoadZoneSection(fileContents);
        LoadZoneAliasSection(fileContents);
        LoadBonusZones(fileContents);
        LoadDkpDiscountsSection(fileContents);

        AllValidRaidZoneNames = _zoneValues.Select(x => x.ZoneName).Union(_zoneRaidAliases.Keys).Order().ToList();
    }

    private static int GetStartingIndex(IList<string> fileContents, string configurationSectionName)
        => fileContents.IndexOf(configurationSectionName);

    private static bool IsValidIndex(int indexOfSection, ICollection<string> fileContents)
        => 0 <= indexOfSection && indexOfSection < fileContents.Count - 1;

    private List<string> GetAllEntriesInSection(string[] fileContents, string key)
    {
        List<string> entries = [];
        int index = GetStartingIndex(fileContents, key);
        if (!IsValidIndex(index, fileContents))
            return entries;

        string sectionEnd = key + SectionEnding;
        index++;
        string entry = fileContents[index];
        while (entry != sectionEnd)
        {
            if (!entry.StartsWith(Comment))
            {
                entries.Add(entry);
            }

            index++;
            entry = fileContents[index];
        }

        return entries;
    }

    private int GetBossKillValue(string bossName, string zoneName)
    {
        BossKillValue boss = _bossKillValues.FirstOrDefault(x => x.BossName == bossName);
        if (boss == null)
            return GetTimeBasedValue(zoneName);

        if (boss.UseOverrideValue)
            return boss.OverrideValue;

        int tierValue = boss.Tier >= 0
            ? GetTierValue(boss.Tier)
            : GetTimeBasedValue(zoneName);

        return tierValue;
    }

    private int GetTierValue(int tier)
    {
        if (_tiers.TryGetValue(tier, out int configuredTierValue))
            return configuredTierValue;
        return tier;
    }

    private int GetTimeBasedValue(string zoneName)
    {
        string alias = GetZoneRaidAlias(zoneName);
        ZoneValue zone = _zoneValues.FirstOrDefault(x => x.ZoneName == alias);
        if (zone == null)
            return 0;

        if (zone.UseOverrideValue)
            return zone.OverrideValue;

        int tierValue = GetTierValue(zone.Tier);
        return tierValue;
    }

    private void LoadBonusZones(string[] fileContents)
    {
        _bonusZones = GetAllEntriesInSection(fileContents, BonusZonesSection);
    }

    private void LoadBossNoDruzzilSection(string[] fileContents)
    {
        _bossesNoDruzzil = GetAllEntriesInSection(fileContents, BossNoDruzzilSection);
    }

    private void LoadBossSection(string[] fileContents)
    {
        ICollection<string> entries = GetAllEntriesInSection(fileContents, BossSection);
        if (entries.Count == 0)
        {
            Log.Warning($"{LogPrefix} No entries found for section {BossSection}.");
            return;
        }

        foreach (string entry in entries)
        {
            string[] values = entry.Split(Delimiter);
            string bossName = values[0];
            int tierValue = int.Parse(values[1]);
            int overrideValue = int.Parse(values[2]);

            BossKillValue boss = new()
            {
                BossName = bossName,
                Tier = tierValue,
                OverrideValue = overrideValue
            };

            _bossKillValues.Add(boss);
        }
    }

    private void LoadDkpDiscountsSection(string[] fileContents)
    {
        // Classname    DiscountZoneOrBoss  RAThreshold DiscountFraction
        List<DkpDiscountConfiguration> configurations = [];
        List<string> entries = GetAllEntriesInSection(fileContents, DkpDiscountsSection);
        foreach (string entry in entries)
        {
            string[] values = entry.Split(Delimiter);
            string className = values[0];
            string zoneOrBoss = values[1];
            int raThreshold = int.Parse(values[2]);
            double discountFraction = int.Parse(values[3]) / 100.0;

            DkpDiscountConfiguration config = new()
            {
                ClassName = className,
                DiscountZoneOrBoss = zoneOrBoss,
                MinimumRAThreshold = raThreshold,
                DiscountFraction = discountFraction
            };

            configurations.Add(config);
        }

        DkpDiscounts = configurations;
    }

    private void LoadTierSection(string[] fileContents)
    {
        ICollection<string> entries = GetAllEntriesInSection(fileContents, TierSection);
        foreach (string entry in entries)
        {
            string[] values = entry.Split(Delimiter);
            int tierNumber = int.Parse(values[0]);
            int tierValue = int.Parse(values[1]);

            _tiers[tierNumber] = tierValue;
        }
    }

    private void LoadZoneAliasSection(string[] fileContents)
    {
        ICollection<string> entries = GetAllEntriesInSection(fileContents, AliasSection);
        if (entries.Count == 0)
        {
            Log.Warning($"{LogPrefix} No entries found for section {AliasSection}.");
            return;
        }

        foreach (string entry in entries)
        {
            string[] values = entry.Split(Delimiter);
            string zoneName = values[0];
            string alias = values[1];

            _zoneRaidAliases[zoneName] = alias;
        }
    }

    private void LoadZoneSection(string[] fileContents)
    {
        ICollection<string> entries = GetAllEntriesInSection(fileContents, ZoneValueSection);
        if (entries.Count == 0)
        {
            Log.Warning($"{LogPrefix} No entries found for section {ZoneValueSection}.");
            return;
        }

        foreach (string entry in entries)
        {
            string[] values = entry.Split(Delimiter);
            string zoneName = values[0];
            int tierValue = int.Parse(values[1]);
            int overrideValue = int.Parse(values[2]);

            ZoneValue zone = new()
            {
                ZoneName = zoneName,
                Tier = tierValue,
                OverrideValue = overrideValue
            };

            _zoneValues.Add(zone);
        }
    }

    [DebuggerDisplay("{BossName}")]
    private sealed class BossKillValue
    {
        public string BossName { get; init; }

        public int OverrideValue { get; init; } = -1;

        public int Tier { get; init; }

        public bool UseOverrideValue
            => OverrideValue >= 0;
    }

    [DebuggerDisplay("{ZoneName}")]
    private sealed class ZoneValue
    {
        public int OverrideValue { get; init; } = -1;

        public int Tier { get; init; }

        public bool UseOverrideValue
            => OverrideValue >= 0;

        public string ZoneName { get; init; }
    }
}

public sealed class ClassBonus
{
    public string ClassName { get; init; }

    /// <summary>
    /// Returns e.g. 0.25 for a 25% bonus.
    /// </summary>
    public double PercentBonus { get; init; }
}

public interface IRaidValues
{
    ICollection<string> AllBossMobNames { get; }

    ICollection<string> AllValidRaidZoneNames { get; }

    IEnumerable<string> BossesWithNoDruzzilMessage { get; }

    IEnumerable<DkpDiscountConfiguration> DkpDiscounts { get; }

    int GetDkpValueForRaid(AttendanceUploadInfo attendanceEntry);

    string GetZoneRaidAlias(string zoneName);

    bool IsBonusZone(string zoneName);

    void LoadSettings();
}
