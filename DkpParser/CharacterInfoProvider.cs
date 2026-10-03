// -----------------------------------------------------------------------
// CharacterInfoProvider.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using DkpParser.Uploading;
using Gjeltema.Logging;

public sealed class CharacterInfoProvider : IRaidAttendance
{
    private const string LogPrefix = $"[{nameof(DkpServer)}]";
    public static readonly CharacterInfoProvider Instance = new();
    private static int _attemptedInitialization = -1;
    private static Dictionary<string, CharacterServerInfo> _raidAttendances = [];

    private CharacterInfoProvider() { }

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
            ICollection<CharacterServerInfo> baseCharInfoFromServer = await dkpServer.GetAllCharactersBaseInfoAsync();
            if (baseCharInfoFromServer.Count == 0)
            {
                Log.Error($"{LogPrefix} Failed to initialize base character info.");
                Interlocked.Decrement(ref _attemptedInitialization);
                return false;
            }

            _raidAttendances = baseCharInfoFromServer.ToDictionary(x => x.CharacterName);

            ICollection<CharacterServerInfo> attendancesFromServer = await dkpServer.GetAllActiveCharacterAttendancesAsync();
            if (attendancesFromServer.Count == 0)
            {
                Log.Error($"{LogPrefix} Failed to initialize the raid attendances.");
                Interlocked.Decrement(ref _attemptedInitialization);
                return false;
            }

            foreach (CharacterServerInfo attendance in attendancesFromServer)
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

    public IEnumerable<CharacterServerInfo> GetAllCharactersInfo()
        => _raidAttendances.Values;

    public IEnumerable<CharacterServerInfo> GetAllRelatedCharactersForUser(string characterName)
    {
        CharacterServerInfo charInfo = GetCharacterRaidAttendance(characterName);
        if (charInfo == null)
            return [];

        return _raidAttendances.Values.Where(x => x.UserId == charInfo.UserId).ToList();
    }

    public CharacterServerInfo GetCharacterRaidAttendance(string characterName)
    {
        if (_raidAttendances.TryGetValue(characterName.NormalizeName(), out CharacterServerInfo ra))
            return ra;
        return null;
    }
}

public interface IRaidAttendance
{
    bool CharacterExistsOnDkpServer(string characterName);

    IEnumerable<CharacterServerInfo> GetAllCharactersInfo();

    IEnumerable<CharacterServerInfo> GetAllRelatedCharactersForUser(string characterName);

    CharacterServerInfo GetCharacterRaidAttendance(string characterName);
}
