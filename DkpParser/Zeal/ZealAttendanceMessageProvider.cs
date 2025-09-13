// -----------------------------------------------------------------------
// ZealAttendanceMessageProvider.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Collections.Generic;

public sealed class ZealAttendanceMessageProvider : IZealMessageUpdater, IZealMessageProvider
{
    public event EventHandler<ZealPipeErrorEventArgs> PipeError;

    private ZealAttendanceMessageProvider()
    { }

    public static ZealAttendanceMessageProvider Instance { get; } = new();

    public ZealCharacterInfo CharacterInfo { get; private set; } = new();

    public ZealRaidInfo RaidInfo { get; } = new() { InternalAttendees = new List<ZealRaidCharacter>(72) };

    public List<ZealRaidCharacter> GetRaidAttendees()
        => RaidInfo.InternalAttendees;

    public void SendPipeError(string errorMessage, Exception errorException)
        => PipeError?.Invoke(this, new ZealPipeErrorEventArgs { ErrorMessage = errorMessage, ErrorException = errorException });

    public void SetCharacterInfo(ZealCharacterInfo zealCharacterInfo)
    {
        CharacterInfo = zealCharacterInfo;
        CharacterInfo.LastUpdate = DateTime.Now;
    }

    public void SetRaidAttendees(List<ZealRaidCharacter> raidCharacters)
    {
        RaidInfo.InternalAttendees = raidCharacters;
        RaidInfo.LastUpdate = DateTime.Now;
    }

    public void StartMessageProcessing(string characterName)
    {
        ZealNamedPipe.Instance.StartListening(characterName, this);
    }

    public void StopMessageProcessing()
    {
        ZealNamedPipe.Instance.StopListening();
    }
}

public interface IZealMessageProvider
{
    event EventHandler<ZealPipeErrorEventArgs> PipeError;

    ZealCharacterInfo CharacterInfo { get; }

    ZealRaidInfo RaidInfo { get; }

    void StartMessageProcessing(string characterName);

    void StopMessageProcessing();
}

public sealed class ZealPipeErrorEventArgs : EventArgs
{
    public Exception ErrorException { get; init; }

    public string ErrorMessage { get; init; }
}
