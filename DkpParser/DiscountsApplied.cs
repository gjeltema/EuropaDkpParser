// -----------------------------------------------------------------------
// DiscountsApplied.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class DiscountApplied
{
    public int AfterDiscountSpent { get; init; }

    public string AttendanceName { get; init; }

    public string AttendanceZone { get; init; }

    public string CharacterName { get; init; }

    public string Item { get; init; }

    public int OriginalSpent { get; init; }

    private string DebugText
        => $"{CharacterName} {Item} {OriginalSpent}->{AfterDiscountSpent}, {AttendanceName}";

    public string ToDisplayString()
        => $"{CharacterName} for {Item} reducing {OriginalSpent} to {AfterDiscountSpent} DKP for raid {AttendanceName} in {AttendanceZone}.";
}
