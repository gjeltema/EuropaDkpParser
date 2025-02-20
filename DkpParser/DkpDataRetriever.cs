// -----------------------------------------------------------------------
// DkpDataRetriever.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpDataRetriever : IDkpDataRetriever
{
    private const int EmptyResponsesThreshold = 30;
    private readonly IDkpServer _server;

    public DkpDataRetriever(IDkpParserSettings settings)
    {
        _server = new DkpServer(settings);
    }

    public async Task<ICollection<DkpUserCharacter>> GetUserCharacters()
    {
        int userId = 1;
        int numberOfEmptyResponses = 0;
        List<DkpUserCharacter> allUserCharacters = new(1200);
        while (numberOfEmptyResponses < EmptyResponsesThreshold)
        {
            userId++;
            ICollection<DkpUserCharacter> userCharacters = await _server.GetUserCharacters(userId);
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

    public async Task<int> GetUserDkp(DkpUserCharacter userCharacter)
    {
        if (userCharacter == null || userCharacter.UserId < 2)
            return int.MinValue;

        return await _server.GetUserDkp(userCharacter.UserId);
    }
}

public interface IDkpDataRetriever
{
    Task<ICollection<DkpUserCharacter>> GetUserCharacters();

    /// <summary>
    /// Gets the DKP amount for the user account from the DKP server.</br>
    /// If unable to retrieve the DKP amount, returns <see cref="int.MinValue"/>.
    /// </summary>
    Task<int> GetUserDkp(DkpUserCharacter userCharacter);
}
