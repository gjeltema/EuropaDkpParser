// -----------------------------------------------------------------------
// IZealMessageUpdater.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

public interface IZealMessageUpdater
{
    List<ZealRaidCharacter> GetRaidAttendees();

    void SetCharacterInfo(ZealCharacterInfo zealCharacterInfo);

    void SetRaidAttendees(List<ZealRaidCharacter> raidCharacters);
}
