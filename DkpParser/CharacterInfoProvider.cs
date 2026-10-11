// -----------------------------------------------------------------------
// CharacterInfoProvider.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using DkpParser.Uploading;
using Gjeltema.Logging;

public sealed class CharacterInfoProvider : ICharacterInfo
{
    private const string LogPrefix = $"[{nameof(CharacterInfoProvider)}]";
    public static readonly ICharacterInfo Instance = new CharacterInfoProvider();
    private Dictionary<string, CharacterServerInfo> _characterInfo = [];
    private IMusterDkpServer _dkpServer;

    private CharacterInfoProvider() { }

    public Task<bool> InitializeCharacterInfoAsync { get; private set; }

    public bool CharacterExistsOnDkpServer(string characterName)
        => _characterInfo.Values.Any(x => x.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<CharacterServerInfo> GetAllCharactersInfo()
        => _characterInfo.Values;

    public ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(string characterName)
    {
        CharacterServerInfo charInfo = GetCharacterInfo(characterName);
        if (charInfo == null)
            return [];

        return _characterInfo.Values.Where(x => x.UserId == charInfo.UserId).ToList();
    }

    public ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(CharacterServerInfo characterInfo)
        => _characterInfo.Values.Where(x => x.UserId == characterInfo.UserId).ToList();

    public CharacterServerInfo GetCharacterInfo(string characterName)
    {
        if (_characterInfo.TryGetValue(characterName?.NormalizeName(), out CharacterServerInfo ra))
            return ra;
        return null;
    }

    public void InitializeServer(IMusterDkpServer dkpServer)
    {
        _dkpServer = dkpServer;
        InitializeCharacterInfoAsync = RefreshCharacterInfoAsync();
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

    public async Task<bool> RefreshCharacterInfoAsync()
    {
        Dictionary<string, CharacterServerInfo> characterInfo = [];
        bool success = false;
        int attempt = 0;
        while (!success && attempt < 3)
        {
            attempt++;

            try
            {
                Log.Debug($"{LogPrefix} Initializing character info.");
                ICollection<CharacterServerInfo> baseCharInfoFromServer = await _dkpServer.GetAllCharactersBaseInfoAsync();
                if (baseCharInfoFromServer.Count == 0)
                {
                    Log.Error($"{LogPrefix} Failed to get the base character info.  Attempt #{attempt}.");
                    continue;
                }

                characterInfo = baseCharInfoFromServer.ToDictionary(x => x.CharacterName);

                ICollection<CharacterServerInfo> attendancesFromServer = await _dkpServer.GetAllActiveCharacterAttendancesAsync();
                if (attendancesFromServer.Count == 0)
                {
                    Log.Error($"{LogPrefix} Failed to get the character info with RA.  Attempt #{attempt}.");
                    continue;
                }

                foreach (CharacterServerInfo attendance in attendancesFromServer)
                {
                    characterInfo[attendance.CharacterName] = attendance;
                }

                success = true;
            }
            catch (Exception ex)
            {
                Log.Error($"{LogPrefix}  Attempt #{attempt}. Failed to initialize the character info: {ex.ToLogMessage()}");
            }
        }

        _characterInfo = characterInfo;
        Log.Debug($"{LogPrefix} Character info initialized.");
        return success;
    }
}

public interface ICharacterInfo
{
    /// <summary>
    /// Triggers an initialization of the locally stored character info, if it has not already done so.<br/>
    /// Blocks until the initialization is complete (returns true), or fails (returns false).<br/>
    /// Only initializes once, no matter how many times this is called.
    /// </summary>
    Task<bool> InitializeCharacterInfoAsync { get; }

    bool CharacterExistsOnDkpServer(string characterName);

    IEnumerable<CharacterServerInfo> GetAllCharactersInfo();

    ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(string characterName);

    ICollection<CharacterServerInfo> GetAllRelatedCharactersForUser(CharacterServerInfo characterInfo);

    CharacterServerInfo GetCharacterInfo(string characterName);

    bool IsRelatedCharacterInCollection(PlayerCharacter character, IEnumerable<PlayerCharacter> characters);

    /// <summary>
    /// Refreshes the locally stored character info from the DKP server.
    /// </summary>
    Task<bool> RefreshCharacterInfoAsync();
}
