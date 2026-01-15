// -----------------------------------------------------------------------
// AttendanceTimerHandler.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using System.Windows.Threading;
using DkpParser;
using EuropaDkpParser.Resources;
using EuropaDkpParser.ViewModels;
using Gjeltema.Logging;

internal sealed class AttendanceTimerHandler
{
    private enum OverlayType : byte
    {
        None = 0,
        Time,
        Kill,
        Positioning
    }

    private const string LogPrefix = $"[{nameof(AttendanceTimerHandler)}]";
    private readonly IAttendanceSnapshot _attendanceSnapshot;
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

    public AttendanceTimerHandler(IDkpParserSettings settings, IAttendanceSnapshot attendanceSnapshot, IOverlayFactory overlayFactory, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _attendanceSnapshot = attendanceSnapshot;
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
        Log.Debug($"{LogPrefix} {nameof(CloseAll)} called.");

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

        Log.Debug($"{LogPrefix} {nameof(RemindForKillAttendance)} called for {bossName}.  {nameof(RemindAttendances)} is {RemindAttendances}.");

        if (_overlayTypeShowing != OverlayType.None)
        {
            Log.Debug($"{LogPrefix} Overlay already showing. Adding to queue.");
            _pendingOverlays.Enqueue(new PendingOverlay { OverlayType = OverlayType.Kill, BossName = bossName });
            return;
        }

        DoAudioReminder();

        if (UseOverlayForAttendanceReminder)
        {
            Log.Info($"{LogPrefix} Showing reminder overlay for boss name {bossName}.");
            InitializeAttendanceOverlay();
            _attendanceOverlayViewModel.Show(bossName);
            _overlayTypeShowing = OverlayType.Kill;
        }
        else
        {
            string statusMessageFormat = Strings.GetString("KillAttendanceReminderMessageFormat");
            string statusMessage = string.Format(statusMessageFormat, bossName);

            Log.Info($"{LogPrefix} Showing reminder dialog for boss name {bossName}.");

            IReminderDialogViewModel reminder = GetReminderDialog(statusMessage, AttendanceCallType.Kill);
            reminder.AttendanceName = bossName;

            bool closeOk = reminder.ShowDialog() == true;
            if (reminder.Remind)
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
        Log.Debug($"{LogPrefix} {nameof(ShowTimeAttendanceReminder)} called.  {nameof(RemindAttendances)} is {RemindAttendances}.");

        if (!RemindAttendances)
        {
            SetReminderForAttendances();
            return;
        }

        _attendanceReminderTimer.Stop();

        if (_overlayTypeShowing != OverlayType.None)
        {
            Log.Info($"{LogPrefix} Overlay already showing. Adding time reminder for time call index {_timeCallIndex} to queue.");
            _pendingOverlays.Enqueue(new PendingOverlay { OverlayType = OverlayType.Time });
            return;
        }

        DoAudioReminder();
        TimeSpan nextInterval;

        if (UseOverlayForAttendanceReminder)
        {
            Log.Info($"{LogPrefix} Showing reminder overlay for time call index {_timeCallIndex}.");
            InitializeAttendanceOverlay();
            _attendanceOverlayViewModel.Show(_timeCallIndex);
            _overlayTypeShowing = OverlayType.Time;

            nextInterval = GetAttendanceReminderInterval();
        }
        else
        {
            IReminderDialogViewModel reminder = GetReminderDialog(Strings.GetString("TimeAttendanceReminderMessage"), AttendanceCallType.Time);
            reminder.SetTimeCallIndex(_timeCallIndex);

            Log.Info($"{LogPrefix} Showing reminder dialog for time call index {_timeCallIndex}.");

            bool ok = reminder.ShowDialog() == true;
            nextInterval = reminder.Remind ? TimeSpan.FromMinutes(reminder.ReminderInterval) : GetAttendanceReminderInterval();
            int selectedTimeCallIndex = reminder.GetTimeCallIndex();
            _timeCallIndex = ok ? selectedTimeCallIndex + 1 : selectedTimeCallIndex - 1;
        }

        _attendanceReminderTimer.Interval = nextInterval;
        _attendanceReminderTimer.Start();
    }

    public bool TogglePositioningOverlay(bool showPositionOverlay)
    {
        Log.Debug($"{LogPrefix} {nameof(TogglePositioningOverlay)} called.");

        if (showPositionOverlay)
        {
            Log.Debug($"{LogPrefix} Attempting to show overlay.");

            if (_overlayTypeShowing == OverlayType.Positioning)
            {
                Log.Debug($"{LogPrefix} Positioning overlay already showing.");
                return true;
            }
            else if (_overlayTypeShowing == OverlayType.None)
            {
                Log.Debug($"{LogPrefix} Displaying positioning overlay.");
                _movingOverlay ??= _overlayFactory.CreateOverlayPositioningViewModel(_settings);
                _movingOverlay.ShowToMove();
                _overlayTypeShowing = OverlayType.Positioning;
                return true;
            }
            else
            {
                Log.Debug($"{LogPrefix} Another overlay is already displayed. Ending.");
                return false;
            }
        }
        else
        {
            Log.Debug($"{LogPrefix} Attempting to close positioning overlay.");
            if (_overlayTypeShowing == OverlayType.Positioning)
            {
                int newXPosition = _movingOverlay.XPos;
                int newYPosition = _movingOverlay.YPos;
                if (_settings.OverlayLocationX != newXPosition || _settings.OverlayLocationY != newYPosition)
                {
                    _settings.OverlayLocationX = newXPosition;
                    _settings.OverlayLocationY = newYPosition;
                    _settings.SaveSettings();

                    // Have to recreate the VM/window as Windows wont accept the position changes if they shift the window
                    // to another monitor.
                    _attendanceOverlayViewModel = null;
                }

                _movingOverlay.Close();
                _movingOverlay = null;

                _overlayTypeShowing = OverlayType.None;
                HandleOverlayHide();

                Log.Debug($"{LogPrefix} Closed positioning overlay.");
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
        IReminderDialogViewModel reminderDialogViewModel = _dialogFactory.CreateReminderDialogViewModel(_attendanceSnapshot);
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
            _attendanceOverlayViewModel = _overlayFactory.CreateAttendanceOverlayViewModel(_settings, _attendanceSnapshot);
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
