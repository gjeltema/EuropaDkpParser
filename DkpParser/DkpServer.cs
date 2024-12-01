// -----------------------------------------------------------------------
// DkpServer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using DkpParser.Uploading;

public sealed class DkpServer : IDkpServer
{
    private const string ServerTimeFormat = "yyyy-MM-dd HH:mm";
    private static readonly HttpClient LocalHttpClient = new();
    private readonly IUploadDebugInfo _debugInfo;
    private readonly Dictionary<string, int> _eventIdCache = [];
    private readonly MediaTypeHeaderValue _mediaHeader = new("application/xml");
    private readonly Dictionary<string, int> _playerIdCache = [];
    private readonly Dictionary<string, int> _raidIdCache = [];
    private readonly IDkpParserSettings _settings;

    public DkpServer(IDkpParserSettings settings, IUploadDebugInfo debugInfo)
    {
        _settings = settings;
        _debugInfo = debugInfo;

        _debugInfo.AddDebugMessage($"HttpClient initialized with default User Agent: {LocalHttpClient.DefaultRequestHeaders.UserAgent}");
    }

    static DkpServer()
    {
        LocalHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        // Leaving these here in case the DKP server ends up needing these later.
        //LocalHttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        //LocalHttpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        //LocalHttpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en");
    }

    public async Task InitializeIdentifiers(IEnumerable<string> playerNames, IEnumerable<string> zoneNames, RaidUploadResults results)
    {
        _debugInfo.AddDebugMessage("------- Starting retrieval of IDs -------");

        await GetEventIds(zoneNames, results);

        foreach (string playerName in playerNames)
        {
            await GetCharacterIdFromServer(playerName, results);
        }

        _debugInfo.AddDebugMessage("------- Completed retrieval of IDs -------");
    }

    public async Task UploadAttendance(AttendanceUploadInfo attendanceEntry)
    {
        string postBody = CraftAttendanceString(attendanceEntry);
        string response = await UploadMessage("add_raid", postBody);

        XElement root = XDocument.Parse(response).Root;
        int raidId = (int)root.Descendants("raid_id").FirstOrDefault();

        _debugInfo.AddDebugMessage($"Raid ID assigned to attendance call: {raidId}");

        _raidIdCache[attendanceEntry.CallName] = raidId;
    }

    public async Task UploadDkpSpent(DkpUploadInfo dkpEntry)
    {
        string postBody = CraftDkpString(dkpEntry);
        await UploadMessage("add_item", postBody);
    }

    private string CraftAttendanceString(AttendanceUploadInfo attendanceEntry)
    {
        IEnumerable<int> memberIds = attendanceEntry.Characters
            .Select(x => x.CharacterName)
            .Select(playerName => _playerIdCache[playerName]);

        int eventId = _eventIdCache[attendanceEntry.ZoneName];
        int dkpValue = _settings.RaidValue.GetDkpValueForRaid(attendanceEntry);

        var attendanceContent =
            new XElement("request",
                new XElement("raid_date", attendanceEntry.Timestamp.ToUsTimestamp(ServerTimeFormat)),
                new XElement("raid_value", dkpValue),
                new XElement("raid_event_id", eventId),
                new XElement("raid_note", SanitizeString(attendanceEntry.ToDkpServerDescription())),
                new XElement("raid_attendees",
                    memberIds.Select(x => new XElement("member", x))
                )
            );

        return attendanceContent.ToString();
    }

    private string CraftDkpString(DkpUploadInfo dkpEntry)
    {
        int characterId = _playerIdCache[dkpEntry.CharacterName];
        AttendanceEntry associatedEntry = dkpEntry.AssociatedAttendanceCall;
        int raidId = _raidIdCache.Last().Value;
        if (associatedEntry != null && !string.IsNullOrEmpty(associatedEntry.CallName))
            raidId = _raidIdCache[associatedEntry.CallName];

        var dkpContent =
            new XElement("request",
                new XElement("item_date", dkpEntry.Timestamp.ToUsTimestamp(ServerTimeFormat)),
                new XElement("item_value", dkpEntry.DkpSpent),
                new XElement("item_name", SanitizeString(dkpEntry.Item)),
                new XElement("item_raid_id", raidId),
                new XElement("item_itempool_id", 1),
                new XElement("item_buyers",
                    new XElement("member", characterId),
                    new XElement("member"))
            );

        return dkpContent.ToString();
    }

    private int GetCharacterIdFromResponse(string response)
    {
        XElement root = XDocument.Parse(response).Root;
        XElement directNode = root.Descendants("direct").FirstOrDefault();
        XElement characterIdNode = directNode.Descendants("id").FirstOrDefault();
        int characterId = (int)characterIdNode;
        return characterId;
    }

    private async Task<int> GetCharacterIdFromServer(string characterName)
    {
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=search&in=charname&for={characterName}";

        _debugInfo.AddDebugMessage($"---- Calling search function for character name {characterName} with URL: {uri}");

        using HttpResponseMessage response = await LocalHttpClient.GetAsync(uri);

        _debugInfo.AddDebugMessage($"Search response received.  Response object:{Environment.NewLine}{response}");

        string responseText = await response.Content.ReadAsStringAsync();

        _debugInfo.AddDebugMessage($"Character ID response text:{Environment.NewLine}{responseText}");

        response.EnsureSuccessStatusCode();

        int characterId = GetCharacterIdFromResponse(responseText);

        _debugInfo.AddDebugMessage($"Extracted character ID for {characterName} is {characterId}");

        _playerIdCache[characterName] = characterId;

        return characterId;
    }

    private async Task GetCharacterIdFromServer(string playerName, RaidUploadResults results)
    {
        try
        {
            await GetCharacterIdFromServer(playerName);
        }
        catch (Exception ex)
        {
            CharacterIdFailure fail = new()
            {
                CharacterName = playerName,
                Error = ex
            };
            results.FailedCharacterIdRetrievals.Add(fail);
            _debugInfo.AddDebugMessage($"Error encountered retrieving character ID for {playerName}: {ex}");
        }
    }

    private async Task GetEventIds(IEnumerable<string> zoneNames, RaidUploadResults results)
    {
        XDocument eventIdsDoc = await GetEventIdsFromServer(results);
        if (results.EventIdCallFailure != null)
            return;

        foreach (string zoneName in zoneNames)
        {
            string aliasedZoneName = _settings.RaidValue.GetZoneRaidAlias(zoneName);
            if (string.IsNullOrEmpty(aliasedZoneName))
            {
                results.EventIdNotFoundErrors.Add(new EventIdNotFoundFailure { ErrorType = EventIdNotFoundFailure.EventIdError.ZoneNotConfigured, ZoneName = zoneName });
                continue;
            }

            XElement idValue = eventIdsDoc.Root.Elements()
                .Where(x => x.Elements().Any(x => x.Name == "name" && x.Value == aliasedZoneName))
                .Elements().FirstOrDefault(x => x.Name == "id");

            if (idValue == null)
            {
                results.EventIdNotFoundErrors.Add(
                    new EventIdNotFoundFailure { ErrorType = EventIdNotFoundFailure.EventIdError.ZoneNotFoundOnDkpServer, ZoneName = zoneName, ZoneAlias = aliasedZoneName }
                    );
                continue;
            }

            if (!int.TryParse(idValue.Value, out int eventId))
            {
                results.EventIdNotFoundErrors.Add(
                    new EventIdNotFoundFailure
                    {
                        ErrorType = EventIdNotFoundFailure.EventIdError.ZoneNotFoundOnDkpServer,
                        ZoneName = zoneName,
                        ZoneAlias = aliasedZoneName,
                        IdValue = idValue.Value
                    }
                    );
                continue;
            }

            _eventIdCache[zoneName] = eventId;
        }
    }

    private async Task<XDocument> GetEventIdsFromServer(RaidUploadResults results)
    {
        try
        {
            string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=events";

            _debugInfo.AddDebugMessage($"---- Calling Events API function with URL: {uri}");

            using HttpResponseMessage response = await LocalHttpClient.GetAsync(uri);

            _debugInfo.AddDebugMessage($"Events response received.  Response object: {response}");

            string responseText = await response.Content.ReadAsStringAsync();

            _debugInfo.AddDebugMessage($"Events response text:{Environment.NewLine}{responseText}");

            response.EnsureSuccessStatusCode();

            var doc = XDocument.Parse(responseText);
            return doc;
        }
        catch (Exception ex)
        {
            results.EventIdCallFailure = ex;
            _debugInfo.AddDebugMessage($"Error encountered in Events call: {ex}");
        }

        return null;
    }

    private HttpContent GetPostContent(string postBody)
        => new StringContent(postBody);

    private string SanitizeString(string toBeSanitized)
    {
        char[] sanitizedChars = toBeSanitized.Where(x => x != '<' && x != '>').ToArray();
        return new string(sanitizedChars);
    }

    private async Task<string> UploadMessage(string function, string content)
    {
        _debugInfo.AddDebugMessage($"Uploading with POST body:{Environment.NewLine}{content}");

        using HttpContent postContent = GetPostContent(content);
        postContent.Headers.ContentType = _mediaHeader;
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiWriteToken}&function={function}";

        _debugInfo.AddDebugMessage($"Uploading with URL: {uri}");

        using HttpResponseMessage response = await LocalHttpClient.PostAsync(uri, postContent);

        _debugInfo.AddDebugMessage($"Received response.  Response object:{Environment.NewLine}{response}");

        string text = await response.Content.ReadAsStringAsync();

        _debugInfo.AddDebugMessage($"Response:{Environment.NewLine}{text}");

        response.EnsureSuccessStatusCode();

        return text;
    }
}

public interface IDkpServer
{
    Task InitializeIdentifiers(IEnumerable<string> playerNames, IEnumerable<string> zoneNames, RaidUploadResults results);

    Task UploadAttendance(AttendanceUploadInfo attendanceEntry);

    Task UploadDkpSpent(DkpUploadInfo dkpEntry);
}
