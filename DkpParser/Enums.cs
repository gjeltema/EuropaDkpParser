// -----------------------------------------------------------------------
// Enums.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public enum LogEntryType
{
    Unknown = 0,
    Attendance,
    DkpSpent,
    Kill,
    PlayerName,
    WhoZoneName,
    PlayerLooted
}

public enum PossibleError
{
    None = 0,
    TwoColons
}
