// -----------------------------------------------------------------------
// AttendanceEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{RaidName,nq} {AttendanceCallType,nq}")]
public sealed class AttendanceEntry
{
    public AttendanceCallType AttendanceCallType { get; set; }

    public ICollection<PlayerCharacter> Players { get; private set; } = new HashSet<PlayerCharacter>();

    public PossibleError PossibleError { get; set; }

    public string RaidName { get; set; }

    public DateTime Timestamp { get; set; }

    public string ZoneName { get; set; }

    public void AddOrMergeInPlayerCharacter(PlayerCharacter playerCharacter)
    {
        PlayerCharacter currentChar = Players.FirstOrDefault(x => x.PlayerName == playerCharacter.PlayerName);
        if (currentChar != null)
        {
            currentChar.Merge(playerCharacter);
        }
        else
        {
            Players.Add(playerCharacter);
        }
    }

    public override string ToString()
        => $"{Timestamp:HH:mm:ss} {AttendanceCallType}\t{RaidName}\t{ZoneName}";
}
