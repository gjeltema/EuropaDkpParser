namespace DkpParser;

public sealed class AttendanceEntryModifier : IAttendanceEntryModifier
{
    private readonly LogParseResults _results;
    private readonly RaidEntries _raidEntries;

    public AttendanceEntryModifier(LogParseResults results, RaidEntries raidEntries) 
    {
        _results = results;
        _raidEntries = raidEntries;
    }

    public AttendanceEntry CreateAttendanceEntry(AttendanceEntry baseline, DateTime timestamp)
    {

        return null;
    }

    public void MoveAttendanceEntry(AttendanceEntry baseline, AttendanceEntry toBeMoved, DateTime newTimestamp)
    {

    }
}

public interface IAttendanceEntryModifier
{
    void MoveAttendanceEntry(AttendanceEntry baseline, AttendanceEntry toBeMoved, DateTime newTimestamp);

    AttendanceEntry CreateAttendanceEntry( AttendanceEntry baseline, DateTime timestamp);
}
