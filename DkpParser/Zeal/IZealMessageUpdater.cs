// -----------------------------------------------------------------------
// IZealMessageUpdater.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

public interface IZealMessageUpdater
{
    List<ZealRaidCharacter> GetRaidAttendees();

    void SendPipeError(string errorMessage, Exception errorException);

    void SetCharacterInfo(ZealCharacterInfo zealCharacterInfo);

    void SetRaidAttendees(List<ZealRaidCharacter> raidCharacters);
}
