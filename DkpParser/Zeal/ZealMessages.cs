// -----------------------------------------------------------------------
// ZealMessages.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Diagnostics;
using System.Text.Json.Serialization;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class ZealRaidInfo
{
    private readonly TimeSpan _threshold = TimeSpan.FromSeconds(4);

    public bool IsDataStale
        => (DateTime.Now - LastUpdate) > _threshold;

    public DateTime LastUpdate { get; set; } = DateTime.MinValue;

    public PipeMessageType MessageType
        => PipeMessageType.Raid;

    public ICollection<ZealRaidCharacter> RaidAttendees
        => InternalAttendees;

    internal List<ZealRaidCharacter> InternalAttendees { get; set; }

    private string DebugText
        => $"{RaidAttendees?.Count ?? -1}";

    public override string ToString()
       => $"{string.Join(";", RaidAttendees)}";
}

[DebuggerDisplay("{DebugText,nq}")]
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

    private string DebugText
        => ToString();

    public override string ToString()
        => $"{Name} {Class} {Level} {Group} {Rank}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class ZealVector3d
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("z")]
    public float Z { get; set; }

    private string DebugText
        => ToString();

    public override string ToString()
       => $"{X:0.0} {Y:0.0} {Z:0.0}";
}

[DebuggerDisplay("{DebugText,nq}")]
public class ZealCharacterInfo
{
    private readonly TimeSpan _threshold = TimeSpan.FromSeconds(4);

    //** Need to get Json name
    [JsonPropertyName("name")]
    public string CharacterName { get; set; }

    [JsonPropertyName("heading")]
    public float Heading { get; set; }

    public bool IsDataStale
        => (DateTime.Now - LastUpdate) > _threshold;

    public DateTime LastUpdate { get; set; } = DateTime.MinValue;

    [JsonPropertyName("location")]
    public ZealVector3d Position { get; set; }

    [JsonPropertyName("zone")]
    public int ZoneId { get; set; }

    private string DebugText
        => ToString();

    public override string ToString()
       => $"{ZoneId} {Position} {Heading}";
}

[JsonSerializable(typeof(ZealCharacterInfo))]
internal partial class ZealCharacterInfoGenerationContext : JsonSerializerContext
{
}
