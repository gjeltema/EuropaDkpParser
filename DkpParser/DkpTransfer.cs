// -----------------------------------------------------------------------
// DkpTransfer.cs Copyright 2025 Craig Gjeltema
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
        => $"From:{FromCharacter.CharacterName} To:{ToCharacterName}";

    public string ToDisplayString()
        => $"Transfer from: {FromCharacter} to {ToCharacterName}";
}
