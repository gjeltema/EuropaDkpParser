﻿// -----------------------------------------------------------------------
// PlayerCharacter.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText}")]
public sealed class PlayerCharacter : IComparable<PlayerCharacter>
{
    public string ClassName { get; set; }

    public bool IsAnonymous
        => string.IsNullOrEmpty(ClassName);

    public int Level { get; set; }

    public string PlayerName { get; set; }

    public string Race { get; set; }

    private string DebugText
        => IsAnonymous ? $"{PlayerName} ANON" : $"{PlayerName} {ClassName} {Level}";

    public static bool Equals(PlayerCharacter a, PlayerCharacter b)
    {
        if (a is null || b is null)
            return false;

        if (a.PlayerName != b.PlayerName)
            return false;

        if (a.IsAnonymous && b.IsAnonymous)
            return true;

        if (b.IsAnonymous || b.IsAnonymous)
            return false;

        if (a.ClassName != b.ClassName)
            return false;

        if (a.Race != b.Race)
            return false;

        if (a.Level != b.Level)
            return false;

        return true;
    }

    public int CompareTo(PlayerCharacter other)
        => Equals(this, other) ? 1 : -1;

    public override bool Equals(object obj)
        => Equals(this, obj as PlayerCharacter);

    public override int GetHashCode()
    {
        if (IsAnonymous)
        {
            return PlayerName.GetHashCode();
        }
        else
        {
            return PlayerName.GetHashCode() ^ ClassName.GetHashCode() ^ Level;
        }
    }

    public void Merge(PlayerCharacter other)
    {
        if (other is null)
            return;

        if (string.IsNullOrEmpty(PlayerName))
            PlayerName = other.PlayerName;

        if (string.IsNullOrEmpty(ClassName))
            ClassName = other.ClassName;

        if (Level == 0)
            Level = other.Level;

        if (string.IsNullOrEmpty(Race))
            Race = other.Race;
    }

    public string ToDisplayString()
        => IsAnonymous
        ? $"{PlayerName} [ANONYMOUS]"
        : $"{PlayerName} [{Level} {ClassName}] ({Race})";

    public string ToLogString()
    {
        string race = string.IsNullOrEmpty(Race) ? "Unknown" : Race;

        return IsAnonymous
            ? $"[{Constants.Anonymous}] {PlayerName}  <Europa>"
            : $"[{Level} {ClassName}] {PlayerName} ({race}) <Europa>";
    }
}
