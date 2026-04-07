// -----------------------------------------------------------------------
// DkpAdjustmentProcessor.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using Gjeltema.Logging;

public sealed class DkpAdjustmentProcessor : IDkpAdjustments
{
    private const string LogPrefix = $"[{nameof(DkpAdjustmentProcessor)}]";
    private readonly DkpServerCharacters _charactersOnDkpServer;
    private readonly List<string> _classesWithDiscounts;
    private readonly List<DkpDiscountConfiguration> _discounts;
    private readonly IRaidAttendance _raidAttendances;

    public DkpAdjustmentProcessor(IDkpParserSettings settings, IRaidAttendance raidAttendances)
    {
        _charactersOnDkpServer = settings.CharactersOnDkpServer;
        _raidAttendances = raidAttendances;
        _discounts = settings.RaidValue.DkpDiscounts.ToList();
        _classesWithDiscounts = _discounts.Select(x => x.ClassName).Distinct().ToList();
    }

    public async Task<int> GetDkpDiscountedAmountAsync(DkpEntry dkpEntry, string className, AttendanceEntry associatedAttendance)
    {
        Log.Trace($"{LogPrefix} Starting discount analysis for: {dkpEntry} {className}");

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
        double raidAttendance = await GetRaidAttendanceAsync(dkpEntry.CharacterName);
        Log.Debug($"Calculated Raid Attendance for {dkpEntry.CharacterName}: {raidAttendance:0.00}%");

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

    private List<DkpDiscountConfiguration> GetPossibleDiscountRules(string className, AttendanceEntry associatedAttendance)
    {
        List<DkpDiscountConfiguration> discountRules = _discounts
            .Where(x => x.ClassName == className)
            .Where(x => x.DiscountZoneOrBoss == associatedAttendance.CallName || x.DiscountZoneOrBoss == associatedAttendance.ZoneName)
            .ToList();

        return discountRules;
    }

    private async Task<double> GetRaidAttendanceAsync(string characterName)
    {
        RaidAttendanceInfo raidAttInfo = await _raidAttendances.Get30DayRaidAttendanceAsync(characterName);
        return raidAttInfo.ThirtyDayRA;
    }
}

public interface IDkpAdjustments
{
    Task<int> GetDkpDiscountedAmountAsync(DkpEntry dkpEntry, string className, AttendanceEntry associatedAttendance);
}
