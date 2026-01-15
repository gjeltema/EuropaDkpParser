// -----------------------------------------------------------------------
// DkpAdjustmentProcessor.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using Gjeltema.Logging;

public sealed class DkpAdjustmentProcessor : IDkpAdjustments
{
    private const string LogPrefix = $"[{nameof(DkpAdjustmentProcessor)}]";
    private const int NumberOfRaids = 250;
    private readonly DkpServerCharacters _charactersOnDkpServer;
    private readonly List<string> _classesWithDiscounts;
    private readonly List<DkpDiscountConfiguration> _discounts;
    private readonly IDkpServer _dkpServer;
    private readonly Dictionary<string, int> _raidAttendance = [];
    private static ICollection<PreviousRaid> _previousRaids = [];

    public DkpAdjustmentProcessor(IDkpServer dkpServer, DkpServerCharacters charactersOnDkpServer, IEnumerable<DkpDiscountConfiguration> discounts)
    {
        _dkpServer = dkpServer;
        _charactersOnDkpServer = charactersOnDkpServer;
        _discounts = discounts.ToList();
        _classesWithDiscounts = _discounts.Select(x => x.ClassName).Distinct().ToList();
    }

    public async Task<int> GetDkpDiscountedAmount(DkpEntry dkpEntry, string className, AttendanceEntry associatedAttendance)
    {
        Log.Trace($"{LogPrefix} Starting discount analysys for: {dkpEntry} {className}");

        if (string.IsNullOrEmpty(className))
        {
            DkpUserCharacter character = _charactersOnDkpServer.AllUserCharacters.FirstOrDefault(x => x.Name == dkpEntry.CharacterName);
            className = character.ClassName;
            Log.Trace($"{LogPrefix} Updated class name to: {className}");
        }

        string actualClass = EqClasses.GetClassFromTitle(className);

        if (!_classesWithDiscounts.Contains(actualClass))
        {
            Log.Trace($"{LogPrefix} No discounts found for class.");
            return dkpEntry.DkpSpent;
        }

        List<DkpDiscountConfiguration> possibleDiscountRules = GetPossibleDiscountRules(actualClass, associatedAttendance);
        if (possibleDiscountRules.Count == 0)
        {
            Log.Debug($"{LogPrefix} No discounts configured for {dkpEntry} in {associatedAttendance.ToDisplayString()}.");
            return dkpEntry.DkpSpent;
        }

        Log.Debug($"{LogPrefix} Evaluating {dkpEntry} for possible discount.");
        int raidAttendance = await GetRaidAttendance(dkpEntry.CharacterName);
        Log.Debug($"Calculated Raid Attendance for {dkpEntry.CharacterName}: {raidAttendance}%");

        DkpDiscountConfiguration discountRule = possibleDiscountRules
            .Where(x => x.MinimumRAThreshold <= raidAttendance)
            .MaxBy(x => x.DiscountFraction);

        if (discountRule == null)
        {
            Log.Debug($"{LogPrefix} Insufficient RA for for discount on {dkpEntry}.");
            return dkpEntry.DkpSpent;
        }

        Log.Debug($"{LogPrefix} Discount rule being applied: {discountRule.DebugText}.");
        int discountedAmount = (int)(discountRule.DiscountFraction * dkpEntry.DkpSpent);
        Log.Debug($"{LogPrefix} Discount for {dkpEntry} -> {discountedAmount}.");

        return discountedAmount < 1 ? 1 : discountedAmount;
    }

    private async Task<int> GetCharacterId(string characterName)
    {
        DkpUserCharacter character = _charactersOnDkpServer.AllUserCharacters.FirstOrDefault(x => x.Name == characterName);
        if (character != null)
            return character.CharacterId;

        int characterId = await _dkpServer.GetCharacterId(characterName);
        return characterId;
    }

    private List<DkpDiscountConfiguration> GetPossibleDiscountRules(string className, AttendanceEntry associatedAttendance)
    {
        List<DkpDiscountConfiguration> discountRules = _discounts
            .Where(x => x.ClassName == className)
            .Where(x => x.DiscountZoneOrBoss == associatedAttendance.CallName || x.DiscountZoneOrBoss == associatedAttendance.ZoneName)
            .ToList();

        return discountRules;
    }

    private async Task<int> GetRaidAttendance(string characterName)
    {
        if (_raidAttendance.TryGetValue(characterName, out int ra))
            return ra;

        int characterId = await GetCharacterId(characterName);
        Log.Debug($"{LogPrefix} CharacterID obtained: {characterId}.");
        if (characterId < 1)
            return 0;

        await SetPriorRaidsForAttendance();

        int numberOfRaidsAttended = _previousRaids.Count(x => x.CharacterIds.Contains(characterId));
        Log.Debug($"{LogPrefix} Character raids/Total raids: {numberOfRaidsAttended}/{_previousRaids.Count}.");
        if (numberOfRaidsAttended == 0)
            return 0;

        int raidAtt = numberOfRaidsAttended * 100 / _previousRaids.Count;
        _raidAttendance[characterName] = raidAtt;

        Log.Debug($"{LogPrefix} Calculated RA for ID {characterId}: {raidAtt}.");

        return raidAtt;
    }

    private async Task SetPriorRaidsForAttendance()
    {
        if (_previousRaids.Count > 0)
            return;

        DateTime thirtyDaysAgo = DateTime.Now.AddDays(-30);
        ICollection<PreviousRaid> priorRaids = await _dkpServer.GetPriorRaids(NumberOfRaids);
        _previousRaids = priorRaids.Where(x => x.RaidTime > thirtyDaysAgo).OrderBy(x => x.RaidTime).ToList();

        Log.Debug($"{LogPrefix} Last 30 days of raids:{Environment.NewLine}{string.Join(Environment.NewLine, _previousRaids.Select(x => x.ToString()))}");
    }
}

public interface IDkpAdjustments
{
    Task<int> GetDkpDiscountedAmount(DkpEntry dkpEntry, string className, AttendanceEntry associatedAttendance);
}
