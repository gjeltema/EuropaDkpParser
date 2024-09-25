// -----------------------------------------------------------------------
// RaidUploadDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using DkpParser;
using DkpParser.Parsers;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal sealed class RaidUploadDialogViewModel : DialogViewModelBase, IRaidUploadDialogViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly ParsedFileGenerator _parsedFileGenerator;
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<UploadErrorDisplay> _errorMessages;
    private bool _outputDebugInfo;
    private AttendanceEntry _selectedAttendance;
    private ObservableCollection<AttendanceEntry> _selectedAttendances;
    private AttendanceEntry _selectedAttendanceToRemove;
    private UploadErrorDisplay _selectedError;
    private bool _showErrorMessages;
    private bool _showProgress;
    private string _statusMessage;
    private bool _uploadButtonEnabled;
    private bool _uploadInProgress = false;
    private bool _uploadSelectedAttendances;

    public RaidUploadDialogViewModel(IDialogViewFactory viewFactory, IDialogFactory dialogFactory, RaidEntries raidEntries, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("RaidUploadDialogTitleText");
        _dialogFactory = dialogFactory;
        _raidEntries = raidEntries;
        _settings = settings;

        StatusMessage = Strings.GetString("BeginStatus");

        UploadButtonEnabled = true;

        BeginUploadCommand = new DelegateCommand(BeginUpload, CanBeginUpload);
        RemoveSelectedPlayerCommand = new DelegateCommand(RemoveSelectedPlayer, () => !_uploadInProgress && SelectedError != null && SelectedError.FailedCharacterIdRetrieval != null)
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

    public ICollection<UploadErrorDisplay> ErrorMessages
    {
        get => _errorMessages;
        set => SetProperty(ref _errorMessages, value);
    }

    public bool OutputDebugInfo
    {
        get => _outputDebugInfo;
        set => SetProperty(ref _outputDebugInfo, value);
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

    public UploadErrorDisplay SelectedError
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

            IUploadDebugInfo debugInfo = OutputDebugInfo ? new UploadDebugInfo() : new NullUploadDebugInfo();
            RaidUploader server = new(_settings, debugInfo);
            RaidUploadResults uploadResults = await server.UploadRaid(entriesToUpload);

            ErrorMessages = SetDisplayedErrorMessages(uploadResults).ToList();
            ShowErrorMessages = ErrorMessages.Count > 0;

            await WriteDebugInfo(debugInfo);
        }
        catch (Exception e)
        {
            ErrorMessages = [new UploadErrorDisplay { UnexpectedError = e }];
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

    private async Task<bool> CreateFile(string fileToWriteTo, IEnumerable<string> fileContents)
    {
        try
        {
            await Task.Run(() => File.AppendAllLines(fileToWriteTo, fileContents));
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Strings.GetString("DebugFileGenerationErrorMessage") + ex.ToString(), Strings.GetString("DebugFileGenerationError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private async Task<ICollection<string>> GetAllBiddingLogEntriesForDkpspentCalls(ICollection<DkpEntry> dkpSpentEntriesRemoved)
    {
        List<string> logEntries = new(dkpSpentEntriesRemoved.Count * 12);
        foreach (DkpEntry entry in dkpSpentEntriesRemoved)
        {
            IEnumerable<string> logLines = await GetBiddingLogEntries(entry);
            logEntries.AddRange(logLines);
        }

        return logEntries;
    }

    private async Task<IEnumerable<string>> GetBiddingLogEntries(DkpEntry entry)
    {
        ITermParser termParser = new TermParser(_settings, entry.Item, true);
        ICollection<EqLogFile> logFiles = await Task.Run(() => termParser.GetEqLogFiles(entry.Timestamp.AddMinutes(-15), entry.Timestamp));

        ICollection<string> logEntries = (from log in logFiles
                                          from logEntry in log.LogEntries
                                          orderby logEntry.Timestamp
                                          select logEntry.LogLine)
                                          .ToList();

        if (logEntries.Count == 0)
            return [];

        string header = $"------------- {entry.Item} -------------";
        return [header, .. logEntries, Environment.NewLine];
    }

    private RaidEntries GetSelectedAttendancesToUpload()
    {
        RaidEntries entriesToUpload = new();

        entriesToUpload.AttendanceEntries = SelectedAttendances;
        entriesToUpload.AllCharactersInRaid = _raidEntries.AllCharactersInRaid;

        return entriesToUpload;
    }

    private void RefreshCommands()
    {
        BeginUploadCommand.RaiseCanExecuteChanged();
        RemoveSelectedPlayerCommand.RaiseCanExecuteChanged();
    }

    private ICollection<DkpEntry> RemovePlayerDkpspentEntries(string playerName)
    {
        ICollection<DkpEntry> dkpSpentsToRemove = _raidEntries.DkpEntries.Where(x => x.PlayerName == playerName).ToList();
        foreach (DkpEntry dkpToRemove in dkpSpentsToRemove)
        {
            _raidEntries.DkpEntries.Remove(dkpToRemove);
            _raidEntries.RemovedDkpEntries.Add(dkpToRemove);
        }

        return dkpSpentsToRemove;
    }

    private void RemovePlayerFromAttendances(string playerName)
    {
        PlayerCharacter playerChar = _raidEntries.AllCharactersInRaid.FirstOrDefault(x => x.CharacterName == playerName);
        if (playerChar == null)
            return;

        IEnumerable<AttendanceEntry> attendancesToRemoveFrom = _raidEntries.AttendanceEntries.Where(x => x.Characters.Contains(playerChar));
        foreach (AttendanceEntry attendance in attendancesToRemoveFrom)
        {
            attendance.Characters.Remove(playerChar);
        }

        _raidEntries.AllCharactersInRaid.Remove(playerChar);
    }

    private void RemoveSelectedAttendance()
    {
        if (SelectedAttendanceToRemove == null)
            return;

        SelectedAttendances.Remove(SelectedAttendanceToRemove);
    }

    private async void RemoveSelectedPlayer()
        => await RemoveSelectedPlayerAsync();

    private async Task RemoveSelectedPlayerAsync()
    {
        if (SelectedError == null || SelectedError.FailedCharacterIdRetrieval == null)
            return;

        string playerName = SelectedError.FailedCharacterIdRetrieval.PlayerName;

        RemovePlayerFromAttendances(playerName);
        ICollection<DkpEntry> dkpSpentEntriesRemoved = RemovePlayerDkpspentEntries(playerName);

        if (dkpSpentEntriesRemoved.Count == 0)
            return;

        ICollection<string> bidLogEntries = await GetAllBiddingLogEntriesForDkpspentCalls(dkpSpentEntriesRemoved);
        if (bidLogEntries.Count == 0)
            return;

        ISimpleMultilineDisplayDialogViewModel displayDialog = _dialogFactory.CreateSimpleMultilineDisplayDialogViewModel();
        displayDialog.DisplayLines = string.Join(Environment.NewLine, bidLogEntries);
        displayDialog.ShowDialog();
    }

    private IEnumerable<UploadErrorDisplay> SetDisplayedErrorMessages(RaidUploadResults uploadResults)
    {
        if (uploadResults.EventIdCallFailure != null)
            yield return new UploadErrorDisplay { EventIdCallFailure = uploadResults.EventIdCallFailure };

        foreach (CharacterIdFailure characterIdFail in uploadResults.FailedCharacterIdRetrievals)
            yield return new UploadErrorDisplay { FailedCharacterIdRetrieval = characterIdFail };

        foreach (EventIdNotFoundFailure eventIdNotFound in uploadResults.EventIdNotFoundErrors)
            yield return new UploadErrorDisplay { EventIdNotFound = eventIdNotFound };

        if (uploadResults.AttendanceError != null)
            yield return new UploadErrorDisplay { AttendanceError = uploadResults.AttendanceError };

        if (uploadResults.DkpFailure != null)
            yield return new UploadErrorDisplay { DkpFailure = uploadResults.DkpFailure };
    }

    private async Task WriteDebugInfo(IUploadDebugInfo debugInfo)
    {
        if (!OutputDebugInfo)
            return;

        string outputFile = $"{Constants.UploadDebugInfoFileNamePrefix}{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string fullOutputPath = Path.Combine(_settings.OutputDirectory, outputFile);
        await CreateFile(fullOutputPath, debugInfo.GetFullDebugInfo());
    }
}

public sealed class UploadErrorDisplay
{
    private const string PlayerDelimiter = "**";

    public AttendanceUploadFailure AttendanceError { get; init; }

    public DkpUploadFailure DkpFailure { get; init; }

    public Exception EventIdCallFailure { get; init; }

    public EventIdNotFoundFailure EventIdNotFound { get; init; }

    public CharacterIdFailure FailedCharacterIdRetrieval { get; init; }

    public Exception UnexpectedError { get; init; }

    public override sealed string ToString()
    {
        if (EventIdCallFailure != null)
            return $"Failed to get listing of all event IDs from DKP server: {EventIdCallFailure.Message}";
        else if (FailedCharacterIdRetrieval != null)
            return $"Failed to get character ID for {PlayerDelimiter}{FailedCharacterIdRetrieval.PlayerName}{PlayerDelimiter}, likely character does not exist on DKP server";
        else if (EventIdNotFound != null)
            return GetEventIdFailureMessage(EventIdNotFound);
        else if (AttendanceError != null)
            return $"Failed to upload attendance call {AttendanceError.Attendance.CallName}: {AttendanceError.Error.Message}";
        else if (DkpFailure != null)
            return $"Failed to upload DKP spend call for {DkpFailure.Dkp.PlayerName} for item {DkpFailure.Dkp.Item}: {DkpFailure.Error.Message}";
        else if (UnexpectedError != null)
            return $"Unexpected error encountered when uploading: {UnexpectedError}";
        else
            return "Error not found";
    }

    private string GetEventIdFailureMessage(EventIdNotFoundFailure eventIdNotFound)
    {
        const string Prefix = "Unable to retrieve event ID for ";

        switch (eventIdNotFound.ErrorType)
        {
            case EventIdNotFoundFailure.EventIdError.ZoneNotConfigured:
                return $"{Prefix} {eventIdNotFound.ZoneName}.  Update the RaidValues.txt to add, correct, or alias this zone.";
            case EventIdNotFoundFailure.EventIdError.ZoneNotFoundOnDkpServer:
                return eventIdNotFound.ZoneName == eventIdNotFound.ZoneAlias
                    ? $"{Prefix} {eventIdNotFound.ZoneName}"
                    : $"{Prefix} {eventIdNotFound.ZoneName} with alias {eventIdNotFound.ZoneAlias}";
            case EventIdNotFoundFailure.EventIdError.InvalidZoneValue:
                return $"{Prefix} {eventIdNotFound.ZoneName}, value returned from DKP server was: {eventIdNotFound.IdValue}";
            default:
                return "Error not found.";
        };
    }
}

public interface IRaidUploadDialogViewModel : IDialogViewModel
{
    DelegateCommand AddSelectedAttendanceCommand { get; }

    ICollection<AttendanceEntry> AllAttendances { get; }

    DelegateCommand BeginUploadCommand { get; }

    ICollection<UploadErrorDisplay> ErrorMessages { get; }

    bool OutputDebugInfo { get; set; }

    DelegateCommand RemoveSelectedAttendanceCommand { get; }

    DelegateCommand RemoveSelectedPlayerCommand { get; }

    ObservableCollection<AttendanceEntry> SelectedAttendances { get; }

    AttendanceEntry SelectedAttendanceToAdd { get; set; }

    AttendanceEntry SelectedAttendanceToRemove { get; set; }

    UploadErrorDisplay SelectedError { get; set; }

    bool ShowErrorMessages { get; }

    bool ShowProgress { get; set; }

    string StatusMessage { get; }

    bool UploadButtonEnabled { get; }

    bool UploadSelectedAttendances { get; set; }
}
