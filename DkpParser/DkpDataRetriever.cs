// -----------------------------------------------------------------------
// DkpDataRetriever.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpDataRetriever : IDkpDataRetriever
{
    private const int EmptyResponsesThreshold = 30;
    private readonly IDkpServer _server;

    public DkpDataRetriever(IDkpParserSettings settings)
    {
        _server = new DkpServer(settings, new NullServerCommDebugInfo());
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
}

public interface IDkpDataRetriever
{
    Task<ICollection<DkpUserCharacter>> GetUserCharacters();
}
