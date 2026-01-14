// -----------------------------------------------------------------------
// DiscountsApplied.cs Copyright 2026 Craig Gjeltema
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
    {
        string itemName = Item.Replace('`', '\'');
        return $"{CharacterName} for {itemName} reducing {OriginalSpent} to {AfterDiscountSpent} DKP for raid {AttendanceName} in {AttendanceZone}.";
    }
}
