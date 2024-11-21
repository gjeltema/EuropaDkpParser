// -----------------------------------------------------------------------
// ReminderDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal sealed class ReminderDialogViewModel : DialogViewModelBase, IReminderDialogViewModel
{
    private readonly List<string> _timeCalls;
    private static int timeCallIndex = -1;
    private string _attendanceName;
    private int _reminderInterval;
    private string _reminderText;

    public ReminderDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("ReminderDialogTitleText");
        Height = 220;
        Width = 400;

        ReminderInterval = 3;

        CopyAttendanceCallCommand = new DelegateCommand(CopyAttendanceCall);

        _timeCalls = ["First Call", "Second Call", "Third Call", "Fourth Call", "Fifth Call", "Sixth Call"
            , "Seventh Call", "Eighth Call", "Ninth Call", "Tenth Call", "Eleventh Call", "Twelfth Call"];

        if (timeCallIndex > _timeCalls.Count || timeCallIndex < 0)
            _attendanceName = _timeCalls.First();
        else
            _attendanceName = _timeCalls[timeCallIndex];
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
                    timeCallIndex = indexOfSelection;
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

    public void IncrementToNextTimeCall()
    {
        if (timeCallIndex >= TimeCalls.Count - 1)
            timeCallIndex = 0;
        else
            timeCallIndex++;

        AttendanceName = _timeCalls[timeCallIndex];
    }

    private void CopyAttendanceCall()
    {
        string message = AttendanceType.GetAttendanceCall(AttendanceName);
        Clip.Copy(message);
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

    void IncrementToNextTimeCall();
}
