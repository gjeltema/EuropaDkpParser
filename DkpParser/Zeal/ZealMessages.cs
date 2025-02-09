// -----------------------------------------------------------------------
// ZealMessages.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Text.Json.Serialization;

public sealed class ZealRaidInfo
{
    public string CharacterName { get; init; }

    public PipeMessageType MessageType
        => PipeMessageType.Raid;

    public ICollection<ZealRaidCharacter> RaidAttendees { get; init; }
}

public sealed class ZealRaidCharacter
{
    [JsonPropertyName("class")]
    public string Class { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rank")]
    public string Rank { get; set; }
}

public sealed class ZealPlayerCharacter
{
    public CharacterInfo CharacterData { get; init; }

    public string CharacterName { get; init; }

    public PipeMessageType MessageType
        => PipeMessageType.Player;
}

public sealed class Vector3d
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("z")]
    public float Z { get; set; }
}

public class CharacterInfo
{
    [JsonPropertyName("heading")]
    public float Heading { get; set; }

    [JsonPropertyName("location")]
    public Vector3d Position { get; set; }

    [JsonPropertyName("zone")]
    public int ZoneId { get; set; }
}
