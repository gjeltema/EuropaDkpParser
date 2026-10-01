// -----------------------------------------------------------------------
// MusterDkpServer.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gjeltema.Logging;

public sealed class MusterDkpServer : IMusterDkpServer
{
    private const string LogPrefix = $"[{nameof(MusterDkpServer)}]";
    private static readonly HttpClient LocalHttpClient = new();
    private static readonly JsonSerializerOptions PrettyPrintJsonOption = new() { WriteIndented = true };
    private readonly MediaTypeHeaderValue _mediaHeader = new("application/json");
    private readonly IDkpParserSettings _settings;
    private static bool _authorizationSet = false;

    public MusterDkpServer(IDkpParserSettings settings)
    {
        _settings = settings;

        if (!_authorizationSet)
        {
            LocalHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiMusterToken}");
            _authorizationSet = true;
        }

        Log.Debug($"{LogPrefix} HttpClient initialized with default User Agent: {LocalHttpClient.DefaultRequestHeaders.UserAgent}");
    }

    static MusterDkpServer()
    {
        LocalHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public async Task<ICollection<CharacterRaidAttendance>> GetAllActiveCharacterAttendancesAsync()
    {
        try
        {
            ServerResponse response = await MakeGetCallAsync("v1/parser/characters");

            ICollection<CharacterRaidAttendance> raidAttendances = GetRaidAttendancesFromResponse(response);
            return raidAttendances;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(GetAllActiveCharacterAttendancesAsync)} Error encountered in getting raid attendances: {ex.ToLogMessage()}");
            return [];
        }
    }

    public async Task<ICollection<CharacterRaidAttendance>> GetAllCharactersBaseInfoAsync()
    {
        try
        {
            ServerResponse response = await MakeGetCallAsync("v1/parser/allactivecharacters");

            ICollection<CharacterRaidAttendance> baseCharacterInfo = GetBaseCharInfoFromResponse(response);
            return baseCharacterInfo;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(GetAllCharactersBaseInfoAsync)} Error encountered in getting raid attendances: {ex.ToLogMessage()}");
            return [];
        }
    }

    public async Task<CharacterRaidAttendance> GetCharacterAttendanceAsync(string characterName)
    {
        try
        {
            ServerResponse response = await MakeGetCallAsync($"v1/parser/characters/{characterName}");

            CharacterRaidAttendance raidAttendance = GetSingleRaidAttendanceFromResponse(response);
            return raidAttendance;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(GetCharacterAttendanceAsync)} Error encountered in getting raid attendance for '{characterName}': {ex.ToLogMessage()}");
            return null;
        }
    }

    public async Task<MusterDkpRaidUploadResults> UploadRaidAsync(UploadRaidInfo uploadRaidInfo)
    {
        Log.Debug($"{LogPrefix} =========== Beginning Upload Process ===========");

        MusterRaidUpload raidData = CreateRaidUploadData(uploadRaidInfo);
        string postBody = ConvertToJson(raidData);
        ServerResponse response = await UploadMessageAsync("v1/parser/raids", postBody);

        MusterDkpRaidUploadResults results = ProcessUploadRaidResponse(response, raidData, uploadRaidInfo);

        Log.Debug($"{LogPrefix} =========== Completed Upload Process ===========");

        return results;
    }

    private string ConvertToJson(MusterRaidUpload raidData)
    {
        Log.Debug($"{LogPrefix} Raid Upload Data:{Environment.NewLine}{JsonSerializer.Serialize(raidData, PrettyPrintJsonOption)}");

        return JsonSerializer.Serialize(raidData);
    }

    private MusterRaidUpload CreateRaidUploadData(UploadRaidInfo uploadRaidInfo)
    {
        List<MusterTimeTick> timeTicks = uploadRaidInfo.AttendanceInfo.Select(x => new MusterTimeTick
        {
            Timestamp = x.Timestamp.ToUniversalTime().ToString("O"),
            Points = x.DkpAwarded,
            TickName = x.CallName,
            Zone = x.ZoneName,
            Attendees = x.Characters.Select(x => x.CharacterName).ToList()
        }).ToList();

        List<MusterItemBought> buys = uploadRaidInfo.DkpInfo.Select(x => new MusterItemBought
        {
            Timestamp = x.Timestamp.ToUniversalTime().ToString("O"),
            ItemName = x.Item,
            CharacterBuying = x.CharacterName,
            DkpSpent = x.DkpSpent,
        }).ToList();

        MusterRaidUpload raid = new()
        {
            RaidName = GetRaidName(uploadRaidInfo),
            IsDryRun = uploadRaidInfo.IsTestUpload,
            TimeTicks = timeTicks,
            Items = buys
        };

        return raid;
    }

    private ICollection<CharacterRaidAttendance> GetBaseCharInfoFromResponse(ServerResponse response)
    {
        if (response.ResponseCode != HttpStatusCode.OK)
        {
            Log.Error($"{LogPrefix} Error in response: {response.ResponseCode}: Text:{response.Response}");

            return [];
        }

        AllMusterCharacters allChars = JsonSerializer.Deserialize<AllMusterCharacters>(response.Response);
        Log.Trace($"{LogPrefix} Attendance response: {JsonSerializer.Serialize(allChars, PrettyPrintJsonOption)}");

        List<CharacterRaidAttendance> allAttendances = allChars.Characters.Select(x => new CharacterRaidAttendance
        {
            CharacterId = x.CharacterId,
            CharacterName = x.CharacterName.NormalizeName(),
            ClassName = x.ClassName,
            Level = x.Level,
            UserId = x.UserId,
            UserName = x.UserName.NormalizeName(),
            IsMainCharacter = x.IsMainCharacter,
            Rank = x.Rank,
            Character30DayRa = 0,
            Character60DayRa = 0,
            Character90DayRa = 0,
            Player30DayRa = 0,
            Player60DayRa = 0,
            Player90DayRa = 0,
            PlayerCurrentDkp = 0
        }).ToList();
        return allAttendances;
    }

    private ICollection<MusterItemBoughtError> GetItemBoughtErrors(List<MusterItemBoughtResponse> items, MusterRaidUpload raidData, UploadRaidInfo uploadRaidInfo)
        => items.Where(x => x.Status == "failed").Select(i => GetItemError(i, raidData, uploadRaidInfo)).ToList();

    private MusterItemBoughtError GetItemError(MusterItemBoughtResponse itemError, MusterRaidUpload raidData, UploadRaidInfo uploadRaidInfo)
    {
        if (raidData.Items?.Count <= itemError.Index)
        {
            return new MusterItemBoughtError
            {
                Error = itemError.Error,
                ItemIndex = itemError.Index,
                BuyingCharacter = "Uknown",
                ItemName = "Unknown"
            };
        }

        MusterItemBought itemEntry = raidData.Items[itemError.Index];
        DkpUploadInfo itemInfo = uploadRaidInfo.DkpInfo.Skip(itemError.Index).Take(1).FirstOrDefault();

        return new MusterItemBoughtError
        {
            Error = itemError.Error,
            ItemIndex = itemError.Index,
            ItemName = itemEntry.ItemName,
            BuyingCharacter = itemEntry.CharacterBuying,
            DkpSpent = itemEntry.DkpSpent,
            ItemInfo = itemInfo
        };
    }

    private HttpContent GetPostContent(string postBody)
       => new StringContent(postBody);

    private ICollection<CharacterRaidAttendance> GetRaidAttendancesFromResponse(ServerResponse response)
    {
        if (response.ResponseCode != HttpStatusCode.OK)
        {
            Log.Error($"{LogPrefix} Error in response: {response.ResponseCode}: Text:{response.Response}");

            return [];
        }

        AllMusterCharacters allChars = JsonSerializer.Deserialize<AllMusterCharacters>(response.Response);
        Log.Trace($"{LogPrefix} Attendance response: {JsonSerializer.Serialize(allChars, PrettyPrintJsonOption)}");

        List<CharacterRaidAttendance> allAttendances = allChars.Characters.Select(x => new CharacterRaidAttendance
        {
            CharacterId = x.CharacterId,
            CharacterName = x.CharacterName.NormalizeName(),
            ClassName = x.ClassName,
            Level = x.Level,
            UserId = x.UserId,
            UserName = x.UserName.NormalizeName(),
            IsMainCharacter = x.IsMainCharacter,
            Rank = x.Rank,
            Character30DayRa = x.CharacterRa.ThirtyDay.RaidAttendancePercent,
            Character60DayRa = x.CharacterRa.SixtyDay.RaidAttendancePercent,
            Character90DayRa = x.CharacterRa.NinetyDay.RaidAttendancePercent,
            Player30DayRa = x.UserRa.ThirtyDay.RaidAttendancePercent,
            Player60DayRa = x.UserRa.SixtyDay.RaidAttendancePercent,
            Player90DayRa = x.UserRa.NinetyDay.RaidAttendancePercent,
            PlayerCurrentDkp = x.CurrentDkp
        }).ToList();
        return allAttendances;
    }

    private string GetRaidName(UploadRaidInfo uploadRaidInfo)
    {
        IEnumerable<string> zones = uploadRaidInfo.AttendanceInfo.Select(x => x.ZoneName).Distinct();
        AttendanceUploadInfo firstRaid = uploadRaidInfo.AttendanceInfo.OrderBy(x => x.Timestamp).First();
        return $"{firstRaid.Timestamp:yyyyMMdd} - {string.Join(" / ", zones)}";
    }

    private CharacterRaidAttendance GetSingleRaidAttendanceFromResponse(ServerResponse response)
    {
        if (response.ResponseCode != HttpStatusCode.OK)
        {
            Log.Error($"{LogPrefix} Error in response: {response.ResponseCode}: Text:{response.Response}");

            return null;
        }

        MusterCharacter charAttendance = JsonSerializer.Deserialize<MusterCharacter>(response.Response);
        Log.Trace($"{LogPrefix} Attendance response: {JsonSerializer.Serialize(charAttendance, PrettyPrintJsonOption)}");

        CharacterRaidAttendance attendance = new()
        {
            CharacterId = charAttendance.CharacterId,
            CharacterName = charAttendance.CharacterName.NormalizeName(),
            ClassName = charAttendance.ClassName,
            Level = charAttendance.Level,
            UserId = charAttendance.UserId,
            UserName = charAttendance.UserName.NormalizeName(),
            IsMainCharacter = charAttendance.IsMainCharacter,
            Rank = charAttendance.Rank,
            Character30DayRa = charAttendance.CharacterRa.ThirtyDay.RaidAttendancePercent,
            Character60DayRa = charAttendance.CharacterRa.SixtyDay.RaidAttendancePercent,
            Character90DayRa = charAttendance.CharacterRa.NinetyDay.RaidAttendancePercent,
            Player30DayRa = charAttendance.UserRa.ThirtyDay.RaidAttendancePercent,
            Player60DayRa = charAttendance.UserRa.SixtyDay.RaidAttendancePercent,
            Player90DayRa = charAttendance.UserRa.NinetyDay.RaidAttendancePercent,
            PlayerCurrentDkp = charAttendance.CurrentDkp
        };
        return attendance;
    }

    private MusterTimeTickError GetTickError(MusterTimeTickResponse timeTickError, MusterRaidUpload raidData, UploadRaidInfo uploadRaidInfo)
    {
        if (raidData.TimeTicks?.Count <= timeTickError.Index)
        {
            return new MusterTimeTickError
            {
                MembersNotIncluded = timeTickError.CharactersNotCreated,
                Status = timeTickError.Status,
                TickIndex = timeTickError.Index,
                TickName = "Unknown"
            };
        }

        MusterTimeTick timeTick = raidData.TimeTicks[timeTickError.Index];
        AttendanceUploadInfo attendance = uploadRaidInfo.AttendanceInfo.Skip(timeTickError.Index).Take(1).FirstOrDefault();

        return new MusterTimeTickError
        {
            MembersNotIncluded = timeTickError.CharactersNotCreated,
            Status = timeTickError.Status,
            TickIndex = timeTickError.Index,
            TickName = timeTick.TickName,
            AttendanceInfo = attendance
        };
    }

    private ICollection<MusterTimeTickError> GetTimeTickErrors(List<MusterTimeTickResponse> timeTicks, MusterRaidUpload raidData, UploadRaidInfo uploadRaidInfo)
        => timeTicks.Where(x => x.CharactersNotCreated?.Count > 0).Select(t => GetTickError(t, raidData, uploadRaidInfo)).ToList();

    private async Task<ServerResponse> MakeGetCallAsync(string function)
    {
        string uri = $"{_settings.ApiMusterUrl}{function}";

        Log.Debug($"{LogPrefix} ---- Making GET call with URL: {uri}");

        using HttpResponseMessage response = await LocalHttpClient.GetAsync(uri);

        Log.Debug($"{LogPrefix} GET response received.  Response object: {response}");

        string responseText = await response.Content.ReadAsStringAsync();

        Log.Trace($"{LogPrefix} GET response text:{Environment.NewLine}{responseText}");

        ServerResponse responseInfo = new()
        {
            Response = responseText,
            ResponseCode = response.StatusCode
        };

        return responseInfo;
    }

    private MusterDkpRaidUploadResults ProcessUploadRaidResponse(ServerResponse response, MusterRaidUpload raidData, UploadRaidInfo uploadRaidInfo)
    {
        if (response.ResponseCode != HttpStatusCode.OK && response.ResponseCode != HttpStatusCode.Created)
        {
            Log.Error($"{LogPrefix} Error in response: {response.ResponseCode}: Text:{response.Response}");

            MusterDkpRaidUploadResults errorResults = new()
            {
                DryRun = raidData.IsDryRun,
                Error = $"{response.ResponseCode}: Text:{response.Response}"
            };
            return errorResults;
        }

        MusterRaidUploadResponse parsedResponse = JsonSerializer.Deserialize<MusterRaidUploadResponse>(response.Response);

        Log.Trace($"{LogPrefix} Parsed response: {parsedResponse}");

        MusterDkpRaidUploadResults results = new()
        {
            RaidId = parsedResponse.RaidId,
            RaidCreated = parsedResponse.RaidCreated,
            Error = string.Empty,
            DryRun = raidData.IsDryRun,
            TimeTickErrors = GetTimeTickErrors(parsedResponse.TimeTicks, raidData, uploadRaidInfo),
            ItemBoughtErrors = GetItemBoughtErrors(parsedResponse.Items, raidData, uploadRaidInfo)
        };

        Log.Trace($"{LogPrefix} Upload Results: {results}");

        return results;
    }

    private async Task<ServerResponse> UploadMessageAsync(string function, string postBody)
    {
        using HttpContent postContent = GetPostContent(postBody);
        postContent.Headers.ContentType = _mediaHeader;

        string uri = $"{_settings.ApiMusterUrl}{function}";

        Log.Debug($"{LogPrefix} Uploading with URL: {uri}");

        using HttpResponseMessage response = await LocalHttpClient.PostAsync(uri, postContent);

        Log.Debug($"{LogPrefix} Received response.  Response object:{Environment.NewLine}{response}");

        string responseText = await response.Content.ReadAsStringAsync();

        Log.Trace($"{LogPrefix} GET response code:{response.StatusCode}, text:{Environment.NewLine}{responseText}");

        ServerResponse responseInfo = new()
        {
            Response = responseText,
            ResponseCode = response.StatusCode
        };

        return responseInfo;
    }

    [DebuggerDisplay("{DebugText,nq}")]
    private sealed class ServerResponse
    {
        public string Response { get; init; }

        public HttpStatusCode ResponseCode { get; init; }

        private string DebugText
            => $"{ResponseCode}";
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class MusterDkpRaidUploadResults
{
    public ICollection<MusterPreCheckError> CharactersDontExistPreCheck { get; set; } = [];

    public bool DryRun { get; init; }

    public string Error { get; init; }

    public ICollection<MusterItemBoughtError> ItemBoughtErrors { get; init; } = [];

    public bool RaidCreated { get; init; }

    public string RaidId { get; init; }

    public ICollection<MusterTimeTickError> TimeTickErrors { get; init; } = [];

    private string DebugText
       => $"{RaidId} {(DryRun ? "Dry Run" : "")} Tick Errors:{TimeTickErrors?.Count} Item Errors:{ItemBoughtErrors?.Count}";

    public override sealed string ToString()
        => $"{RaidId} {(RaidCreated ? "Created" : "Not Created")} {(DryRun ? "Dry Run" : "")} Error:{Error}{Environment.NewLine}{string.Join(Environment.NewLine, TimeTickErrors)}{Environment.NewLine}{string.Join(Environment.NewLine, ItemBoughtErrors)}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class MusterPreCheckError
{
    public bool BoughtItem
        => ItemBought != null;

    public string CharacterName { get; init; }

    public DkpUploadInfo ItemBought { get; init; }

    private string DebugText
       => $"{CharacterName}{(BoughtItem ? " Bought Item" : "")}";

    public override sealed string ToString()
        => DebugText;
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class MusterTimeTickError
{
    public AttendanceUploadInfo AttendanceInfo { get; init; }

    public ICollection<string> MembersNotIncluded { get; init; }

    public string Status { get; init; }

    public int TickIndex { get; init; }

    public string TickName { get; set; }

    private string DebugText
       => $"{TickName} #Missing:{MembersNotIncluded?.Count}";

    public override sealed string ToString()
        => $"{TickIndex} {TickName} {Status} {string.Join(',', MembersNotIncluded)}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class MusterItemBoughtError
{
    public string BuyingCharacter { get; set; }

    public int DkpSpent { get; set; }

    public string Error { get; init; }

    public int ItemIndex { get; init; }

    public DkpUploadInfo ItemInfo { get; set; }

    public string ItemName { get; set; }

    private string DebugText
       => $"{ItemIndex} {ItemName} {BuyingCharacter} {DkpSpent}DKP";

    public override sealed string ToString()
       => $"{ItemIndex} {ItemName} {BuyingCharacter} {DkpSpent}DKP Error:{Error}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class AllMusterCharacters
{
    [JsonPropertyName("characters")]
    public List<MusterCharacter> Characters { get; set; }

    private string DebugText
       => $"Characters: {Characters?.Count ?? 0}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterCharacter
{
    [JsonPropertyName("character_id")]
    public string CharacterId { get; set; }

    [JsonPropertyName("character_name")]
    public string CharacterName { get; set; }

    [JsonPropertyName("character_ra")]
    public MusterRaSet CharacterRa { get; set; }

    [JsonPropertyName("class")]
    public string ClassName { get; set => field = value ?? string.Empty; }

    [JsonPropertyName("current_dkp")]
    public int CurrentDkp { get; set; }

    [JsonPropertyName("is_main")]
    public bool IsMainCharacter { get; set; }

    [JsonIgnore]
    public int Level { get; set; }

    [JsonPropertyName("rank")]
    public string Rank { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }

    [JsonPropertyName("user_ra")]
    public MusterRaSet UserRa { get; set; }

    [JsonPropertyName("level")]
    internal int? LevelSerialize { get => Level; set => Level = value == null ? 0 : value.Value; }

    private string DebugText
       => $"{CharacterName}[{(IsMainCharacter ? "M" : "A")}] {Level} {ClassName} User:{UserName}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterRaSet
{
    [JsonPropertyName("d90")]
    public MusterRa NinetyDay { get; set; }

    [JsonPropertyName("d60")]
    public MusterRa SixtyDay { get; set; }

    [JsonPropertyName("d30")]
    public MusterRa ThirtyDay { get; set; }

    private string DebugText
       => $"30:{ThirtyDay.RaidAttendancePercent}, 60:{SixtyDay.RaidAttendancePercent}, 90:{NinetyDay.RaidAttendancePercent}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterRa
{
    [JsonPropertyName("attended")]
    public int NumberAttended { get; set; }

    [JsonPropertyName("pct")]
    public int RaidAttendancePercent { get; set; }

    [JsonPropertyName("total")]
    public int TotalPossible { get; set; }

    private string DebugText
       => $"{RaidAttendancePercent}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterRaidUpload
{
    [JsonPropertyName("dry_run")]
    public bool IsDryRun { get; set; }

    [JsonPropertyName("items")]
    public List<MusterItemBought> Items { get; set; }

    [JsonPropertyName("raid_name")]
    public string RaidName { get; set; }

    [JsonPropertyName("ticks")]
    public List<MusterTimeTick> TimeTicks { get; set; }

    private string DebugText
       => $"{RaidName} {(IsDryRun ? "(Dry Run)" : "")} Ticks:{TimeTicks?.Count ?? -1}, Items:{Items?.Count ?? -1}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterTimeTick
{
    [JsonPropertyName("attendees")]
    public List<string> Attendees { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("target")]
    public string TickName { get; set; }

    [JsonPropertyName("time")]
    public string Timestamp { get; set; }

    [JsonPropertyName("zone")]
    public string Zone { get; set; }

    private string DebugText
       => $"{Zone} DKP:{Points} Attend:{Attendees?.Count ?? -1} {Timestamp}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterItemBought
{
    [JsonPropertyName("buyer")]
    public string CharacterBuying { get; set; }

    [JsonPropertyName("item_value")]
    public int DkpSpent { get; set; }

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; }

    [JsonPropertyName("time")]
    public string Timestamp { get; set; }

    private string DebugText
       => $"{ItemName} DKP:{DkpSpent} Buyer:{CharacterBuying} {Timestamp}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterRaidUploadResponse
{
    [JsonPropertyName("dry_run")]
    public bool DryRun { get; set; }

    [JsonPropertyName("items")]
    public List<MusterItemBoughtResponse> Items { get; set; }

    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("created_raid")]
    public bool RaidCreated { get; set; }

    [JsonPropertyName("raid_id")]
    public string RaidId { get; set; }

    [JsonPropertyName("raid_start")]
    public string RaidStartTime { get; set; }

    [JsonPropertyName("ticks")]
    public List<MusterTimeTickResponse> TimeTicks { get; set; }

    private string DebugText
       => $"{(Ok ? "Ok" : "Not OK")} {(DryRun ? "Dry Run" : "")} Ticks:{TimeTicks?.Count ?? -1} Items:{(Items?.Count ?? -1)} RaidID:{RaidId}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterTimeTickResponse
{
    [JsonPropertyName("missing")]
    public List<string> CharactersNotCreated { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("credited")]
    public int PlayersCredited { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    private string DebugText
       => $"{Index} {Status} {PlayersCredited} Missing Chars:{CharactersNotCreated?.Count ?? -1}";
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class MusterItemBoughtResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    private string DebugText
       => $"{Index} {Status} Error:{Error}";
}

public interface IMusterDkpServer
{
    Task<ICollection<CharacterRaidAttendance>> GetAllActiveCharacterAttendancesAsync();

    Task<ICollection<CharacterRaidAttendance>> GetAllCharactersBaseInfoAsync();

    Task<CharacterRaidAttendance> GetCharacterAttendanceAsync(string characterName);

    Task<MusterDkpRaidUploadResults> UploadRaidAsync(UploadRaidInfo uploadRaidInfo);
}

/*
Authentication

Send Authorization: Bearer mst_… on every call. Lookups use a read-only key and uploads a read-write key.
Admins create and revoke keys on a new "API Keys" section of the Administration page. The key is shown once when created.

1. Everyone with attendance: GET /api/v1/parser/characters

`{ "characters": [ {
  "character_id": "cm…", "character_name": "Undertree", "class": "Druid", "level": 60,
  "user_id": "cm…", "user_name": "Undertree",
  "current_dkp": 32606,
  "character_ra": { "d30": {"attended":77,"total":82,"pct":93}, "d60": {…}, "d90": {…} },
  "user_ra":      { "d30": {…}, "d60": {…}, "d90": {…} }
} ] }`

Who's listed: Only characters with at least one raid tick of their own in the last 90 days.
current_dkp: The user's pooled DKP. Character-level DKP is not exposed.
Attendance: It counts raid ticks (attended / total ticks), and pct rounds down. The windows are the last 30, 60 and 90 calendar days. user_ra counts a tick as attended if any of the user's characters was on it.

-----

2. One character: GET /api/v1/parser/characters/{name}

It returns the same object. A missing character returns 404 with {"error":"character_not_found"}. The name is matched ignoring capitalization and it works for any character, not only those with recent attendance.

-----

. Upload a raid: POST /api/v1/parser/raids

`{ "raid_name": "VTNight", "dry_run": false,
  "ticks": [ { "time": "2026-09-15T20:00:00+02:00", "points": 95, "zone": "Vex Thal",
               "target": "optional", "attendees": ["Undertree","Kassandra"] } ],
  "items": [ { "time": "2026-09-15T21:30:00+02:00", "item_name": "Songblade of the Eternal",
               "buyer": "Kassandra", "item_value": 100, "zone": "optional" } ] }`
Adding to an existing raid: Send "raid_id": "…" instead of raid_name. Re-sending the same raid_name with the same earliest tick time also joins the existing raid instead of creating a second one.
Raid start: The earliest tick time.
Reply:
`{ "ok": true, "dry_run": false, "raid_id": "cm…", "raid_start": "2026-09-15T18:00:00Z", "created_raid": true,
  "ticks": [ { "index":0, "status":"recorded", "credited":68, "missing":["Typoname"] } ],
  "items": [ { "index":0, "status":"saved" },
             { "index":1, "status":"failed", "error":"unknown_character" } ],
  "discord": { "status":"posted", "thread_id":"…", "items_posted":1 } }`
Tick statuses: recorded (missing names skipped and listed), duplicate_rejected (a second tick in the same minute), or invalid (for example fractional points).
Item statuses: saved or failed, each on its own. Exact repeats are skipped as duplicate_skipped.
Discord: The first upload creates the thread. Later uploads to the same raid add to it. If the post fails, the upload still succeeds with "discord":{"status":"failed"}.
HTTP codes: 200 whenever the request was processed, even if some rows failed. 400 for a malformed body and 401 for a bad key.
dry_run: It runs every check and writes and posts nothing.

-----

4. Add a forgotten attendee: POST /api/v1/parser/raids/{raid_id}/ticks/attendees

The body is {"time":"…","attendees":["Name"]}. It credits only people not already on that tick and reports anyone still unknown.

Admin pages

API Keys: Create, show once, and revoke.
Raid page, admin only: "Remove tick" and "Remove item". Each confirms, reverses the DKP (an offsetting entry, nothing hard-deleted), and records who did it and when. Removed ticks stop counting toward attendance totals. Since your note in item 4 mentions manually removing duplicate items, I've included item removal.

-----

Created API key "raid-parser" mst_AJ-l5vYW0uxvcgZDkDoy5x-he4sqkTa4aZKpUczh5-M

*/
