// -----------------------------------------------------------------------
// RaidAttendanceProvider.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using Gjeltema.Logging;

public sealed class RaidAttendanceProvider : IRaidAttendance
{
    private const string LogPrefix = $"[{nameof(DkpServer)}]";
    public static readonly RaidAttendanceProvider Instance = new();
    private static int _attemptedInitialization = -1;
    private static Dictionary<string, CharacterRaidAttendance> _raidAttendances = [];

    private RaidAttendanceProvider() { }

    public static async Task<bool> InitializeAsync(IDkpServer dkpServer)
    {
        if (Interlocked.Increment(ref _attemptedInitialization) > 0)
        {
            Interlocked.Decrement(ref _attemptedInitialization);
            return true;
        }

        try
        {
            Log.Debug($"{LogPrefix} Initializing attendances.");
            ICollection<CharacterRaidAttendance> attendancesFromServer = await dkpServer.GetAllCharacterAttendancesAsync();
            if (attendancesFromServer.Count == 0)
            {
                Log.Error($"{LogPrefix} Failed to initialize the raid attendances.");
                Interlocked.Decrement(ref _attemptedInitialization);
                return false;
            }

            _raidAttendances = attendancesFromServer.OrderByDescending(x => x.Character30DayRa).ToDictionary(x => x.CharacterName);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Failed to initialize the raid attendances: {ex.ToLogMessage()}");
            Interlocked.Decrement(ref _attemptedInitialization);
            return false;
        }

        return true;
    }

    public IEnumerable<CharacterRaidAttendance> GetAllRaidAttendances()
        => _raidAttendances.Values;

    public CharacterRaidAttendance GetCharacterRaidAttendance(string characterName)
    {
        if (_raidAttendances.TryGetValue(characterName.NormalizeName(), out CharacterRaidAttendance ra))
            return ra;
        return null;
    }
}

public interface IRaidAttendance
{
    IEnumerable<CharacterRaidAttendance> GetAllRaidAttendances();

    CharacterRaidAttendance GetCharacterRaidAttendance(string characterName);
}
