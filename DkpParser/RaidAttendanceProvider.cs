// -----------------------------------------------------------------------
// RaidAttendanceProvider.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using DkpParser.Uploading;
using Gjeltema.Logging;

public sealed class RaidAttendanceProvider : IRaidAttendance
{
    private const string LogPrefix = $"[{nameof(DkpServer)}]";
    public static readonly RaidAttendanceProvider Instance = new();
    private static int _attemptedInitialization = -1;
    private static Dictionary<string, CharacterRaidAttendance> _raidAttendances = [];

    private RaidAttendanceProvider() { }

    public static async Task<bool> InitializeAsync(IMusterDkpServer dkpServer)
    {
        if (Interlocked.Increment(ref _attemptedInitialization) > 0)
        {
            Interlocked.Decrement(ref _attemptedInitialization);
            return true;
        }

        try
        {
            Log.Debug($"{LogPrefix} Initializing character info.");
            ICollection<CharacterRaidAttendance> baseCharInfoFromServer = await dkpServer.GetAllCharactersBaseInfoAsync();
            if (baseCharInfoFromServer.Count == 0)
            {
                Log.Error($"{LogPrefix} Failed to initialize base character info.");
                Interlocked.Decrement(ref _attemptedInitialization);
                return false;
            }

            _raidAttendances = baseCharInfoFromServer.ToDictionary(x => x.CharacterName);

            ICollection<CharacterRaidAttendance> attendancesFromServer = await dkpServer.GetAllActiveCharacterAttendancesAsync();
            if (attendancesFromServer.Count == 0)
            {
                Log.Error($"{LogPrefix} Failed to initialize the raid attendances.");
                Interlocked.Decrement(ref _attemptedInitialization);
                return false;
            }

            foreach (CharacterRaidAttendance attendance in attendancesFromServer)
            {
                _raidAttendances[attendance.CharacterName] = attendance;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Failed to initialize the raid attendances: {ex.ToLogMessage()}");
            Interlocked.Decrement(ref _attemptedInitialization);
            return false;
        }

        return true;
    }

    public bool CharacterExistsOnDkpServer(string characterName)
        => _raidAttendances.Values.Any(x => x.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<CharacterRaidAttendance> GetAllRaidAttendances()
        => _raidAttendances.Values;

    public IEnumerable<CharacterRaidAttendance> GetAllRelatedCharactersForUser(string characterName)
    {
        CharacterRaidAttendance charInfo = GetCharacterRaidAttendance(characterName);
        if (charInfo == null)
            return [];

        return _raidAttendances.Values.Where(x => x.UserId == charInfo.UserId).ToList();
    }

    public CharacterRaidAttendance GetCharacterRaidAttendance(string characterName)
    {
        if (_raidAttendances.TryGetValue(characterName.NormalizeName(), out CharacterRaidAttendance ra))
            return ra;
        return null;
    }
}

public interface IRaidAttendance
{
    bool CharacterExistsOnDkpServer(string characterName);

    IEnumerable<CharacterRaidAttendance> GetAllRaidAttendances();

    IEnumerable<CharacterRaidAttendance> GetAllRelatedCharactersForUser(string characterName);

    CharacterRaidAttendance GetCharacterRaidAttendance(string characterName);
}
