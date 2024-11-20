// -----------------------------------------------------------------------
// PlayerCharacter.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText}")]
public sealed class PlayerCharacter : IComparable<PlayerCharacter>
{
    public string CharacterName { get; set; }

    public string ClassName { get; set; }

    public bool IsAnonymous
        => string.IsNullOrEmpty(ClassName);

    public int Level { get; set; }

    public string Race { get; set; }

    private string DebugText
        => IsAnonymous ? $"{CharacterName} ANON" : $"{CharacterName} {ClassName} {Level}";

    public static bool Equals(PlayerCharacter a, PlayerCharacter b)
    {
        if (a is null || b is null)
            return false;

        if (!a.CharacterName.Equals(b.CharacterName, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public int CompareTo(PlayerCharacter other)
        => Equals(this, other) ? 1 : -1;

    public override bool Equals(object obj)
        => Equals(this, obj as PlayerCharacter);

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
