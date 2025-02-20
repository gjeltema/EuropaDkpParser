// -----------------------------------------------------------------------
// ZealPipeMessageProcessor.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Diagnostics;
using System.Text.Json;
using Gjeltema.Logging;

public sealed class ZealPipeMessageProcessor
{
    private const string LogPrefix = $"[{nameof(ZealPipeMessageProcessor)}]";
    private readonly ZealPipe _zealPipe = ZealPipe.Instance;

    private ZealPipeMessageProcessor()
    {
        CharacterInfo = new()
        {
            CharacterName = string.Empty,
            ZoneId = 0,
            CharacterPosition = new Vector3d { X = 0, Y = 0, Z = 0 }
        };
    }

    public static ZealPipeMessageProcessor Instance
        => new();

    public RemotePlayerCharacterInfo CharacterInfo { get; }

    public ICollection<RemoteRaidCharacterInfo> RaidInfo { get; private set; } = new List<RemoteRaidCharacterInfo>(72);

    public void StartListeningToPipe(string characterName)
    {
        StopListeningToPipe();

        _zealPipe.ZealPipeMessageReceived += HandlePipeMessages;
        _zealPipe.StartListening(characterName);
    }

    public void StopListeningToPipe()
    {
        _zealPipe.StopListening();
        _zealPipe.ZealPipeMessageReceived -= HandlePipeMessages;
    }

    private void HandlePipeMessages(object sender, ZealPipeMessageEventArgs e)
    {
        switch (e.Message.MessageType)
        {
            case PipeMessageType.Raid:
                HandleRaidMessage(e.Message);
                break;
            case PipeMessageType.Player:
                HandlePlayerMessage(e.Message);
                break;
            default:
                break;
        }
    }

    private void HandlePlayerMessage(ZealPipeMessage message)
    {
        ZealCharacterInfo characterInfo = JsonSerializer.Deserialize<ZealCharacterInfo>(message.Data);
        ZealPlayerCharacter character = new()
        {
            CharacterName = message.Character,
            CharacterData = characterInfo
        };

        CharacterInfo.CharacterName = character.CharacterName;
        CharacterInfo.ZoneId = character.CharacterData.ZoneId;
        CharacterInfo.CharacterPosition.X = character.CharacterData.Position.X;
        CharacterInfo.CharacterPosition.Y = character.CharacterData.Position.Y;
        CharacterInfo.CharacterPosition.Z = character.CharacterData.Position.Z;

        Log.Trace($"{LogPrefix} ZealPlayerCharacter message: {character}");
    }

    private void HandleRaidMessage(ZealPipeMessage message)
    {
        ICollection<ZealRaidCharacter> raidAttendees = JsonSerializer.Deserialize<ZealRaidCharacter[]>(message.Data)
            .Where(x => !string.IsNullOrEmpty(x?.Level))
            .ToList();

        ZealRaidInfo raidInfo = new()
        {
            CharacterName = message.Character,
            RaidAttendees = raidAttendees
        };

        UpdateRaidListing(raidInfo);

        Log.Trace($"{LogPrefix} ZealRaidInfo message: {raidInfo}");
    }

    private void UpdateRaidListing(ZealRaidInfo raidInfo)
    {
        RaidInfo.Clear();

        foreach (ZealRaidCharacter raidChar in raidInfo.RaidAttendees)
        {
            RemoteRaidCharacterInfo newRaidChar = new()
            {
                CharacterName = raidChar.Name,
                Class = raidChar.Class,
                Group = raidChar.Group,
                Level = raidChar.Level,
                Rank = raidChar.Rank,
            };

            RaidInfo.Add(newRaidChar);

            //RemoteRaidCharacterInfo existingRaidChar = RaidInfo.FirstOrDefault(x => x.CharacterName == raidChar.Name);
            //if (existingRaidChar == null)
            //{
            //    RemoteRaidCharacterInfo newRaidChar = new()
            //    {
            //        CharacterName = raidChar.Name,
            //        Class = raidChar.Class,
            //        Group = raidChar.Group,
            //        Level = raidChar.Level,
            //        Rank = raidChar.Rank,
            //    };

            //    RaidInfo.Add(newRaidChar);
            //}
            //else
            //{
            //    existingRaidChar.Group = raidChar.Group;
            //    existingRaidChar.Level = raidChar.Level;
            //    existingRaidChar.Rank = raidChar.Rank;
            //}
        }
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class RemotePlayerCharacterInfo
{
    public string CharacterName { get; set; }

    public Vector3d CharacterPosition { get; set; }

    public int ZoneId { get; set; }

    private string DebugText
        => ToString();

    public override string ToString()
        => $"{CharacterName} {CharacterPosition} {ZoneId}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class Vector3d
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    private string DebugText
        => ToString();

    public override string ToString()
       => $"{X:0} {Y:0} {Z:0}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class RemoteRaidCharacterInfo
{
    public string CharacterName { get; set; }

    public string Class { get; set; }

    public string Group { get; set; }

    public string Level { get; set; }

    public string Rank { get; set; }

    private string DebugText
        => ToString();

    public override string ToString()
        => $"{CharacterName} {Class} {Level} {Group} {Rank}";
}
