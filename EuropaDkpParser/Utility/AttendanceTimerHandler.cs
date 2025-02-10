// -----------------------------------------------------------------------
// AttendanceTimerHandler.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using System.Windows.Threading;
using DkpParser;
using EuropaDkpParser.Resources;
using EuropaDkpParser.ViewModels;

internal sealed class AttendanceTimerHandler
{
    private enum OverlayType : byte
    {
        None = 0,
        Time,
        Kill,
        Positioning
    }

    private readonly IDialogFactory _dialogFactory;
    private readonly IOverlayFactory _overlayFactory;
    private readonly Queue<PendingOverlay> _pendingOverlays = [];
    private readonly IDkpParserSettings _settings;
    private IAttendanceOverlayViewModel _attendanceOverlayViewModel;
    private DispatcherTimer _attendanceReminderTimer;
    private DispatcherTimer _killCallReminderTimer;
    private IOverlayPositioningViewModel _movingOverlay;
    private OverlayType _overlayTypeShowing;
    private bool _remindAttendances;
    private int _timeCallIndex = 0;
    private bool _useOverlayForAttendanceReminder;

    public AttendanceTimerHandler(IDkpParserSettings settings, IOverlayFactory overlayFactory, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _overlayFactory = overlayFactory;
        _dialogFactory = dialogFactory;
    }

    public bool RemindAttendances
    {
        get => _remindAttendances;
        set
        {
            if (_remindAttendances != value)
            {
                _remindAttendances = value;
                SetReminderForAttendances();
            }
        }
    }

    public bool UseAudioReminder { get; set; }

    public bool UseOverlayForAttendanceReminder
    {
        get => _useOverlayForAttendanceReminder;
        set
        {
            if (_useOverlayForAttendanceReminder != value)
            {
                _useOverlayForAttendanceReminder = value;
                if (_useOverlayForAttendanceReminder)
                {
                    InitializeAttendanceOverlay();
                }
            }
        }
    }

    public void CloseAll()
    {
        _pendingOverlays.Clear();

        _attendanceReminderTimer?.Stop();
        _attendanceReminderTimer = null;

        _killCallReminderTimer?.Stop();
        _killCallReminderTimer = null;

        _attendanceOverlayViewModel?.Close();
        _attendanceOverlayViewModel = null;

        _movingOverlay?.Close();
        _movingOverlay = null;
    }

    public void CloseOverlays()
    {
        _pendingOverlays.Clear();

        _attendanceOverlayViewModel?.HideOverlay();
        _movingOverlay?.HideOverlay();
    }

    public void RemindForKillAttendance(string bossName)
    {
        if (!RemindAttendances || string.IsNullOrEmpty(bossName))
            return;

        if (_overlayTypeShowing != OverlayType.None)
        {
            _pendingOverlays.Enqueue(new PendingOverlay { OverlayType = OverlayType.Kill, BossName = bossName });
            return;
        }

        DoAudioReminder();

        if (UseOverlayForAttendanceReminder)
        {
            _attendanceOverlayViewModel ??= _overlayFactory.CreateAttendanceOverlayViewModel(_settings);
            _attendanceOverlayViewModel.Show(bossName);
            _overlayTypeShowing = OverlayType.Kill;
        }
        else
        {
            string statusMessageFormat = Strings.GetString("KillAttendanceReminderMessageFormat");
            string statusMessage = string.Format(statusMessageFormat, bossName);

            IReminderDialogViewModel reminder = GetReminderDialog(statusMessage, AttendanceCallType.Kill);
            reminder.AttendanceName = bossName;

            bool doneWithReminder = reminder.ShowDialog() == true;
            if (!doneWithReminder)
            {
                TimeSpan userSpecifiedInterval = TimeSpan.FromMinutes(reminder.ReminderInterval);
                _killCallReminderTimer = new DispatcherTimer(
                    userSpecifiedInterval,
                    DispatcherPriority.Normal,
                    (s, e) => HandleKillCallReminder(bossName),
                    Dispatcher.CurrentDispatcher);
                _killCallReminderTimer.Start();
            }
        }
    }

    public void ShowTimeAttendanceReminder()
    {
        if (!RemindAttendances)
        {
            SetReminderForAttendances();
            return;
        }

        _attendanceReminderTimer.Stop();

        if (_overlayTypeShowing != OverlayType.None)
        {
            _pendingOverlays.Enqueue(new PendingOverlay { OverlayType = OverlayType.Time });
            return;
        }

        DoAudioReminder();
        TimeSpan nextInterval;

        if (UseOverlayForAttendanceReminder)
        {
            _attendanceOverlayViewModel ??= _overlayFactory.CreateAttendanceOverlayViewModel(_settings);
            _attendanceOverlayViewModel.Show(_timeCallIndex);
            _overlayTypeShowing = OverlayType.Time;

            nextInterval = GetAttendanceReminderInterval();
        }
        else
        {
            IReminderDialogViewModel reminder = GetReminderDialog(Strings.GetString("TimeAttendanceReminderMessage"), AttendanceCallType.Time);
            reminder.SetTimeCallIndex(_timeCallIndex);

            bool ok = reminder.ShowDialog() == true;
            nextInterval = ok ? GetAttendanceReminderInterval() : TimeSpan.FromMinutes(reminder.ReminderInterval);
            int selectedTimeCallIndex = reminder.GetTimeCallIndex();
            _timeCallIndex = ok ? selectedTimeCallIndex + 1 : selectedTimeCallIndex - 1;
        }

        _attendanceReminderTimer.Interval = nextInterval;
        _attendanceReminderTimer.Start();
    }

    public bool TogglePositioningOverlay(bool showPositionOverlay)
    {
        if (showPositionOverlay)
        {
            if (_overlayTypeShowing == OverlayType.Positioning)
            {
                return true;
            }
            else if (_overlayTypeShowing == OverlayType.None)
            {
                _movingOverlay ??= _overlayFactory.CreateOverlayPositioningViewModel(_settings);
                _movingOverlay.ShowToMove();
                _overlayTypeShowing = OverlayType.Positioning;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (_overlayTypeShowing == OverlayType.Positioning)
            {
                int newXPosition = _movingOverlay.XPos;
                int newYPosition = _movingOverlay.YPos;
                if (_settings.OverlayLocationX != newXPosition || _settings.OverlayLocationY != newYPosition)
                {
                    _settings.OverlayLocationX = newXPosition;
                    _settings.OverlayLocationY = newYPosition;
                    _settings.SaveSettings();
                }

                _movingOverlay.Close();
                _movingOverlay = null;

                _overlayTypeShowing = OverlayType.None;
                HandleOverlayHide();
            }
            return true;
        }
    }

    private void DoAudioReminder()
    {
        if (UseAudioReminder)
            System.Media.SystemSounds.Hand.Play();
    }

    private TimeSpan GetAttendanceReminderInterval()
    {
        int minutes = DateTime.Now.Minute;
        if (minutes < 30)
            return TimeSpan.FromMinutes(30 - minutes);
        else
            return TimeSpan.FromMinutes(60 - minutes);
    }

    private IReminderDialogViewModel GetReminderDialog(string reminderDisplayText, AttendanceCallType callType)
    {
        IReminderDialogViewModel reminderDialogViewModel = _dialogFactory.CreateReminderDialogViewModel();
        reminderDialogViewModel.ReminderText = reminderDisplayText;
        reminderDialogViewModel.AttendanceType = callType;

        return reminderDialogViewModel;
    }

    private void HandleAttendanceReminderTimer(object sender, EventArgs e)
        => ShowTimeAttendanceReminder();

    private void HandleKillCallReminder(string bossName)
    {
        _killCallReminderTimer?.Stop();
        _killCallReminderTimer = null;

        RemindForKillAttendance(bossName);
    }

    private void HandleOverlayHide()
    {
        if (_overlayTypeShowing == OverlayType.Time)
        {
            _timeCallIndex = (_attendanceOverlayViewModel?.GetTimeCallIndex() ?? _timeCallIndex) + 1;
        }

        _overlayTypeShowing = OverlayType.None;
        if (_pendingOverlays.Count <= 0)
            return;

        PendingOverlay nextOverlay = _pendingOverlays.Dequeue();
        if (nextOverlay != null)
        {
            if (nextOverlay.OverlayType == OverlayType.Time)
                ShowTimeAttendanceReminder();
            else
                RemindForKillAttendance(nextOverlay.BossName);
        }
    }

    private void InitializeAttendanceOverlay()
    {
        if (_attendanceOverlayViewModel == null)
        {
            _attendanceOverlayViewModel = _overlayFactory.CreateAttendanceOverlayViewModel(_settings);
            _attendanceOverlayViewModel.SetHideHandler(HandleOverlayHide);
        }
    }

    private void SetReminderForAttendances()
    {
        if (RemindAttendances)
        {
            TimeSpan interval = GetAttendanceReminderInterval();
            _attendanceReminderTimer = new DispatcherTimer(
                interval,
                DispatcherPriority.Normal,
                HandleAttendanceReminderTimer,
                Dispatcher.CurrentDispatcher);
        }
        else
        {
            _attendanceReminderTimer?.Stop();
            _attendanceReminderTimer = null;
        }
    }

    private sealed class PendingOverlay
    {
        public string BossName { get; set; }

        public OverlayType OverlayType { get; set; }
    }
}
