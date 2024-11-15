// -----------------------------------------------------------------------
// DkpTransfer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpTransfer
{
    public PlayerCharacter FromCharacter { get; set; }

    public PlayerCharacter ToCharacter { get; set; }

    public string ToDisplayString()
        => $"Transfer from: {FromCharacter} to {ToCharacter}";
}
