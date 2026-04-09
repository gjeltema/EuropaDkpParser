// -----------------------------------------------------------------------
// RaidAttendanceCalculator.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using Gjeltema.Logging;

public sealed class RaidAttendanceCalculator : IRaidAttendance
{
    private const string LogPrefix = $"[{nameof(RaidAttendanceCalculator)}]";
    private const int NumberOfRaids = 250;
    public static readonly RaidAttendanceCalculator Instance = new();
    private static readonly Dictionary<string, RaidAttendanceInfo> _raidAttendances = [];
    private static IDkpServer _dkpServer;
    private static ICollection<PreviousRaid> _previousRaids = [];
    private static IDkpParserSettings _settings;

    private RaidAttendanceCalculator() { }

    public static async Task InitializeAsync(IDkpServer dkpServer, IDkpParserSettings settings)
    {
        if (_previousRaids.Count > 0)
            return;

        _dkpServer = dkpServer;
        _settings = settings;

        DateTime thirtyDaysAgo = DateTime.Now.AddDays(-30);

        ICollection<PreviousRaid> priorRaids = await _dkpServer.GetPriorRaidsAsync(NumberOfRaids);
        _previousRaids = priorRaids.Where(x => x.RaidTime > thirtyDaysAgo).OrderBy(x => x.RaidTime).ToList();

        Log.Trace($"{LogPrefix} Last 30 days of raids:{Environment.NewLine}{string.Join(Environment.NewLine, _previousRaids.Select(x => x.ToString()))}");

        foreach (DkpUserCharacter eqCharacter in _settings.CharactersOnDkpServer.AllUserCharacters)
        {
            double raidAtt = CalculateRaidAttendance(eqCharacter.CharacterId);
            RaidAttendanceInfo raidAttInfo = CreateRaidAttendanceInfo(eqCharacter, raidAtt);
            _raidAttendances[eqCharacter.Name] = raidAttInfo;
        }
    }

    public async Task<RaidAttendanceInfo> Get30DayRaidAttendanceAsync(string characterName, int characterId = -1)
    {
        string normalizedCharacterName = characterName.NormalizeName();

        if (_raidAttendances.TryGetValue(normalizedCharacterName, out RaidAttendanceInfo ra))
            return ra;

        if (characterId < 1)
        {
            characterId = await GetCharacterIdAsync(normalizedCharacterName);
            Log.Debug($"{LogPrefix} CharacterID for {normalizedCharacterName} obtained: {characterId}.");
            if (characterId < 1)
                return CreateRaidAttendanceInfo(normalizedCharacterName, characterId);
        }

        double raidAtt = CalculateRaidAttendance(characterId);
        RaidAttendanceInfo raidAttInfo = CreateRaidAttendanceInfo(normalizedCharacterName, characterId, 0, raidAtt);
        _raidAttendances[normalizedCharacterName] = raidAttInfo;
        Log.Debug($"{LogPrefix} Calculated RA for ID {characterId}: {raidAtt}.");

        return raidAttInfo;
    }

    public IEnumerable<RaidAttendanceInfo> GetAll30DayRaidAttendances()
        => _raidAttendances.Values;

    private static double CalculateRaidAttendance(int characterId)
    {
        int numberOfRaidsAttended = _previousRaids.Count(x => x.CharacterIds.Contains(characterId));
        Log.Debug($"{LogPrefix} Character raids/Total raids: {numberOfRaidsAttended}/{_previousRaids.Count}.");
        if (numberOfRaidsAttended == 0)
            return 0;

        double raidAtt = numberOfRaidsAttended * 100.0 / _previousRaids.Count;
        return raidAtt;
    }

    private static RaidAttendanceInfo CreateRaidAttendanceInfo(DkpUserCharacter eqCharacter, double thirtyDayRA)
        => CreateRaidAttendanceInfo(eqCharacter.Name, eqCharacter.CharacterId, eqCharacter.UserId, thirtyDayRA);

    private static RaidAttendanceInfo CreateRaidAttendanceInfo(string characterName, int characterId, int userId = 0, double thirtyDayRA = 0)
        => new()
        {
            CharacterId = characterId,
            CharacterName = characterName,
            ThirtyDayRA = thirtyDayRA,
            UserId = userId
        };

    private async Task<int> GetCharacterIdAsync(string characterName)
    {
        DkpUserCharacter character = _settings.CharactersOnDkpServer.AllUserCharacters.FirstOrDefault(x => x.Name == characterName);
        if (character != null)
            return character.CharacterId;

        int characterId = await _dkpServer.GetCharacterIdAsync(characterName);
        return characterId;
    }
}

public interface IRaidAttendance
{
    Task<RaidAttendanceInfo> Get30DayRaidAttendanceAsync(string characterName, int characterid = -1);

    IEnumerable<RaidAttendanceInfo> GetAll30DayRaidAttendances();
}

public sealed class RaidAttendanceInfo
{
    public int CharacterId { get; init; }

    public string CharacterName { get; init; }

    public double ThirtyDayRA { get; init; }

    public int UserId { get; init; }
}
