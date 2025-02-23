// -----------------------------------------------------------------------
// ZealAttendanceMessageProvider.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Collections.Generic;

public sealed class ZealAttendanceMessageProvider : IZealMessageUpdater, IZealMessageProvider
{
    private ZealAttendanceMessageProvider()
    { }

    public static ZealAttendanceMessageProvider Instance
        => new();

    public ZealCharacterInfo CharacterInfo { get; private set; }

    public ZealRaidInfo RaidInfo { get; } = new() { InternalAttendees = new List<ZealRaidCharacter>(72) };

    public List<ZealRaidCharacter> GetRaidAttendees()
        => RaidInfo.InternalAttendees;

    public void SetCharacterInfo(ZealCharacterInfo zealCharacterInfo)
        => CharacterInfo = zealCharacterInfo;

    public void SetRaidAttendees(List<ZealRaidCharacter> raidCharacters)
        => RaidInfo.InternalAttendees = raidCharacters;

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
    ZealCharacterInfo CharacterInfo { get; }

    ZealRaidInfo RaidInfo { get; }
}
