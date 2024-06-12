// -----------------------------------------------------------------------
// RaidUploadDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Collections.ObjectModel;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class RaidUploadDialogViewModel : DialogViewModelBase, IRaidUploadDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<string> _errorMessages;
    private AttendanceEntry _selectedAttendance;
    private ObservableCollection<AttendanceEntry> _selectedAttendances;
    private AttendanceEntry _selectedAttendanceToRemove;
    private string _selectedError;
    private bool _showErrorMessages;
    private bool _showProgress;
    private string _statusMessage;
    private bool _uploadButtonEnabled;
    private bool _uploadInProgress = false;
    private bool _uploadSelectedAttendances;

    public RaidUploadDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("RaidUploadDialogTitleText");

        _raidEntries = raidEntries;
        _settings = settings;

        StatusMessage = Strings.GetString("BeginStatus");

        UploadButtonEnabled = true;

        BeginUploadCommand = new DelegateCommand(BeginUpload, CanBeginUpload);
        RemoveSelectedPlayerCommand = new DelegateCommand(RemoveSelectedPlayer, () => !_uploadInProgress && !string.IsNullOrWhiteSpace(SelectedError))
            .ObservesProperty(() => SelectedError);
        AddSelectedAttendanceCommand = new DelegateCommand(AddSelectedAttendance, () => SelectedAttendanceToAdd != null)
            .ObservesProperty(() => SelectedAttendanceToAdd);
        RemoveSelectedAttendanceCommand = new DelegateCommand(RemoveSelectedAttendance, () => SelectedAttendanceToRemove != null)
            .ObservesProperty(() => SelectedAttendanceToRemove);

        SelectedAttendances = new ObservableCollection<AttendanceEntry>();
        AllAttendances = raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp).ToList();
    }

    public DelegateCommand AddSelectedAttendanceCommand { get; }

    public ICollection<AttendanceEntry> AllAttendances { get; }

    public DelegateCommand BeginUploadCommand { get; }

    public ICollection<string> ErrorMessages
    {
        get => _errorMessages;
        set => SetProperty(ref _errorMessages, value);
    }

    public DelegateCommand RemoveSelectedAttendanceCommand { get; }

    public DelegateCommand RemoveSelectedPlayerCommand { get; }

    public ObservableCollection<AttendanceEntry> SelectedAttendances
    {
        get => _selectedAttendances;
        set => SetProperty(ref _selectedAttendances, value);
    }

    public AttendanceEntry SelectedAttendanceToAdd
    {
        get => _selectedAttendance;
        set => SetProperty(ref _selectedAttendance, value);
    }

    public AttendanceEntry SelectedAttendanceToRemove
    {
        get => _selectedAttendanceToRemove;
        set => SetProperty(ref _selectedAttendanceToRemove, value);
    }

    public string SelectedError
    {
        get => _selectedError;
        set => SetProperty(ref _selectedError, value);
    }

    public bool ShowErrorMessages
    {
        get => _showErrorMessages && !UploadSelectedAttendances;
        set => SetProperty(ref _showErrorMessages, value);
    }

    public bool ShowProgress
    {
        get => _showProgress;
        set => SetProperty(ref _showProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool UploadButtonEnabled
    {
        get => _uploadButtonEnabled;
        set => SetProperty(ref _uploadButtonEnabled, value);
    }

    public bool UploadSelectedAttendances
    {
        get => _uploadSelectedAttendances;
        set
        {
            SetProperty(ref _uploadSelectedAttendances, value);
            RaisePropertyChanged(nameof(ShowErrorMessages));
        }
    }

    private void AddSelectedAttendance()
    {
        if (SelectedAttendanceToAdd == null)
            return;

        AttendanceEntry nextAttendance = SelectedAttendances.Where(x => SelectedAttendanceToAdd.Timestamp < x.Timestamp).MinBy(x => x.Timestamp);
        int indexOfNext = SelectedAttendances.IndexOf(nextAttendance);
        if (indexOfNext < 0)
            SelectedAttendances.Add(SelectedAttendanceToAdd);
        else
            SelectedAttendances.Insert(indexOfNext, SelectedAttendanceToAdd);
    }

    private async void BeginUpload()
        => await BeginUploadAsync();

    private async Task BeginUploadAsync()
    {
        ShowErrorMessages = false;

        try
        {
            _uploadInProgress = true;
            RefreshCommands();

            UploadButtonEnabled = false;
            StatusMessage = Strings.GetString("UploadingStatus");
            ShowProgress = true;

            RaidEntries entriesToUpload = _raidEntries;
            if (UploadSelectedAttendances)
            {
                UploadSelectedAttendances = false;
                entriesToUpload = GetSelectedAttendancesToUpload();
            }

            RaidUploader server = new(_settings);
            RaidUploadResults uploadResults = await server.UploadRaid(entriesToUpload);

            ErrorMessages = uploadResults.GetErrorMessages().ToList();
            ShowErrorMessages = ErrorMessages.Count > 0;
        }
        catch (Exception e)
        {
            ErrorMessages = [$"Unexpected error encountered when uploading: {e}"];
            ShowErrorMessages = true;
            StatusMessage = Strings.GetString("FailureStatus");
        }
        finally
        {
            _uploadInProgress = false;
            RefreshCommands();

            ShowProgress = false;
            UploadButtonEnabled = !ShowErrorMessages;
            StatusMessage = ShowErrorMessages ? Strings.GetString("FailureStatus") : Strings.GetString("SuccessStatus");
        }
    }

    private bool CanBeginUpload()
    {
        if (_uploadInProgress)
            return false;

        if (UploadSelectedAttendances)
        {
            return SelectedAttendances.Any();
        }

        return true;
    }

    private RaidEntries GetSelectedAttendancesToUpload()
    {
        RaidEntries entriesToUpload = new();

        entriesToUpload.AttendanceEntries = SelectedAttendances;
        entriesToUpload.AllPlayersInRaid = _raidEntries.AllPlayersInRaid;

        return entriesToUpload;
    }

    private void RefreshCommands()
    {
        BeginUploadCommand.RaiseCanExecuteChanged();
        RemoveSelectedPlayerCommand.RaiseCanExecuteChanged();
    }

    private void RemoveSelectedAttendance()
    {
        if (SelectedAttendanceToRemove == null)
            return;

        SelectedAttendances.Remove(SelectedAttendanceToRemove);
    }

    private void RemoveSelectedPlayer()
    {
        if (string.IsNullOrWhiteSpace(SelectedError))
            return;

        string[] errorMessageParts = SelectedError.Split(RaidUploadResults.PlayerDelimiter);
        if (errorMessageParts.Length < 3)
            return;

        string playerName = errorMessageParts[1];
        PlayerCharacter playerChar = _raidEntries.AllPlayersInRaid.FirstOrDefault(x => x.PlayerName == playerName);
        if (playerChar == null)
            return;

        IEnumerable<AttendanceEntry> attendancesToRemoveFrom = _raidEntries.AttendanceEntries.Where(x => x.Players.Contains(playerChar));
        foreach (AttendanceEntry attendance in attendancesToRemoveFrom)
        {
            attendance.Players.Remove(playerChar);
        }

        List<DkpEntry> dkpSpentsToRemove = _raidEntries.DkpEntries.Where(x => x.PlayerName == playerName).ToList();
        _raidEntries.RemovedDkpEntries = dkpSpentsToRemove;
        foreach (DkpEntry dkpToRemove in dkpSpentsToRemove)
        {
            _raidEntries.DkpEntries.Remove(dkpToRemove);
        }

        _raidEntries.AllPlayersInRaid.Remove(playerChar);
    }
}

public interface IRaidUploadDialogViewModel : IDialogViewModel
{
    DelegateCommand AddSelectedAttendanceCommand { get; }

    ICollection<AttendanceEntry> AllAttendances { get; }

    DelegateCommand BeginUploadCommand { get; }

    ICollection<string> ErrorMessages { get; }

    DelegateCommand RemoveSelectedAttendanceCommand { get; }

    DelegateCommand RemoveSelectedPlayerCommand { get; }

    ObservableCollection<AttendanceEntry> SelectedAttendances { get; }

    AttendanceEntry SelectedAttendanceToAdd { get; set; }

    AttendanceEntry SelectedAttendanceToRemove { get; set; }

    string SelectedError { get; set; }

    bool ShowErrorMessages { get; }

    bool ShowProgress { get; set; }

    string StatusMessage { get; }

    bool UploadButtonEnabled { get; }

    bool UploadSelectedAttendances { get; set; }
}
