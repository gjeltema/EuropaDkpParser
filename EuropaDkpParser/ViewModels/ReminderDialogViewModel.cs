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
    private int _reminderInterval;
    private string _reminderText;
    private string _selectedTimeMarker;

    public ReminderDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("ReminderDialogTitleText");
        Height = 220;
        Width = 400;

        ReminderInterval = 3;

        CopyAttendanceCallCommand = new DelegateCommand(CopyAttendanceCall);

        TimeCalls = ["First Call", "Second Call", "Third Call", "Fourth Call", "Fifth Call", "Sixth Call"
            , "Seventh Call", "Eighth Call", "Ninth Call", "Tenth Call", "Eleventh Call", "Twelfth Call"];

        AttendanceName = TimeCalls.First();
    }

    public string AttendanceName
    {
        get => _selectedTimeMarker;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            SetProperty(ref _selectedTimeMarker, value);
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

    public ICollection<string> TimeCalls { get; }

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
}
