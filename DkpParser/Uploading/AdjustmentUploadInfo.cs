// -----------------------------------------------------------------------
// AdjustmentUploadInfo.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Uploading;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class AdjustmentUploadInfo
{
    public string AdjustmentReason { get; init; }

    public int CharacterId { get; init; }

    public string CharacterName { get; init; }

    public int DkpAmount { get; init; }

    public int RaidId { get; init; }

    public DateTime Timestamp { get; init; }

    private string DebugText
        => $"{CharacterName}, DKP:{DkpAmount}, RaidID:{RaidId}, Reason:{AdjustmentReason}";

    public override string ToString()
        => DebugText;
}



