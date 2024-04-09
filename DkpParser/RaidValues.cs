// -----------------------------------------------------------------------
// RaidValues.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.IO;

public sealed class RaidValues : IRaidValues
{
    private const string AliasSection = "ZONE_ALIAS_SECTION";
    private const string BossSection = "BOSS_SECTION";
    private const string Comment = "#";
    private const char Delimiter = '\t';
    private const string SectionEnding = "_END";
    private const string TierSection = "TIER_SECTION";
    private const string ZoneValueSection = "ZONE_SECTION";
    private readonly List<BossKillValue> _bossKillValues = [];
    private readonly string _raidValuesFileName;
    private readonly Dictionary<int, int> _tiers = [];
    private readonly Dictionary<string, string> _zoneRaidAliases = [];
    private readonly List<ZoneValue> _zoneValues = [];

    public RaidValues(string raidValuesFileName)
    {
        _raidValuesFileName = raidValuesFileName;
    }

    public ICollection<string> AllBossMobNames
        => _bossKillValues.Select(x => x.BossName).ToList();

    public int GetBossKillValue(string bossName)
    {
        BossKillValue boss = _bossKillValues.FirstOrDefault(x => x.BossName == bossName);
        if (boss == null)
            return 0;

        if (boss.UseOverrideValue)
            return boss.OverrideValue;

        int tierValue = _tiers[boss.Tier];
        return tierValue;
    }

    public int GetTimeBasedValue(string zoneName)
    {
        string alias = GetZoneRaidAlias(zoneName);
        ZoneValue zone = _zoneValues.FirstOrDefault(x => x.ZoneName == alias);
        if (zone == null)
            return 0;

        if (zone.UseOverrideValue)
            return zone.OverrideValue;

        int tierValue = _tiers[zone.Tier];
        return tierValue;
    }

    public string GetZoneRaidAlias(string zoneName)
        => _zoneRaidAliases.TryGetValue(zoneName, out string alias) ? alias : zoneName;

    public void LoadSettings()
    {
        if (!File.Exists(_raidValuesFileName))
        {
            return;
        }

        string[] fileContents = File.ReadAllLines(_raidValuesFileName);
        LoadTierSection(fileContents);
        LoadBossSection(fileContents);
        LoadZoneSection(fileContents);
        LoadZoneAliasSection(fileContents);
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

    private void LoadBossSection(string[] fileContents)
    {
        ICollection<string> entries = GetAllEntriesInSection(fileContents, BossSection);
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

public interface IRaidValues
{
    ICollection<string> AllBossMobNames { get; }

    int GetBossKillValue(string bossName);

    int GetTimeBasedValue(string zoneName);

    string GetZoneRaidAlias(string zoneName);

    void LoadSettings();
}
