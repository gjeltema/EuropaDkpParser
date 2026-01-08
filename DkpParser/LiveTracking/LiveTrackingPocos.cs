// -----------------------------------------------------------------------
// LiveTrackingPocos.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveAuctionInfo : IEquatable<LiveAuctionInfo>
{
    private static int currentId = 1;
    private StatusUpdateInfo _lastStatusUpdate;

    public LiveAuctionInfo()
    {
        Id = currentId++;
    }

    public string Auctioneer { get; init; }

    public EqChannel Channel { get; set; }

    public bool HasBids { get; set; }

    public bool HasNewBidsAdded { get; set; }

    public int Id { get; }

    public bool IsRoll { get; init; } = false;

    /// <summary>
    /// Dual use - the item name being bid on for DKP, or if rolling an item or service or message being rolled on.
    /// </summary>
    public string ItemName { get; init; }

    public int RandValue { get; init; }

    public DateTime Timestamp { get; init; }

    public int TotalNumberOfItems { get; set; }

    private string DebugText
        => IsRoll
        ? $"{Timestamp:HH:mm} {Id} {ItemName} {Auctioneer} x{TotalNumberOfItems} rand: {RandValue}"
        : $"{Timestamp:HH:mm} {Id} {ItemName} {Auctioneer} x{TotalNumberOfItems} item(s)";

    public static bool operator ==(LiveAuctionInfo a, LiveAuctionInfo b)
        => Equals(a, b);

    public static bool operator !=(LiveAuctionInfo a, LiveAuctionInfo b)
        => !Equals(a, b);

    public static bool Equals(LiveAuctionInfo a, LiveAuctionInfo b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        if (a.ItemName != b.ItemName)
            return false;

        if (a.IsRoll != b.IsRoll)
            return false;

        if (a.IsRoll && b.IsRoll && a.RandValue != b.RandValue)
            return false;

        return true;
    }

    public override bool Equals(object obj)
        => Equals(obj as LiveAuctionInfo);

    public bool Equals(LiveAuctionInfo other)
        => Equals(this, other);

    public override int GetHashCode()
        => ItemName.GetHashCode();

    public void SetStatusUpdate(string auctioneer, DateTime timestamp, string updateState)
    {
        _lastStatusUpdate = new StatusUpdateInfo
        {
            Auctioneer = auctioneer,
            Timestamp = timestamp,
            UpdateState = updateState
        };
    }

    public override string ToString()
        => IsRoll
        ? $"{Timestamp:HH:mm} {ItemName}{(TotalNumberOfItems > 1 ? " x" + TotalNumberOfItems.ToString() : "")} roll:{RandValue}{GetStatusUpdateText()}{(HasBids ? " *" : "")}"
        : $"{Timestamp:HH:mm} {ItemName}{(TotalNumberOfItems > 1 ? " x" + TotalNumberOfItems.ToString() : "")}{GetStatusUpdateText()}{(HasBids ? " *" : "")}";

    private string GetStatusUpdateText()
    {
        if (_lastStatusUpdate == null)
            return string.Empty;

        TimeSpan timeSinceUpdate = DateTime.Now - _lastStatusUpdate.Timestamp;
        string timeSinceUpdateText;
        if (timeSinceUpdate.TotalSeconds < 60)
            timeSinceUpdateText = timeSinceUpdate.ToString("s's'");
        else if (timeSinceUpdate.TotalSeconds < 3600)
            timeSinceUpdateText = timeSinceUpdate.ToString("m'm's's'");
        else if (timeSinceUpdate.TotalDays < 1)
            timeSinceUpdateText = timeSinceUpdate.ToString("h'h'm'm's's'");
        else
            timeSinceUpdateText = "very long";

        return $" (Update {timeSinceUpdateText} ago by {_lastStatusUpdate.Auctioneer} {_lastStatusUpdate.UpdateState})";
    }

    private sealed class StatusUpdateInfo
    {
        public string Auctioneer { get; init; }

        public DateTime Timestamp { get; init; }

        public string UpdateState { get; init; }
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveBidInfo : IEquatable<LiveBidInfo>
{
    /// <summary>
    /// Dual use - for DKP bid, amount bid by player.  For roll, the rolled result.
    /// </summary>
    public int BidAmount { get; init; }

    public EqChannel Channel { get; init; }

    public string CharacterBeingBidFor { get; set; }

    public bool CharacterNotOnDkpServer { get; set; }

    public string CharacterPlacingBid { get; init; }

    public bool IsRoll { get; init; }

    /// <summary>
    /// Dual use - the name of the item being bid on for DKP bids, or the message/service/item being rolled on for rolls.
    /// </summary>
    public string ItemName { get; init; }

    public int ParentAuctionId { get; init; }

    public DateTime Timestamp { get; init; }

    private string DebugText
        => $"{Timestamp:HH:mm:ss} {ParentAuctionId} {ItemName} {CharacterBeingBidFor} {BidAmount}";

    public static bool operator ==(LiveBidInfo left, LiveBidInfo right)
        => Equals(left, right);

    public static bool operator !=(LiveBidInfo left, LiveBidInfo right)
        => !Equals(left, right);

    public static bool Equals(LiveBidInfo left, LiveBidInfo right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (left.IsRoll != right.IsRoll)
            return false;

        if (left.ParentAuctionId != right.ParentAuctionId)
            return false;

        if (left.BidAmount != right.BidAmount)
            return false;

        if (left.ItemName != right.ItemName)
            return false;

        if (!left.IsRoll && !left.CharacterBeingBidFor.Equals(right.CharacterBeingBidFor, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public override bool Equals(object other)
        => Equals(this, other as LiveBidInfo);

    public bool Equals(LiveBidInfo other)
        => Equals(this, other);

    public override int GetHashCode()
        => HashCode.Combine(ParentAuctionId, BidAmount, ItemName, CharacterBeingBidFor);

    public override string ToString()
    {
        if (IsRoll)
            return $"{Timestamp:HH:mm:ss} {ItemName} {CharacterPlacingBid} rolled {BidAmount}";
        else
            return CharacterNotOnDkpServer
            ? $"{Timestamp:HH:mm:ss} {ItemName} {CharacterBeingBidFor} {BidAmount} MAYBE NOT ON SERVER"
            : $"{Timestamp:HH:mm:ss} {ItemName} {CharacterBeingBidFor} {BidAmount}";
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveSpentCall
{
    public string Auctioneer { get; init; }

    public LiveAuctionInfo AuctionStart { get; set; }

    public EqChannel Channel { get; init; }

    public string CharacterPlacingBid { get; set; }

    public int DkpSpent { get; init; }

    public bool IsRemoveCall { get; init; }

    public string ItemName { get; init; }

    public DateTime Timestamp { get; init; }

    public string Winner { get; init; }

    private string DebugText
        => $"{Timestamp:HH:mm} {ItemName} {Winner} {DkpSpent}";

    public override string ToString()
        => $"{Channel} {Constants.AttendanceDelimiter}{ItemName}{Constants.AttendanceDelimiter} {Winner} {DkpSpent} {Constants.DkpSpent}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class CompletedAuction
{
    public LiveAuctionInfo AuctionStart { get; set; }

    public string ItemName { get; set; }

    public ICollection<LiveSpentCall> SpentCalls { get; set; }

    // Binding in the xaml to this
    public string Winners
        => string.Join(Environment.NewLine, SpentCalls.Select(GetSpentInfo));

    private string DebugText
        => $"{ItemName} {AuctionStart.Id}";

    public override string ToString()
    => $"{AuctionStart.Timestamp:HH:mm} {ItemName} {string.Join(", ", SpentCalls.Select(GetWinnerSummary))}";

    private string GetSpentInfo(LiveSpentCall spent)
    {
        if (AuctionStart.IsRoll)
        {
            return $"{spent.Winner} rolled {spent.DkpSpent}";
        }
        else
        {
            if (spent.Winner == spent.CharacterPlacingBid)
                return $"{spent.Winner} {spent.DkpSpent} DKP";
            else
                return $"{spent.Winner} ({spent.CharacterPlacingBid}) {spent.DkpSpent} DKP";
        }
    }

    private string GetWinnerSummary(LiveSpentCall spent)
    {
        if (AuctionStart.IsRoll)
        {
            return $"{spent.Winner}";
        }
        else
        {
            if (spent.Winner == spent.CharacterPlacingBid)
                return $"{spent.Winner}";
            else
                return $"{spent.Winner} ({spent.CharacterPlacingBid})";
        }
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class SuggestedSpentCall
{
    public EqChannel Channel { get; init; }

    public int DkpSpent { get; init; }

    public bool IsRoll { get; init; }

    public string ItemName { get; init; }

    public bool SpentCallSent { get; init; }

    public string Winner { get; init; }

    private string DebugText
        => $"{ItemName} {Winner} {DkpSpent} {(IsRoll ? "IsRoll" : "IsDKP")}";

    public override string ToString()
        => IsRoll
        ? $"{ItemName} {Winner} rolled {DkpSpent}"
        : $"{Channel} {Constants.AttendanceDelimiter}{ItemName}{Constants.AttendanceDelimiter} {Winner} {DkpSpent} {Constants.DkpSpent}";
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class CharacterReadyCheckStatus
{
    public string CharacterName { get; init; }

    public bool? IsReady { get; set; }

    private string DebugText
        => ToString();

    public string ToDebugString()
        => $"{CharacterName} '{IsReady}'";

    public override string ToString()
         => CharacterName;
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class MezBreak
{
    public string CharacterName { get; init; }

    public string MobName { get; init; }

    public string Reason { get; init; }

    public DateTime TimeOfBreak { get; set; }

    private string DebugText
        => ToString();

    public override string ToString()
         => $"[{TimeOfBreak:HH:mm:ss}] ({CharacterName} - {Reason}) broke {MobName}";
}
