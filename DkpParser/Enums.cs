﻿// -----------------------------------------------------------------------
// Enums.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public enum AttendanceCallType
{
    Time,
    Kill
}

public enum LogEntryType
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
    LeftRaid
}

public enum PossibleError
{
    None = 0,
    TwoColons,
    DkpAmountNotANumber,
    DkpSpentPlayerNameTypo,
    DkpDuplicateEntry,
    PlayerLootedMessageNotFound,
    DuplicateRaidEntry,
    BossMobNameTypo,
    NoZoneName
}
