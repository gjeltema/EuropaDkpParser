// -----------------------------------------------------------------------
// DkpDataRetriever.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpDataRetriever : IDkpDataRetriever
{
    private const int EmptyResponsesThreshold = 20;
    private readonly IDkpServer _server;

    public DkpDataRetriever(IDkpParserSettings settings)
    {
        _server = new DkpServer(settings);
    }

    public async Task<ICollection<DkpUserCharacter>> GetUserCharactersAsync()
    {
        int userId = 1;
        int numberOfEmptyResponses = 0;
        List<DkpUserCharacter> allUserCharacters = new(1200);
        while (numberOfEmptyResponses < EmptyResponsesThreshold)
        {
            userId++;
            ICollection<DkpUserCharacter> userCharacters = await _server.GetUserCharactersAsync(userId);
            if (userCharacters == null)
            {
                numberOfEmptyResponses++;
            }
            else
            {
                allUserCharacters.AddRange(userCharacters);
                numberOfEmptyResponses = 0;
            }
        }

        return allUserCharacters;
    }

    public async Task<int> GetUserDkpAsync(string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return int.MinValue;

        return await _server.GetUserDkpAsync(characterName);
    }

    public async Task<int> GetUserDkpAsync(DkpUserCharacter userCharacter)
    {
        if (userCharacter == null || userCharacter.UserId < 2)
            return int.MinValue;

        return await _server.GetUserDkpAsync(userCharacter.UserId);
    }
}

public interface IDkpDataRetriever
{
    Task<ICollection<DkpUserCharacter>> GetUserCharactersAsync();

    /// <summary>
    /// Gets the DKP amount for the character name from the DKP server.</br>
    /// If unable to retrieve the DKP amount, returns <see cref="int.MinValue"/>.
    /// </summary>
    Task<int> GetUserDkpAsync(string characterName);

    /// <summary>
    /// Gets the DKP amount for the user account from the DKP server.</br>
    /// If unable to retrieve the DKP amount, returns <see cref="int.MinValue"/>.
    /// </summary>
    Task<int> GetUserDkpAsync(DkpUserCharacter userCharacter);
}
