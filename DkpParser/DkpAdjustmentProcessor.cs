// -----------------------------------------------------------------------
// DkpAdjustmentProcessor.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpAdjustmentProcessor : IDkpAdjustments
{
    private const int NumberOfRaids = 250;
    private readonly DkpServerCharacters _charactersOnDkpServer;
    private readonly List<DkpDiscountConfiguration> _discounts;
    private readonly IDkpServer _dkpServer;
    private static ICollection<PreviousRaid> _previousRaids = null;

    public DkpAdjustmentProcessor(IDkpServer dkpServer, DkpServerCharacters charactersOnDkpServer, IEnumerable<DkpDiscountConfiguration> discounts)
    {
        _dkpServer = dkpServer;
        _charactersOnDkpServer = charactersOnDkpServer;
        _discounts = discounts.ToList();
    }

    public async Task<int> GetDkpDiscount(string characterName, int winningBidAmount, string className, AttendanceEntry associatedAttendance)
    {
        _previousRaids ??= await _dkpServer.GetPriorRaids(NumberOfRaids);
        return 0;
    }
}

public interface IDkpAdjustments
{
    Task<int> GetDkpDiscount(string characterName, int winningBidAmount, string className, AttendanceEntry associatedAttendance);
}
