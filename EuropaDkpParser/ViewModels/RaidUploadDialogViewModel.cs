// -----------------------------------------------------------------------
// RaidUploadDialogViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Collections.ObjectModel;
using DkpParser;
using DkpParser.Parsers;
using DkpParser.Uploading;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
using Gjeltema.Logging;
using Prism.Commands;

internal sealed class RaidUploadDialogViewModel : DialogViewModelBase, IRaidUploadDialogViewModel
{
    private const string LogPrefix = $"[{nameof(RaidUploadDialogViewModel)}]";
    private readonly IDkpAdjustments _dkpAdjustments;
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private bool _showErrorMessages;
    private bool _uploadInProgress = false;
    private bool _uploadSelectedAttendances;

    public RaidUploadDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("RaidUploadDialogTitleText");
        _raidEntries = raidEntries;
        _settings = settings;

        _dkpAdjustments = new DkpAdjustmentProcessor(settings, CharacterInfoProvider.Instance);

        StatusMessage = Strings.GetString("BeginStatus");

        UploadButtonEnabled = true;

        BeginUploadCommand = new DelegateCommand(BeginUpload, CanBeginUpload);
        RemoveSelectedPlayerCommand = new DelegateCommand(RemoveSelectedPlayer, () => !_uploadInProgress && SelectedError != null && SelectedError.ErrorType != RaidUploadError.OverallError)
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

    public ICollection<UploadErrorDisplay> ErrorMessages { get; set => SetProperty(ref field, value); }

    public DelegateCommand RemoveSelectedAttendanceCommand { get; }

    public DelegateCommand RemoveSelectedPlayerCommand { get; }

    public ObservableCollection<AttendanceEntry> SelectedAttendances { get; set => SetProperty(ref field, value); }

    public AttendanceEntry SelectedAttendanceToAdd { get; set => SetProperty(ref field, value); }

    public AttendanceEntry SelectedAttendanceToRemove { get; set => SetProperty(ref field, value); }

    public UploadErrorDisplay SelectedError { get; set => SetProperty(ref field, value); }

    public bool ShowErrorMessages
    {
        get => _showErrorMessages && !UploadSelectedAttendances;
        set => SetProperty(ref _showErrorMessages, value);
    }

    public bool ShowProgress { get; set => SetProperty(ref field, value); }

    public string StatusMessage { get; set => SetProperty(ref field, value); }

    public bool TestRun { get; set => SetProperty(ref field, value); }

    public bool UploadButtonEnabled { get; set => SetProperty(ref field, value); }

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

        if (string.IsNullOrWhiteSpace(_settings.ApiMusterUrl) || string.IsNullOrWhiteSpace(_settings.ApiMusterToken))
        {
            MessageDialog.ShowDialog($"Muster DKP Server API settings are not configured. Ending upload.", "API Not Configured", 300, 400);
            return;
        }

        try
        {
            _uploadInProgress = true;
            RefreshCommands();

            UploadButtonEnabled = false;
            StatusMessage = Strings.GetString("UploadingStatus");
            ShowProgress = true;

            UploadRaidInfo raidsToUpload;
            if (UploadSelectedAttendances)
            {
                UploadSelectedAttendances = false;
                raidsToUpload = UploadRaidInfo.Create(SelectedAttendances, _raidEntries, _settings);
            }
            else
            {
                raidsToUpload = await UploadRaidInfo.CreateAsync(_dkpAdjustments, _raidEntries, _settings);
            }

            raidsToUpload.IsTestUpload = TestRun;

            RaidUploader raidUploader = new(_settings);

            MusterDkpRaidUploadResults musterResults = await raidUploader.UploadMusterRaidAsync(raidsToUpload);
            Log.Trace($"{LogPrefix} Muster upload results: {musterResults}");

            _raidEntries.DkpUploadErrors = musterResults.ItemBoughtErrors.Select(
                x => _raidEntries.DkpEntries.FirstOrDefault(z => z.Item == x.ItemName && z.CharacterName == x.BuyingCharacter))
                .ToList();

            ErrorMessages = SetDisplayedErrorMessages(musterResults);
            ShowErrorMessages = ErrorMessages.Count > 0;
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Unexpected error uploading: {e.ToLogMessage()}");
            ErrorMessages = [new UploadErrorDisplay { ErrorType = RaidUploadError.Unexpected, UnexpectedError = e }];
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

    private async Task<ICollection<string>> GetAllBiddingLogEntriesForDkpspentCallsAsync(ICollection<DkpEntry> dkpSpentEntriesRemoved)
    {
        List<string> logEntries = new(dkpSpentEntriesRemoved.Count * 12);
        foreach (DkpEntry entry in dkpSpentEntriesRemoved)
        {
            IEnumerable<string> logLines = await GetBiddingLogEntriesAsync(entry);
            logEntries.AddRange(logLines);
        }

        return logEntries;
    }

    private async Task<IEnumerable<string>> GetBiddingLogEntriesAsync(DkpEntry entry)
    {
        ITermParser termParser = new TermParser(_settings, entry.Item, true);
        ICollection<EqLogFile> logFiles = await Task.Run(() => termParser.GetEqLogFiles(entry.Timestamp.AddMinutes(-15), entry.Timestamp));

        ICollection<string> logEntries = (from log in logFiles
                                          from logEntry in log.LogEntries
                                          orderby logEntry.Timestamp
                                          select logEntry.FullLogLine)
                                          .ToList();

        if (logEntries.Count == 0)
            return [];

        string header = $"------------- {entry.Item} -------------";
        return [header, .. logEntries, Environment.NewLine];
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

    private async void RemoveSelectedPlayer()
        => await RemoveSelectedPlayerAsync();

    private async Task RemoveSelectedPlayerAsync()
    {
        if (SelectedError == null || SelectedError.ErrorType == RaidUploadError.OverallError)
            return;

        string characterName = SelectedError.CharacterName;

        ICollection<DkpEntry> dkpSpentEntriesRemoved = _raidEntries.RemoveCharacter(characterName);

        ICollection<string> bidLogEntries = await GetAllBiddingLogEntriesForDkpspentCallsAsync(dkpSpentEntriesRemoved);

        IEnumerable<string> displayLines;
        if (dkpSpentEntriesRemoved.Count > 0)
        {
            List<CharacterServerInfo> relatedCharacters = CharacterInfoProvider.Instance.GetAllRelatedCharactersForUser(characterName).ToList();
            string relatedCharsLine = relatedCharacters.Count > 0
                ? $"Related characters: {string.Join(", ", relatedCharacters.Select(x => x.CharacterName))}"
                : string.Empty;

            displayLines = [$"{characterName} was removed from all attendances, and had at least one item awarded in a SPENT call."
                , relatedCharsLine
                , "The following log entries were found relating to items this character was awarded:"
                , ..bidLogEntries];
        }
        else
        {
            displayLines = [$"{characterName} was removed from all attendances.", "No DKPSPENT entries were found for this player."];
        }

        string message = string.Join(Environment.NewLine, displayLines);
        string title = Strings.GetString("DkpspentEntriesRemovedDialogTitleText");
        MessageDialog.ShowDialog(message, title, 500, 700);
    }

    private ICollection<UploadErrorDisplay> SetDisplayedErrorMessages(MusterDkpRaidUploadResults uploadResults)
    {
        List<UploadErrorDisplay> uploadErrors = [];
        if (!string.IsNullOrWhiteSpace(uploadResults.Error))
        {
            uploadErrors.Add(new UploadErrorDisplay { ErrorType = RaidUploadError.OverallError, ErrorMessage = uploadResults.Error });
            return uploadErrors;
        }

        // Item errors need to be listed individually so that the bidding info can be searched for.
        // Missing chars only need to be displayed once per character.  So, do the item errors first and track what characters are missing for those,
        // then only add missing characters from attendance/precheck errors if they are not already being displayed.
        List<string> missingChars = [];

        foreach (MusterPreCheckError preCheckError in uploadResults.CharactersDontExistPreCheck)
        {
            if (!missingChars.Contains(preCheckError.CharacterName))
            {
                missingChars.Add(preCheckError.CharacterName);
                uploadErrors.Add(new UploadErrorDisplay { ErrorType = RaidUploadError.PreCheckError, CharacterName = preCheckError.CharacterName, ItemInfo = preCheckError.ItemBought });
            }
        }

        foreach (MusterItemBoughtError itemBoughtError in uploadResults.ItemBoughtErrors)
        {
            if (!missingChars.Contains(itemBoughtError.BuyingCharacter))
            {
                missingChars.Add(itemBoughtError.BuyingCharacter);
                uploadErrors.Add(new UploadErrorDisplay { ErrorType = RaidUploadError.ItemBoughtError, CharacterName = itemBoughtError.BuyingCharacter, ItemInfo = itemBoughtError.ItemInfo });
            }
        }

        foreach (MusterTimeTickError timeTickError in uploadResults.TimeTickErrors)
        {
            foreach (string missingCharacter in timeTickError.MembersNotIncluded)
            {
                if (!missingChars.Contains(missingCharacter))
                {
                    missingChars.Add(missingCharacter);
                    uploadErrors.Add(new UploadErrorDisplay { ErrorType = RaidUploadError.TimeTickError, CharacterName = missingCharacter, TimeTickError = timeTickError });
                }
            }
        }

        return uploadErrors;
    }
}

public enum RaidUploadError
{
    OverallError,
    PreCheckError,
    TimeTickError,
    ItemBoughtError,
    Unexpected
}

public sealed class UploadErrorDisplay
{
    private const string PlayerDelimiter = "**";

    public string CharacterName { get; init; }

    public string ErrorMessage { get; init; }

    public RaidUploadError ErrorType { get; init; }

    public DkpUploadInfo ItemInfo { get; init; }

    public MusterTimeTickError TimeTickError { get; init; }

    public Exception UnexpectedError { get; init; }

    public override sealed string ToString()
    {
        if (ErrorType == RaidUploadError.OverallError)
            return $"Error uploading: {ErrorMessage}";
        else if (ErrorType == RaidUploadError.PreCheckError)
            return $"{PlayerDelimiter}{CharacterName}{PlayerDelimiter} is missing from DKP server";
        else if (ErrorType == RaidUploadError.TimeTickError)
            return $"Time Tick error: {PlayerDelimiter}{CharacterName}{PlayerDelimiter} is missing from DKP server";
        else if (ErrorType == RaidUploadError.ItemBoughtError)
            return $"Item upload error: {PlayerDelimiter}{CharacterName}{PlayerDelimiter} is missing from DKP server";
        else
            return $"Unexpected error: {UnexpectedError.Message}";
    }
}

public interface IRaidUploadDialogViewModel : IDialogViewModel
{
    DelegateCommand AddSelectedAttendanceCommand { get; }

    ICollection<AttendanceEntry> AllAttendances { get; }

    DelegateCommand BeginUploadCommand { get; }

    ICollection<UploadErrorDisplay> ErrorMessages { get; }

    DelegateCommand RemoveSelectedAttendanceCommand { get; }

    DelegateCommand RemoveSelectedPlayerCommand { get; }

    ObservableCollection<AttendanceEntry> SelectedAttendances { get; }

    AttendanceEntry SelectedAttendanceToAdd { get; set; }

    AttendanceEntry SelectedAttendanceToRemove { get; set; }

    UploadErrorDisplay SelectedError { get; set; }

    bool ShowErrorMessages { get; }

    bool ShowProgress { get; set; }

    string StatusMessage { get; }

    bool TestRun { get; set; }

    bool UploadButtonEnabled { get; }

    bool UploadSelectedAttendances { get; set; }
}
