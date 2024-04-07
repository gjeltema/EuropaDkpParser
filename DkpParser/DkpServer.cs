// -----------------------------------------------------------------------
// DkpServer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Net;
using System.Net.Http;
using System.Xml.Linq;

public sealed class DkpServer : IDkpServer
{
    private const string AddRaidFunction = "add_raid";
    private const string AddSpendFunction = "add_item";
    private const string EventsFunction = "events";
    private static readonly HttpClient _httpClient = new();
    private readonly Dictionary<string, int> _playerIdCache = new();
    private readonly IDkpParserSettings _settings;

    public DkpServer(IDkpParserSettings settings)
    {
        _settings = settings;
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

    private async Task<int> GetCharacterId(string characterName)
    {
        if (_playerIdCache.TryGetValue(characterName, out int characterId))
        {
            return characterId;
        }

        //using HttpRequestMessage request = new() { Method = HttpMethod.Get };

        //request.Headers.Add(TokenHeader, _settings.ApiReadToken);
        //request.Headers.Add(Function, SearchFunction);
        //request.Headers.Add(InHeader, CharnameHeaderValue);
        //request.Headers.Add(ForHeader, characterName);

        //using HttpResponseMessage response = await _httpClient.SendAsync(request);

        //string uri = _settings.ApiUrl + "&atoken=" + _settings.ApiReadToken + "&function=search&in=charname&for=" + characterName;
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=search&in=charname&for={characterName}";
        using HttpResponseMessage response = await _httpClient.GetAsync(uri);

        response.EnsureSuccessStatusCode();

        string responseText = await response.Content.ReadAsStringAsync();

        XDocument doc = XDocument.Parse(responseText);
        XElement userIdElement = doc.Descendants("id").FirstOrDefault();
        string userIdText = userIdElement.Value;
        characterId = int.Parse(userIdText);

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

public interface IDkpServer
{
    Task InitializeCharacterIds(IEnumerable<PlayerCharacter> players, IEnumerable<DkpEntry> dkpCalls, RaidUploadResults results);

    Task<DkpServerMessageResult> UploadAttendance(AttendanceEntry attendanceEntry);

    Task<DkpServerMessageResult> UploadDkpSpent(DkpEntry dkpEntry);
}
