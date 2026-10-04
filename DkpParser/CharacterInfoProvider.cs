// -----------------------------------------------------------------------
// CharacterInfoProvider.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using DkpParser.Uploading;
using Gjeltema.Logging;

public sealed class CharacterInfoProvider : ICharacterInfo
{
    private const string LogPrefix = $"[{nameof(CharacterInfoProvider)}]";
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

    public ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(string characterName)
    {
        CharacterServerInfo charInfo = GetCharacterInfo(characterName);
        if (charInfo == null)
            return [];

        return _raidAttendances.Values.Where(x => x.UserId == charInfo.UserId).ToList();
    }

    public ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(CharacterServerInfo characterInfo)
        => _raidAttendances.Values.Where(x => x.UserId == characterInfo.UserId).ToList();

    public CharacterServerInfo GetCharacterInfo(string characterName)
    {
        if (_raidAttendances.TryGetValue(characterName?.NormalizeName(), out CharacterServerInfo ra))
            return ra;
        return null;
    }

    public bool IsRelatedCharacterInCollection(PlayerCharacter character, IEnumerable<PlayerCharacter> characters)
    {
        CharacterServerInfo charInfo = GetCharacterInfo(character.CharacterName);
        if (charInfo == null)
            return false;

        List<CharacterServerInfo> relatedChars = GetAllRelatedCharactersForUser(charInfo).ToList();
        if (relatedChars.Count == 0)
            return false;

        foreach (PlayerCharacter characterInList in characters)
        {
            if (relatedChars.Any(x => x.CharacterName.Equals(characterInList.CharacterName, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }
}

public interface ICharacterInfo
{
    bool CharacterExistsOnDkpServer(string characterName);

    IEnumerable<CharacterServerInfo> GetAllCharactersInfo();

    ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(string characterName);

    ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(CharacterServerInfo characterInfo);

    CharacterServerInfo GetCharacterInfo(string characterName);
}
