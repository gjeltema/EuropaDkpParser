// -----------------------------------------------------------------------
// AttendanceOverlayViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal sealed class AttendanceOverlayViewModel : OverlayViewModelBase, IAttendanceOverlayViewModel
{
    private readonly IDkpParserSettings _settings;
    private readonly List<string> _timeCalls = ["First Call", "Second Call", "Third Call", "Fourth Call", "Fifth Call", "Sixth Call"
            , "Seventh Call", "Eighth Call", "Ninth Call", "Tenth Call", "Eleventh Call", "Twelfth Call"];
    private string _attendanceName;
    private string _displayMessage;
    private bool _isTimeCall;
    private int _timeCallIndex = -1;

    public AttendanceOverlayViewModel(IOverlayViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        _settings = settings;

        XPos = _settings.OverlayLocationX;
        YPos = _settings.OverlayLocationY;

        DisplayFontSize = _settings.OverlayFontSize;
        DisplayColor = _settings.OverlayFontColor;

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

    public string DisplayColor { get; }

    public int DisplayFontSize { get; }

    public string DisplayMessage
    {
        get => _displayMessage;
        private set => SetProperty(ref _displayMessage, value);
    }

    public bool IsTimeCall
    {
        get => _isTimeCall;
        private set => SetProperty(ref _isTimeCall, value);
    }

    public ICollection<string> TimeCalls
        => _timeCalls;

    public int GetTimeCallIndex()
        => _timeCallIndex;

    public void Show(int timeCallIndex)
    {
        _timeCallIndex = timeCallIndex;

        AttendanceType = AttendanceCallType.Time;
        IsTimeCall = true;

        XPos = _settings.OverlayLocationX;
        YPos = _settings.OverlayLocationY;

        if (_timeCallIndex > _timeCalls.Count || _timeCallIndex < 0)
            _timeCallIndex = 0;

        AttendanceName = _timeCalls[_timeCallIndex];

        DisplayMessage = $"{AttendanceType} attendance:";

        CreateAndShowOverlay();
    }

    public void Show(string bossName)
    {
        AttendanceType = AttendanceCallType.Kill;
        IsTimeCall = false;
        AttendanceName = bossName;
        DisplayMessage = $"{AttendanceType} attendance: {AttendanceName}";

        CreateAndShowOverlay();
    }

    private void CopyAttendanceCall()
    {
        string message = AttendanceType.GetAttendanceCall(AttendanceName);
        Clip.Copy(message);
        HideOverlay();
    }
}

public interface IAttendanceOverlayViewModel : IOverlayViewModel
{
    string AttendanceName { get; set; }

    AttendanceCallType AttendanceType { get; set; }

    DelegateCommand CopyAttendanceCallCommand { get; }

    string DisplayColor { get; }

    int DisplayFontSize { get; }

    string DisplayMessage { get; }

    bool IsTimeCall { get; }

    ICollection<string> TimeCalls { get; }

    int GetTimeCallIndex();

    void Show(int timeCallIndex);

    void Show(string bossName);
}
