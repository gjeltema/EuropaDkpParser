// -----------------------------------------------------------------------
// DkpServer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Net;
using System.Net.Http;

public sealed class DkpServer : IDkpServer
{
    private const string AddRaidFunction = "function=add_raid";
    private const string AddSpendFunction = "function=add_item";
    private const string CharacterIdFunctionPrefix = "function=search&in=charname&for="; // Add character's name to the end
    private const string EventsFunction = "function=events";
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, int> _playerIdCache = new();
    private readonly DkpParserSettings _settings;

    public DkpServer(DkpParserSettings settings)
    {
        _settings = settings;
        _httpClient = new()
        {
            BaseAddress = new Uri(_settings.ApiUrl),
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task InitializeCharacterIds(IEnumerable<PlayerCharacter> players, IEnumerable<DkpEntry> dkpCalls, RaidUploadResults results)
    {
        foreach (PlayerCharacter player in players)
        {
            await GetCharacterId(player.PlayerName, results);
        }

        foreach (DkpEntry dkpEntry in dkpCalls)
        {
            await GetCharacterId(dkpEntry.PlayerName, results);
        }
    }

    public async Task<DkpServerMessageResult> UploadAttendance(AttendanceEntry attendanceEntry)
    {
        DkpServerMessageResult result = new();

        string postBody;

        try
        {
            postBody = CraftAttendanceString(attendanceEntry);
        }
        catch (Exception ex)
        {
            //** Set failure
            return result;
        }

        using HttpContent postContent = GetPostContent(postBody);
        using HttpResponseMessage response = await _httpClient.PostAsync(AddRaidFunction, postContent);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            //** Set success
        }
        else
        {
            //** set failure
        }

        return result;
    }

    public async Task<DkpServerMessageResult> UploadDkpSpent(DkpEntry dkpEntry)
    {
        DkpServerMessageResult result = new();

        string postBody;

        try
        {
            postBody = await CraftDkpString(dkpEntry);
        }
        catch (Exception ex)
        {
            //** Set failure
            return result;
        }

        using HttpContent postContent = GetPostContent(postBody);
        //postContent.Headers.Add();
        using HttpResponseMessage response = await _httpClient.PostAsync(AddSpendFunction, postContent);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            //** Set success
        }
        else
        {
            //** set failure
        }

        return result;
    }

    private string CraftAttendanceString(AttendanceEntry attendanceEntry)
    {

        return null;
    }

    private async Task<string> CraftDkpString(DkpEntry dkpEntry)
    {
        int characterId = await GetCharacterId(dkpEntry.PlayerName);

        return null;
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<int> GetCharacterId(string characterName)
    {
        if (_playerIdCache.TryGetValue(characterName, out int characterId))
        {
            return characterId;
        }

        string header = CharacterIdFunctionPrefix + characterName;
        using HttpResponseMessage response = await _httpClient.GetAsync(header);

        response.EnsureSuccessStatusCode();

        //** Parse out response
        characterId = 0;

        _playerIdCache.Add(characterName, characterId);

        return characterId;
    }

    private async Task GetCharacterId(string playerName, RaidUploadResults results)
    {
        try
        {
            await GetCharacterId(playerName);
        }
        catch (Exception ex)
        {
            CharacterIdFailure fail = new()
            {
                PlayerName = playerName,
                Error = ex
            };
            results.FailedCharacterIdRetrievals.Add(fail);
        }
    }

    private HttpContent GetPostContent(string postBody)
        => new StringContent(postBody);
}

public sealed class DkpServerMessageResult
{

}

public interface IDkpServer : IDisposable
{
    Task InitializeCharacterIds(IEnumerable<PlayerCharacter> players, IEnumerable<DkpEntry> dkpCalls, RaidUploadResults results);

    Task<DkpServerMessageResult> UploadAttendance(AttendanceEntry attendanceEntry);

    Task<DkpServerMessageResult> UploadDkpSpent(DkpEntry dkpEntry);
}
