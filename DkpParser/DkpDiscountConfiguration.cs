// -----------------------------------------------------------------------
// DkpDiscountConfiguration.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class DkpDiscountConfiguration
{
    public string ClassName { get; init; }

    /// <summary>
    /// The fraction to multiple the won amount with to get the final DKP value.  e.g. if the discount is 15%, then this fraction would be 0.85
    /// </summary>
    public double DiscountFraction { get; init; }

    public string DiscountZoneOrBoss { get; init; }

    /// <summary>
    /// Minimum Raid Attendance that the class must have to be eligible for the discount, in %. e.g. 10
    /// </summary>
    public int MinimumRAThreshold { get; init; }

    private string DebugText
        => $"{ClassName}, Zone or Boss:{DiscountZoneOrBoss}, Discount: {DiscountFraction}, RA:{MinimumRAThreshold}%";
}
