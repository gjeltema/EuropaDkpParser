// -----------------------------------------------------------------------
// AttendanceEntry.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{RaidName,nq} {AttendanceCallType,nq}")]
public sealed class AttendanceEntry : IEquatable<AttendanceEntry>
{
    public ICollection<PlayerCharacter> AfkPlayers { get; } = new HashSet<PlayerCharacter>();

    public AttendanceCallType AttendanceCallType { get; set; }

    /// <summary>
    /// Either the call name if a time based entry (e.g. First Call), or the boss name if a Kill entry.
    /// </summary>
    public string CallName { get; set; }

    public ICollection<PlayerCharacter> Players { get; set; } = new HashSet<PlayerCharacter>();

    public PossibleError PossibleError { get; set; }

    public string RawHeaderLogLine { get; set; }

    public DateTime Timestamp { get; set; }

    public string ZoneName { get; set; }

    public static bool Equals(AttendanceEntry a, AttendanceEntry b)
    {
        if (a is null || b is null)
            return false;

        if (a.CallName != b.CallName)
            return false;

        if (a.Timestamp != b.Timestamp)
            return false;

        return true;
    }

    public void AddOrMergeInPlayerCharacter(PlayerCharacter playerCharacter)
    {
        if (playerCharacter == null)
            return;

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

    public override bool Equals(object obj)
        => Equals(obj as AttendanceEntry);

    public bool Equals(AttendanceEntry other)
        => Equals(this, other);

    public override int GetHashCode()
        => CallName.GetHashCode() ^ Timestamp.GetHashCode();

    public string ToDisplayString()
        => $"{Timestamp:HH:mm:ss} {CallName}\t{ZoneName}";

    public string ToDkpServerDescription()
        => AttendanceCallType == AttendanceCallType.Time
            ? $"Attendance - {CallName}"
            : $"{CallName} - KILL";

    public override string ToString()
        => $"{Timestamp:HH:mm:ss} {AttendanceCallType}\t{CallName}\t{ZoneName}";
}
