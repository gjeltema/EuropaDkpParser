// -----------------------------------------------------------------------
// Enums.cs Copyright 2024 Craig Gjeltema
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
    Shout,
    Say,
    Tell,
    Custom
}

public enum LogEntryType : byte
{
    Unknown = 0,
    Attendance,
    DkpSpent,
    Kill,
    PlayerName,
    WhoZoneName,
    PlayerLooted,
    Conversation,
    JoinedRaid,
    LeftRaid,
    Crashed,
    AfkStart,
    AfkEnd
}

public enum PossibleError : byte
{
    None = 0,
    ZeroDkp,
    DkpSpentPlayerNameTypo,
    DkpDuplicateEntry,
    PlayerLootedMessageNotFound,
    DuplicateRaidEntry,
    BossMobNameTypo,
    NoZoneName,
    RaidNameTooShort,
    InvalidZoneName
}
