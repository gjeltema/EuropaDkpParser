// -----------------------------------------------------------------------
// Enums.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public enum AttendanceCallType : byte
{
    Time,
    Kill
}

public enum EqChannel : byte
{
    None = 0,
    Raid,
    Guild,
    Ooc,
    Auction,
    Group,
    Shout,
    Say,
    Tell,
    ReadyCheck,
    Custom
}

public enum LogEntryType : byte
{
    Unknown = 0,
    Attendance,
    DkpSpent,
    PossibleDkpSpent,
    Kill,
    CharacterName,
    WhoZoneName,
    CharacterLooted,
    Conversation,
    JoinedRaid,
    LeftRaid,
    Crashed,
    AfkStart,
    AfkEnd,
    Transfer
}

public enum PossibleError : byte
{
    None = 0,
    ZeroDkp,
    DkpSpentPlayerNameTypo,
    DkpDuplicateEntry,
    MalformedDkpSpentLine,
    PlayerLootedMessageNotFound,
    DuplicateRaidEntry,
    BossMobNameTypo,
    NoZoneName,
    RaidNameTooShort,
    InvalidZoneName,
    MultipleCharactersFromOneAccount
}

public enum LiveAuctionMessageType : byte
{
    Start = 1,
    End
}

public enum StatusMarker : byte
{
    Completed,
    TenSeconds,
    ThirtySeconds,
    SixtySeconds
}

public enum PipeMessageType
{
    LogText,
    Label,
    Gauge,
    Player,
    PipeCmd, // custom in zeal
    Raid
}
