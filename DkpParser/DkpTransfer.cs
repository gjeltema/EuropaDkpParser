// -----------------------------------------------------------------------
// DkpTransfer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText}")]
public sealed class DkpTransfer
{
    public PlayerCharacter FromCharacter { get; set; }

    public string ToCharacterName { get; set; }

    private string DebugText
        => $"From:{FromCharacter.CharacterName} To:{ToCharacterName}";

    public string ToDisplayString()
        => $"Transfer from: {FromCharacter} to {ToCharacterName}";
}
