// -----------------------------------------------------------------------
// LiveTrackingPocos.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Diagnostics;

[DebuggerDisplay("{DebugText,nq}")]
public sealed class LiveAuctionInfo : IEquatable<LiveAuctionInfo>
{
    private static int currentId = 1;

    public LiveAuctionInfo()
    {
        Id = currentId++;
    }

    public string Auctioneer { get; init; }

    public EqChannel Channel { get; init; }

    public bool HasBids { get; set; }

    public bool HasNewBidsAdded { get; set; }

    public int Id { get; }

    public bool IsRoll { get; init; } = false;

    /// <summary>
    /// Dual use - the item name being bid on for DKP, or if rolling an item or service or message being rolled on.
    /// </summary>
    public string ItemName { get; init; }

    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Dual use - for DKP bidding the total number of items in the auction.  For rolling, the number being /rand'd.
    /// </summary>
    public int TotalNumberOfItems { get; set; }

    private string DebugText
        => IsRoll
        ? $"{Timestamp:HH:mm} {Id} {ItemName} {Auctioneer} rand {TotalNumberOfItems}"
        : $"{Timestamp:HH:mm} {Id} {ItemName} {Auctioneer} {TotalNumberOfItems} item(s)";

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

        if (a.IsRoll && b.IsRoll && a.TotalNumberOfItems == b.TotalNumberOfItems)
            return true;

        return true;
    }

    public override bool Equals(object obj)
        => Equals(obj as LiveAuctionInfo);

    public bool Equals(LiveAuctionInfo other)
        => Equals(this, other);

    public override int GetHashCode()
        => ItemName.GetHashCode();

    public override string ToString()
        => IsRoll
        ? $"{Timestamp:HH:mm} {ItemName} {Auctioneer} roll: {TotalNumberOfItems}{(HasBids ? "*" : "")}"
        : $"{Timestamp:HH:mm} {ItemName} {Auctioneer} {(TotalNumberOfItems > 1 ? "x" + TotalNumberOfItems.ToString() : "")}{(HasBids ? "*" : "")}";
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
        => ParentAuctionId.GetHashCode() ^ BidAmount.GetHashCode() ^ ItemName.GetHashCode() ^ CharacterBeingBidFor.ToUpper().GetHashCode();

    public override string ToString()
    {
        if (IsRoll)
            return $"{Timestamp:HH:mm:ss} {ItemName} {CharacterPlacingBid} rolled {BidAmount}";
        else
            return CharacterNotOnDkpServer
            ? $"{Timestamp:HH:mm:ss} {ItemName} {CharacterBeingBidFor} {BidAmount} NOT ON SERVER"
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

    public string Winners
        => string.Join(Environment.NewLine, SpentCalls.Select(GetSpentInfo));

    private string DebugText
        => $"{ItemName} {AuctionStart.Id}";

    public override string ToString()
        => $"{AuctionStart.Timestamp:HH:mm} {ItemName}";

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
}

public sealed class SuggestedSpentCall
{
    public EqChannel Channel { get; init; }

    public int DkpSpent { get; init; }

    public bool IsRoll { get; init; }

    public string ItemName { get; init; }

    public bool SpentCallSent { get; init; }

    public string Winner { get; init; }

    public override string ToString()
        => IsRoll
        ? $"{ItemName} {Winner} rolled {DkpSpent}"
        : $"{Channel} {Constants.AttendanceDelimiter}{ItemName}{Constants.AttendanceDelimiter} {Winner} {DkpSpent} {Constants.DkpSpent}";
}
