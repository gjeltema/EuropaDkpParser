// -----------------------------------------------------------------------
// ReminderDialogViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal sealed class ReminderDialogViewModel : DialogViewModelBase, IReminderDialogViewModel
{
    private readonly IAttendanceSnapshot _attendanceSnapshot;
    private readonly List<string> _timeCalls = ["First Call", "Second Call", "Third Call", "Fourth Call", "Fifth Call", "Sixth Call"
            , "Seventh Call", "Eighth Call", "Ninth Call", "Tenth Call", "Eleventh Call", "Twelfth Call", "Thirteenth Call", "Fourteenth Call"];
    private string _attendanceName;
    private int _reminderInterval;
    private string _reminderText;
    private int _timeCallIndex = 0;

    public ReminderDialogViewModel(IDialogViewFactory viewFactory, IAttendanceSnapshot attendanceSnapshot)
        : base(viewFactory)
    {
        Title = Strings.GetString("ReminderDialogTitleText");
        Height = 220;
        Width = 400;

        _attendanceSnapshot = attendanceSnapshot;

        ReminderInterval = 3;

        CopyAttendanceCallCommand = new DelegateCommand(CopyAttendanceCall);
    }

    public string AttendanceName
    {
        get => _attendanceName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (SetProperty(ref _attendanceName, value))
            {
                int indexOfSelection = _timeCalls.IndexOf(value);
                if (indexOfSelection > -1)
                    _timeCallIndex = indexOfSelection;
            }
        }
    }

    public AttendanceCallType AttendanceType { get; set; }

    public DelegateCommand CopyAttendanceCallCommand { get; }

    public bool IsTimeCall
        => AttendanceType == AttendanceCallType.Time;

    public int ReminderInterval
    {
        get => _reminderInterval;
        set => SetProperty(ref _reminderInterval, value);
    }

    public string ReminderText
    {
        get => _reminderText;
        set => SetProperty(ref _reminderText, value);
    }

    public ICollection<string> TimeCalls
        => _timeCalls;

    public int GetTimeCallIndex()
        => _timeCallIndex;

    public void SetTimeCallIndex(int timeCallIndex)
    {
        _timeCallIndex = Math.Clamp(timeCallIndex, 0, _timeCalls.Count - 1);
        _attendanceName = _timeCalls[_timeCallIndex];
    }

    private void CopyAttendanceCall()
    {
        string message = AttendanceType.GetAttendanceCall(AttendanceName);
        Clip.Copy(message);
        CloseOkCommand.Execute();
        _attendanceSnapshot.TakeAttendanceSnapshot(AttendanceName, AttendanceType);
    }
}

public interface IReminderDialogViewModel : IDialogViewModel
{
    string AttendanceName { get; set; }

    AttendanceCallType AttendanceType { get; set; }

    DelegateCommand CopyAttendanceCallCommand { get; }

    bool IsTimeCall { get; }

    int ReminderInterval { get; set; }

    string ReminderText { get; set; }

    ICollection<string> TimeCalls { get; }

    int GetTimeCallIndex();

    void SetTimeCallIndex(int timeCallIndex);
}
