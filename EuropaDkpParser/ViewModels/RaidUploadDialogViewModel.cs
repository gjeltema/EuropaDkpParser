// -----------------------------------------------------------------------
// RaidUploadDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class RaidUploadDialogViewModel : DialogViewModelBase, IRaidUploadDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<string> _errorMessages;
    private bool _hasErrorMessages;
    private string _selectedError;
    private bool _showProgress;
    private string _statusMessage;
    private bool _uploadButtonEnabled;

    public RaidUploadDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("RaidUploadDialogTitleText");

        _raidEntries = raidEntries;
        _settings = settings;

        StatusMessage = Strings.GetString("BeginStatus");

        UploadButtonEnabled = true;

        BeginUploadCommand = new DelegateCommand(BeginUpload);
        RemoveSelectedPlayerCommand = new DelegateCommand(RemoveSelectedPlayer, () => !string.IsNullOrWhiteSpace(SelectedError))
            .ObservesProperty(() => SelectedError);
    }

    public DelegateCommand BeginUploadCommand { get; }

    public ICollection<string> ErrorMessages
    {
        get => _errorMessages;
        set => SetProperty(ref _errorMessages, value);
    }

    public bool HasErrorMessages
    {
        get => _hasErrorMessages;
        set => SetProperty(ref _hasErrorMessages, value);
    }

    public DelegateCommand RemoveSelectedPlayerCommand { get; }

    public string SelectedError
    {
        get => _selectedError;
        set => SetProperty(ref _selectedError, value);
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

    private async void BeginUpload()
        => await BeginUploadAsync();

    private async Task BeginUploadAsync()
    {
        HasErrorMessages = false;

        try
        {
            UploadButtonEnabled = false;
            StatusMessage = Strings.GetString("UploadingStatus");
            ShowProgress = true;

            RaidUploader server = new(_settings);
            RaidUploadResults uploadResults = await server.UploadRaid(_raidEntries);

            ErrorMessages = uploadResults.GetErrorMessages().ToList();
            HasErrorMessages = ErrorMessages.Count > 0;
        }
        catch (Exception e)
        {
            ErrorMessages = [$"Unexpected error encountered when uploading: {e}"];
            HasErrorMessages = true;
            StatusMessage = Strings.GetString("FailureStatus");
        }
        finally
        {
            ShowProgress = false;
            UploadButtonEnabled = !HasErrorMessages;
            StatusMessage = HasErrorMessages ? Strings.GetString("FailureStatus") : Strings.GetString("SuccessStatus");
        }
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
    DelegateCommand BeginUploadCommand { get; }

    ICollection<string> ErrorMessages { get; }

    bool HasErrorMessages { get; }

    DelegateCommand RemoveSelectedPlayerCommand { get; }

    string SelectedError { get; set; }

    bool ShowProgress { get; set; }

    string StatusMessage { get; }

    bool UploadButtonEnabled { get; }
}
