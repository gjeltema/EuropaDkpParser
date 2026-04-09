// -----------------------------------------------------------------------
// DkpServer.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using DkpParser.Uploading;
using Gjeltema.Logging;

public sealed class DkpServer : IDkpServer
{
    private const string LogPrefix = $"[{nameof(DkpServer)}]";
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

        Log.Debug($"{LogPrefix} HttpClient initialized with default User Agent: {LocalHttpClient.DefaultRequestHeaders.UserAgent}");
    }

    static DkpServer()
    {
        LocalHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        // Leaving these here in case the DKP server ends up needing these later.
        //LocalHttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        //LocalHttpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        //LocalHttpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en");
    }

    public async Task<int> GetCharacterIdAsync(string characterName)
    {
        try
        {
            return await GetCharacterIdFromServerAsync(characterName);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error encountered retrieving character ID for {characterName}: {ex.ToLogMessage()}");
            return -1;
        }
    }

    public async Task<ICollection<PreviousRaid>> GetPriorRaidsAsync(int numbeOfRaids)
    {
        try
        {
            string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=raids&number={numbeOfRaids}";

            XDocument responseDoc = await MakeGetCallAsync(uri);

            ICollection<PreviousRaid> userCharacters = GetPriorRaidsFromResponse(responseDoc);
            return userCharacters;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(GetPriorRaidsAsync)} Error encountered in getting previous raids: {ex.ToLogMessage()}");
            return [];
        }
    }

    public async Task<ICollection<DkpUserCharacter>> GetUserCharactersAsync(int userId)
    {
        try
        {
            string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=user_chars&userid={userId}";

            XDocument responseDoc = await MakeGetCallAsync(uri);

            ICollection<DkpUserCharacter> userCharacters = GetUserCharactersFromResponse(responseDoc, userId);
            if (userCharacters.Count == 0)
                return null;

            return userCharacters;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(GetUserCharactersAsync)} Error encountered in getting users for user ID: {userId}: {ex.ToLogMessage()}");
        }

        return null;
    }

    public async Task<CharacterDkpAmounts> GetUserDkpAsync(int characterId)
    {
        try
        {
            CharacterDkpAmounts userDkp = await GetUserDkpFromCharacterIdAsync(characterId);
            return userDkp ?? new CharacterDkpAmounts { CharacterId = characterId };
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(GetUserDkpAsync)} Error encountered in retrieving DKP for user ID {characterId}: {ex.ToLogMessage()}");
        }

        return new CharacterDkpAmounts { CharacterId = characterId };
    }

    public async Task<CharacterDkpAmounts> GetUserDkpAsync(string characterName)
    {
        try
        {
            int characterId = await GetCharacterIdFromServerAsync(characterName);
            if (characterId < 0)
                return new CharacterDkpAmounts { CharacterName = characterName };

            CharacterDkpAmounts userDkp = await GetUserDkpFromCharacterIdAsync(characterId);
            return userDkp ?? new CharacterDkpAmounts { CharacterId = characterId, CharacterName = characterName };
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(GetUserDkpAsync)} Error encountered retrieving character ID or DKP for {characterName}: {ex.ToLogMessage()}");
        }
        return new CharacterDkpAmounts { CharacterName = characterName };
    }

    public async Task InitializeIdentifiersAsync(IEnumerable<string> playerNames, IEnumerable<string> zoneNames, RaidUploadResults results)
    {
        Log.Debug($"{LogPrefix} ------- Starting retrieval of IDs -------");

        await GetEventIdsAsync(zoneNames, results);

        foreach (string playerName in playerNames)
        {
            await GetCharacterIdFromServerAsync(playerName, results);
        }

        Log.Debug($"{LogPrefix} ------- Completed retrieval of IDs -------");
    }

    public async Task UploadAdjustmentAsync(AdjustmentUploadInfo adjustment)
    {
        string postBody = CraftAdjustmentString(adjustment);
        await UploadMessageAsync("add_adjustment", postBody);
    }

    public async Task UploadAttendanceAsync(AttendanceUploadInfo attendanceEntry)
    {
        string postBody = CraftAttendanceString(attendanceEntry);
        string response = await UploadMessageAsync("add_raid", postBody);

        XElement root = XDocument.Parse(response).Root;
        int raidId = (int)root.Descendants("raid_id").FirstOrDefault();

        Log.Debug($"{LogPrefix} Raid ID assigned to attendance call: {raidId}");

        _raidIdCache[attendanceEntry.CallName] = raidId;
    }

    public async Task UploadDkpSpentAsync(DkpUploadInfo dkpEntry)
    {
        string postBody = CraftDkpString(dkpEntry);
        await UploadMessageAsync("add_item", postBody);
    }

    private string CraftAdjustmentString(AdjustmentUploadInfo adjustment)
    {
        var adjustmentContent =
            new XElement("request",
                new XElement("adjustment_date", adjustment.Timestamp.ToUsTimestamp(ServerTimeFormat)),
                new XElement("adjustment_value", adjustment.DkpAmount),
                new XElement("adjustment_reason", SanitizeString(adjustment.AdjustmentReason)),
                new XElement("adjustment_raid_id", adjustment.RaidId),
                new XElement("adjustment_members",
                    new XElement("member", adjustment.CharacterId),
                    new XElement("member"))
            );

        return adjustmentContent.ToString();
    }

    private string CraftAttendanceString(AttendanceUploadInfo attendanceEntry)
    {
        IEnumerable<int> memberIds = attendanceEntry.Characters
            .Select(x => x.CharacterName)
            .Select(playerName => _playerIdCache[playerName]);

        int eventId = _eventIdCache[attendanceEntry.ZoneName];

        var attendanceContent =
            new XElement("request",
                new XElement("raid_date", attendanceEntry.Timestamp.ToUsTimestamp(ServerTimeFormat)),
                new XElement("raid_value", attendanceEntry.DkpAwarded),
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

    private int GetCharacterIdFromResponse(XDocument responseDoc)
    {
        XElement root = responseDoc.Root;
        XElement directNode = root.Descendants("direct").FirstOrDefault();
        XElement characterIdNode = directNode.Descendants("id").FirstOrDefault();
        int characterId = (int)characterIdNode;
        return characterId;
    }

    private async Task<int> GetCharacterIdFromServerAsync(string characterName)
    {
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=search&in=charname&for={characterName}";

        XDocument responseDoc = await MakeGetCallAsync(uri);

        int characterId = GetCharacterIdFromResponse(responseDoc);

        Log.Debug($"{LogPrefix} Extracted character ID for {characterName} is {characterId}");

        _playerIdCache[characterName] = characterId;

        return characterId;
    }

    private async Task GetCharacterIdFromServerAsync(string characterName, RaidUploadResults results)
    {
        try
        {
            await GetCharacterIdFromServerAsync(characterName);
        }
        catch (Exception ex)
        {
            CharacterIdFailure fail = new()
            {
                CharacterName = characterName,
                Error = ex
            };
            results.FailedCharacterIdRetrievals.Add(fail);
            Log.Error($"{LogPrefix} Error encountered retrieving character ID for {characterName}: {ex.ToLogMessage()}");
        }
    }

    private async Task GetEventIdsAsync(IEnumerable<string> zoneNames, RaidUploadResults results)
    {
        XDocument eventIdsDoc = await GetEventIdsFromServerAsync(results);
        if (results.EventIdCallFailure != null || eventIdsDoc == null)
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

    private async Task<XDocument> GetEventIdsFromServerAsync(RaidUploadResults results)
    {
        try
        {
            string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=events";

            XDocument responseDoc = await MakeGetCallAsync(uri);
            return responseDoc;
        }
        catch (Exception ex)
        {
            results.EventIdCallFailure = ex;
            Log.Error($"{LogPrefix} Error encountered in Events call: {ex.ToLogMessage()}");
        }

        return null;
    }

    private HttpContent GetPostContent(string postBody)
        => new StringContent(postBody);

    private ICollection<PreviousRaid> GetPriorRaidsFromResponse(XDocument responseDoc)
    {
        List<PreviousRaid> raids = [];
        IEnumerable<XElement> raidNodes = responseDoc.Descendants("raid");
        foreach (XElement raidNode in raidNodes)
        {
            string raidName = (string)raidNode.Element("note");

            string raidTimeRaw = (string)raidNode.Element("date");
            DateTime raidTime = DateTime.ParseExact(raidTimeRaw, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            XElement raidAttendeesElement = raidNode.Element("raid_attendees");
            ICollection<int> characterIds = raidAttendeesElement.Descendants().Select(x => (int)x).ToList();

            raids.Add(new PreviousRaid
            {
                RaidName = raidName,
                CharacterIds = characterIds,
                RaidTime = raidTime
            });
        }

        return raids;
    }

    private ICollection<DkpUserCharacter> GetUserCharactersFromResponse(XDocument response, int userId)
    {
        List<DkpUserCharacter> userChars = [];
        IEnumerable<XElement> characterNodes = response.Descendants("char");
        foreach (XElement characterNode in characterNodes)
        {
            int idValue = (int)characterNode.Element("id");
            string characterName = (string)characterNode.Element("name");
            string className = (string)characterNode.Element("classname");
            int level = (int)characterNode.Descendants("level").FirstOrDefault();

            userChars.Add(new DkpUserCharacter
            {
                UserId = userId,
                CharacterId = idValue,
                Name = characterName.NormalizeName(),
                ClassName = className,
                Level = level
            });
        }

        return userChars;
    }

    private async Task<CharacterDkpAmounts> GetUserDkpFromCharacterIdAsync(int characterId)
    {
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiReadToken}&function=points&filter=character&filterid={characterId}";
        XDocument responseDoc = await MakeGetCallAsync(uri);

        Log.Trace($"{LogPrefix} {nameof(GetUserDkpFromCharacterIdAsync)} response:{Environment.NewLine}{responseDoc}");

        CharacterDkpAmounts userDkp = GetUserDkpFromResponse(responseDoc);
        return userDkp;
    }

    private CharacterDkpAmounts GetUserDkpFromResponse(XDocument responseDoc)
    {
        XElement playerElement = responseDoc.Root.Descendants("player").FirstOrDefault();
        if (playerElement == null)
            return null;

        int characterId = (int)playerElement.Element("id");
        string characterName = (string)playerElement.Element("name");
        string characterClass = (string)playerElement.Element("class_name");
        int mainId = (int)playerElement.Element("main_id");
        string mainName = (string)playerElement.Element("main_name");

        XElement multiDkpPoints = responseDoc.Root.Descendants("multidkp_points").FirstOrDefault();
        int characterCurrentDkp = (int)multiDkpPoints.Element("points_current");
        int characterEarnedDkp = (int)multiDkpPoints.Element("points_earned");
        int characterSpentDkp = (int)multiDkpPoints.Element("points_spent");
        int characterAdjustedDkp = (int)multiDkpPoints.Element("points_adjustment");

        int userCurrentDkp = (int)multiDkpPoints.Element("points_current_with_twink");
        int userEarnedDkp = (int)multiDkpPoints.Element("points_earned_with_twink");
        int userSpentDkp = (int)multiDkpPoints.Element("points_spent_with_twink");
        int userAdjustedDkp = (int)multiDkpPoints.Element("points_adjustment_with_twink");

        return new CharacterDkpAmounts
        {
            CharacterId = characterId,
            CharacterName = characterName,
            MainCharacterId = mainId,
            MainCharacterName = mainName,
            ClassName = characterClass,
            CharacterCurrentDkp = characterCurrentDkp,
            CharacterTotalEarnedDkp = characterEarnedDkp,
            CharacterTotalSpentDkp = characterSpentDkp,
            CharacterAdjustedDkp = characterAdjustedDkp,
            UserCurrentDkp = userCurrentDkp,
            UserTotalEarnedDkp = userEarnedDkp,
            UserTotalSpentDkp = userSpentDkp,
            UserAdjustedDkp = userAdjustedDkp
        };
    }

    private async Task<XDocument> MakeGetCallAsync(string url)
    {
        Log.Debug($"{LogPrefix} ---- Making GET call with URL: {url}");

        using HttpResponseMessage response = await LocalHttpClient.GetAsync(url);

        Log.Trace($"{LogPrefix} GET response received.  Response object: {response}");

        string responseText = await response.Content.ReadAsStringAsync();

        Log.Trace($"{LogPrefix} GET response text:{Environment.NewLine}{responseText}");

        response.EnsureSuccessStatusCode();

        var doc = XDocument.Parse(responseText);
        return doc;
    }

    private string SanitizeString(string toBeSanitized)
    {
        char[] sanitizedChars = toBeSanitized.Where(x => x != '<' && x != '>').ToArray();
        return new string(sanitizedChars);
    }

    private async Task<string> UploadMessageAsync(string function, string content)
    {
        Log.Trace($"{LogPrefix} Uploading to function '{function}' with POST body:{Environment.NewLine}{content}");

        using HttpContent postContent = GetPostContent(content);
        postContent.Headers.ContentType = _mediaHeader;
        string uri = $"{_settings.ApiUrl}&atoken={_settings.ApiWriteToken}&function={function}";

        Log.Debug($"{LogPrefix} Uploading with URL: {uri}");

        using HttpResponseMessage response = await LocalHttpClient.PostAsync(uri, postContent);

        Log.Trace($"{LogPrefix} Received response.  Response object:{Environment.NewLine}{response}");

        string text = await response.Content.ReadAsStringAsync();

        Log.Trace($"{LogPrefix} Response:{Environment.NewLine}{text}");

        response.EnsureSuccessStatusCode();

        return text;
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class DkpUserCharacter
{
    public int CharacterId { get; init; }

    public string ClassName { get; init; }

    public int Level { get; init; }

    public string Name { get; init; }

    public int UserId { get; init; }

    private string DebugText
        => $"{UserId,-4} {Name} {Level} {ClassName}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class CharacterDkpAmounts
{
    public int CharacterAdjustedDkp { get; init; } = int.MinValue;

    public int CharacterCurrentDkp { get; init; } = int.MinValue;

    public int CharacterId { get; init; } = -1;

    public string CharacterName { get; init; } = string.Empty;

    public int CharacterTotalEarnedDkp { get; init; } = int.MinValue;

    public int CharacterTotalSpentDkp { get; init; } = int.MinValue;

    public string ClassName { get; init; }

    public int MainCharacterId { get; init; } = int.MinValue;

    public string MainCharacterName { get; init; } = string.Empty;

    public int UserAdjustedDkp { get; init; } = int.MinValue;

    public int UserCurrentDkp { get; init; } = int.MinValue;

    public int UserTotalEarnedDkp { get; init; } = int.MinValue;

    public int UserTotalSpentDkp { get; init; } = int.MinValue;

    private string DebugText
       => $"{CharacterName} ID:{CharacterId} {UserCurrentDkp} DKP";
}

public interface IDkpServer
{
    Task<int> GetCharacterIdAsync(string characterName);

    Task<ICollection<PreviousRaid>> GetPriorRaidsAsync(int numbeOfRaids);

    Task<ICollection<DkpUserCharacter>> GetUserCharactersAsync(int userId);

    Task<CharacterDkpAmounts> GetUserDkpAsync(int userId);

    Task<CharacterDkpAmounts> GetUserDkpAsync(string characterName);

    Task InitializeIdentifiersAsync(IEnumerable<string> playerNames, IEnumerable<string> zoneNames, RaidUploadResults results);

    Task UploadAdjustmentAsync(AdjustmentUploadInfo adjustment);

    Task UploadAttendanceAsync(AttendanceUploadInfo attendanceEntry);

    Task UploadDkpSpentAsync(DkpUploadInfo dkpEntry);
}
