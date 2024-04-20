// -----------------------------------------------------------------------
// DkpServer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;

public sealed class DkpServer : IDkpServer
{
    private const string ServerTimeFormat = "yyyy-MM-dd HH:mm";
    private static readonly HttpClient LocalHttpClient = new();
    private readonly Dictionary<string, int> _eventIdCache = [];
    private readonly MediaTypeHeaderValue _mediaHeader = new("application/xml");
    private readonly Dictionary<string, int> _playerIdCache = [];
    private readonly Dictionary<string, int> _raidIdCache = [];
    private readonly IDkpParserSettings _settings;

    public DkpServer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public async Task InitializeIdentifiers(IEnumerable<string> playerNames, IEnumerable<string> zoneNames, RaidUploadResults results)
    {
        foreach (string playerName in playerNames)
        {
            await GetCharacterIdFromServer(playerName, results);
        }

        await GetEventIds(zoneNames, results);
    }

    public async Task UploadAttendance(AttendanceEntry attendanceEntry)
    {
        string postBody = CraftAttendanceString(attendanceEntry);
        string response = await UploadMessage("add_raid", postBody);

        XElement root = XDocument.Parse(response).Root;
        int raidId = (int)root.Descendants("raid_id").FirstOrDefault();

        _raidIdCache[attendanceEntry.RaidName] = raidId;
    }

    public async Task UploadDkpSpent(DkpEntry dkpEntry)
    {
        string postBody = CraftDkpString(dkpEntry);
        await UploadMessage("add_item", postBody);
    }

    private string CraftAttendanceString(AttendanceEntry attendanceEntry)
    {
        IEnumerable<int> memberIds = attendanceEntry.Players
            .Select(x => x.PlayerName)
            .Select(playerName => _playerIdCache[playerName]);

        int eventId = _eventIdCache[attendanceEntry.ZoneName];
        int dkpValue = attendanceEntry.AttendanceCallType == AttendanceCallType.Time
            ? _settings.RaidValue.GetTimeBasedValue(attendanceEntry.ZoneName)
            : _settings.RaidValue.GetBossKillValue(attendanceEntry.RaidName);

        var attendanceContent =
            new XElement("request",
                new XElement("raid_date", attendanceEntry.Timestamp.ToUsTimestamp(ServerTimeFormat)),
                new XElement("raid_value", dkpValue),
                new XElement("raid_event_id", eventId),
                new XElement("raid_note", attendanceEntry.ToDkpServerDescription()),
                new XElement("raid_attendees",
                    memberIds.Select(x => new XElement("member", x))
                )
            );

        return attendanceContent.ToString();
    }

    private string CraftDkpString(DkpEntry dkpEntry)
    {
        int characterId = _playerIdCache[dkpEntry.PlayerName];
        AttendanceEntry associatedEntry = dkpEntry.AssociatedAttendanceCall;
        int raidId = _raidIdCache.Last().Value;
        if (associatedEntry != null && !string.IsNullOrEmpty(associatedEntry.RaidName))
            raidId = _raidIdCache[dkpEntry.AssociatedAttendanceCall.RaidName];

        var dkpContent =
            new XElement("request",
                new XElement("item_date", dkpEntry.Timestamp.ToUsTimestamp(ServerTimeFormat)),
                new XElement("item_value", dkpEntry.DkpSpent),
                new XElement("item_name", dkpEntry.Item),
                new XElement("item_raid_id", raidId),
                new XElement("item_itempool_id", 1),
                new XElement("item_buyers",
                    new XElement("member", characterId),
                    new XElement("member"))
            );

        return dkpContent.ToString();
    }

    private async Task<int> GetCharacterIdFromServer(string characterName)
    {
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=search&in=charname&for={characterName}";
        using HttpResponseMessage response = await LocalHttpClient.GetAsync(uri);

        response.EnsureSuccessStatusCode();

        string responseText = await response.Content.ReadAsStringAsync();

        XElement root = XDocument.Parse(responseText).Root;
        int characterId = (int)root.Descendants("id").FirstOrDefault();

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
                PlayerName = playerName,
                Error = ex
            };
            results.FailedCharacterIdRetrievals.Add(fail);
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

            XElement idValue = eventIdsDoc.Root.Elements()
                .Where(x => x.Elements().Any(x => x.Name == "name" && x.Value == aliasedZoneName))
                .Elements().FirstOrDefault(x => x.Name == "id");

            if (idValue == null)
            {
                if (zoneName == aliasedZoneName)
                    results.EventIdNotFoundErrors.Add($"{zoneName}");
                else
                    results.EventIdNotFoundErrors.Add($"{zoneName} with alias {aliasedZoneName}");
            }

            int eventId = (int)idValue;

            _eventIdCache[zoneName] = eventId;
        }
    }

    private async Task<XDocument> GetEventIdsFromServer(RaidUploadResults results)
    {
        try
        {
            string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=events";
            using HttpResponseMessage response = await LocalHttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();

            string responseText = await response.Content.ReadAsStringAsync();

            XDocument doc = XDocument.Parse(responseText);
            return doc;
        }
        catch (Exception ex)
        {
            results.EventIdCallFailure = ex;
        }

        return null;
    }

    private HttpContent GetPostContent(string postBody)
        => new StringContent(postBody);

    private async Task<string> UploadMessage(string function, string content)
    {
        using HttpContent postContent = GetPostContent(content);
        postContent.Headers.ContentType = _mediaHeader;
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiWriteToken}&function={function}";
        using HttpResponseMessage response = await LocalHttpClient.PostAsync(uri, postContent);

        string text = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}

public interface IDkpServer
{
    Task InitializeIdentifiers(IEnumerable<string> playerNames, IEnumerable<string> zoneNames, RaidUploadResults results);

    Task UploadAttendance(AttendanceEntry attendanceEntry);

    Task UploadDkpSpent(DkpEntry dkpEntry);
}
