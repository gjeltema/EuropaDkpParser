﻿// -----------------------------------------------------------------------
// ZealMessages.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Diagnostics;
using System.Text.Json.Serialization;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class ZealRaidInfo
{
    public string CharacterName { get; init; }

    public PipeMessageType MessageType
        => PipeMessageType.Raid;

    public ICollection<ZealRaidCharacter> RaidAttendees { get; init; }

    private string DebugText
        => $"{CharacterName} {RaidAttendees?.Count ?? -1}";

    public override string ToString()
       => $"{string.Join(Environment.NewLine, RaidAttendees)}";
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
public sealed class ZealPlayerCharacter
{
    public CharacterInfo CharacterData { get; init; }

    public string CharacterName { get; init; }

    public PipeMessageType MessageType
    => PipeMessageType.Player;

    private string DebugText
        => ToString();

    public override string ToString()
       => $"{CharacterName} {CharacterData}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class Vector3d
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
       => $"{X} {Y} {Z}";
}

[DebuggerDisplay("{DebugText,nq}")]
public class CharacterInfo
{
    [JsonPropertyName("heading")]
    public float Heading { get; set; }

    [JsonPropertyName("location")]
    public Vector3d Position { get; set; }

    [JsonPropertyName("zone")]
    public int ZoneId { get; set; }

    private string DebugText
        => ToString();

    public override string ToString()
       => $"{ZoneId} {Position} {Heading}";
}
