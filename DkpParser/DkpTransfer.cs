// -----------------------------------------------------------------------
// DkpTransfer.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText}")]
public sealed class DkpTransfer
{
    public PlayerCharacter FromCharacter { get; init; }

    public string LogLine { get; init; }

    public DateTime Timestamp { get; init; }

    public string ToCharacterName { get; init; }

    private string DebugText
        => ToString();

    public string ToDisplayString()
        => $"Transfer from: {FromCharacter} to {ToCharacterName}";

    public override string ToString()
        => $"{FromCharacter.CharacterName} -> {ToCharacterName}";
}
