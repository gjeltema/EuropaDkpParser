// -----------------------------------------------------------------------
// ZealPipeMessageProcessor.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Text.Json;
using Gjeltema.Logging;

public sealed class ZealPipeMessageProcessor
{
    private readonly ZealPipe _zealPipe = ZealPipe.Instance;

    private ZealPipeMessageProcessor() { }

    public static ZealPipeMessageProcessor Instance
        => new();

    public ZealPlayerCharacter CharacterInfo { get; private set; }

    public ZealRaidInfo RaidInfo { get; private set; }

    public void StartListeningToPipe()
    {
        _zealPipe.ZealPipeMessageReceived += HandlePipeMessages;
        _zealPipe.StartListening();
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
        CharacterInfo characterInfo = JsonSerializer.Deserialize<CharacterInfo>(message.Data);
        ZealPlayerCharacter character = new()
        {
            CharacterName = message.Character,
            CharacterData = characterInfo
        };

        CharacterInfo = character;

        Log.Trace($"[{nameof(ZealPipeMessageProcessor)}] ZealPlayerCharacter message: {character}");
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

        RaidInfo = raidInfo;

        Log.Trace($"[{nameof(ZealPipeMessageProcessor)}] ZealRaidInfo message: {raidInfo}");
    }
}
