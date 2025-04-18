﻿// -----------------------------------------------------------------------
// PlayerCharacter.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText}")]
public sealed class PlayerCharacter : IEquatable<PlayerCharacter>
{
    private string _characterName;

    public string CharacterName
    {
        get => _characterName;
        set => _characterName = value.NormalizeName();
    }

    public string ClassName { get; set; }

    public bool IsAnonymous
        => string.IsNullOrEmpty(ClassName);

    public int Level { get; set; }

    public string Race { get; set; }

    private string DebugText
        => IsAnonymous ? $"{CharacterName} ANON" : $"{CharacterName} {ClassName} {Level}";

    public static bool operator ==(PlayerCharacter a, PlayerCharacter b)
        => Equals(a, b);

    public static bool operator !=(PlayerCharacter a, PlayerCharacter b)
        => !Equals(a, b);

    public static bool Equals(PlayerCharacter a, PlayerCharacter b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        if (!a.CharacterName.Equals(b.CharacterName, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public override bool Equals(object obj)
        => Equals(this, obj as PlayerCharacter);

    public bool Equals(PlayerCharacter other)
        => Equals(this, other);

    public override int GetHashCode()
        => CharacterName.GetHashCode();

    public void Merge(PlayerCharacter other)
    {
        if (other is null)
            return;

        if (string.IsNullOrEmpty(CharacterName))
            CharacterName = other.CharacterName;

        if (string.IsNullOrEmpty(ClassName))
            ClassName = other.ClassName;

        if (Level == 0)
            Level = other.Level;

        if (string.IsNullOrEmpty(Race))
            Race = other.Race;
    }

    public string ToDisplayString()
        => IsAnonymous
        ? $"{CharacterName} {Constants.AnonWithBrackets}"
        : $"{CharacterName} [{Level} {ClassName}] ({(string.IsNullOrEmpty(Race) ? "Uknown" : Race)})";

    public override string ToString()
        => ToDisplayString();
}
