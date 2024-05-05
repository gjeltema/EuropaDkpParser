// -----------------------------------------------------------------------
// MainDisplayViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows;
using DkpParser;
using DkpParser.Parsers;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class MainDisplayViewModel : EuropaViewModelBase, IMainDisplayViewModel
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;
    private string _conversationPlayer;
    private bool _debugOptionsEnabled;
    private string _endTimeText;
    private string _generatedFile;
    private bool _isCaseSensitive;
    private bool _isOutputRawParseResultsChecked;
    private bool _isRawAnalyzerResultsChecked;
    private bool _outputAnalyzerErrors;
    private string _outputDirectory;
    private bool _performingParse = false;
    private string _searchTermText;
    private string _startTimeText;

    internal MainDisplayViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
        OpenSettingsDialogCommand = new DelegateCommand(OpenSettingsDialog);
        StartLogParseCommand = new DelegateCommand(StartLogParse, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(GeneratedFile))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => GeneratedFile);
        GetRawLogFileCommand = new DelegateCommand(GetRawLogFilesParse, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => OutputDirectory);
        ResetTimeCommand = new DelegateCommand(ResetTime);
        GetConversationCommand = new DelegateCommand(ParseConversation, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(ConversationPlayer) && !string.IsNullOrWhiteSpace(OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => OutputDirectory).ObservesProperty(() => ConversationPlayer);
        GetAllCommunicationCommand = new DelegateCommand(GetAllCommunication, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => OutputDirectory);
        GetSearchTermCommand = new DelegateCommand(GetSearchTerm, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(SearchTermText) && !string.IsNullOrWhiteSpace(OutputDirectory))
           .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => OutputDirectory).ObservesProperty(() => SearchTermText);
        OpenFileArchiveDialogCommand = new DelegateCommand(OpenFileArchiveDialog);
        OpenGeneralParserCommand = new DelegateCommand(OpenGeneralParser);

        ResetTime();

        SetOutputDirectory();
        SetOutputFile();
        DebugOptionsEnabled = _settings.EnableDebugOptions;
    }

    public string ConversationPlayer
    {
        get => _conversationPlayer;
        set => SetProperty(ref _conversationPlayer, value);
    }

    public bool DebugOptionsEnabled
    {
        get => _debugOptionsEnabled;
        set => SetProperty(ref _debugOptionsEnabled, value);
    }

    public string EndTimeText
    {
        get => _endTimeText;
        set
        {
            if (SetProperty(ref _endTimeText, value))
            {
                if (DateTime.TryParse(value, out DateTime endTime))
                {
                    StartTimeText = endTime.AddHours(-6).ToString(DateTimeFormat);
                }
            }
        }
    }

    public string GeneratedFile
    {
        get => _generatedFile;
        set => SetProperty(ref _generatedFile, value);
    }

    public DelegateCommand GetAllCommunicationCommand { get; }

    public DelegateCommand GetConversationCommand { get; }

    public DelegateCommand GetRawLogFileCommand { get; }

    public DelegateCommand GetSearchTermCommand { get; }

    public bool IsCaseSensitive
    {
        get => _isCaseSensitive;
        set => SetProperty(ref _isCaseSensitive, value);
    }

    public bool IsRawAnalyzerResultsChecked
    {
        get => _isRawAnalyzerResultsChecked;
        set => SetProperty(ref _isRawAnalyzerResultsChecked, value);
    }

    public bool IsRawParseResultsChecked
    {
        get => _isOutputRawParseResultsChecked;
        set => SetProperty(ref _isOutputRawParseResultsChecked, value);
    }

    public DelegateCommand OpenFileArchiveDialogCommand { get; }

    public DelegateCommand OpenGeneralParserCommand { get; }

    public DelegateCommand OpenSettingsDialogCommand { get; }

    public bool OutputAnalyzerErrors
    {
        get => _outputAnalyzerErrors;
        set => SetProperty(ref _outputAnalyzerErrors, value);
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        private set => SetProperty(ref _outputDirectory, value);
    }

    public DelegateCommand ResetTimeCommand { get; }

    public string SearchTermText
    {
        get => _searchTermText;
        set => SetProperty(ref _searchTermText, value);
    }

    public DelegateCommand StartLogParseCommand { get; }

    public string StartTimeText
    {
        get => _startTimeText;
        set => SetProperty(ref _startTimeText, value);
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
            MessageBox.Show(Strings.GetString("LogGenerationErrorMessage") + ex.ToString(), Strings.GetString("LogGenerationError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private async Task ExecuteParse(Func<DateTime, DateTime, Task> parseToExecute)
    {
        if (!ValidateTimeSettings(out DateTime startTime, out DateTime endTime))
            return;

        try
        {
            _performingParse = true;
            RefreshCommands();

            await parseToExecute(startTime, endTime);
        }
        finally
        {
            _performingParse = false;
            RefreshCommands();
        }
    }

    private async void GetAllCommunication()
        => await ExecuteParse(GetAllCommunicationAsync);

    private async Task GetAllCommunicationAsync(DateTime startTime, DateTime endTime)
    {
        IAllCommunicationParser communicationParser = new AllCommunicationParser(_settings);
        ICollection<EqLogFile> logFiles = await Task.Run(() => communicationParser.GetEqLogFiles(startTime, endTime));

        string directory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetUserProfilePath() : OutputDirectory;
        string communicationOutputFile = $"{Constants.CommunicationFileNamePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string communicationOutputFullPath = Path.Combine(directory, communicationOutputFile);
        bool anyCommunicationFound = false;
        foreach (EqLogFile logFile in logFiles)
        {
            if (logFile.LogEntries.Count > 0)
            {
                await CreateFile(communicationOutputFullPath, logFile.GetAllLogLines());
                anyCommunicationFound = true;
            }
        }

        if (!anyCommunicationFound)
        {
            MessageBox.Show(Strings.GetString("NoCommunicationFound"), Strings.GetString("NoCommunicationFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(communicationOutputFullPath);
        completedDialog.ShowDialog();
    }

    private int GetIntValue(string inputValue)
    {
        if (int.TryParse(inputValue, out int parsedValue))
        {
            return parsedValue;
        }
        return 0;
    }

    private async void GetRawLogFilesParse()
        => await ExecuteParse(GetRawLogFilesParseAsync);

    private async Task GetRawLogFilesParseAsync(DateTime startTime, DateTime endTime)
    {
        IFullEqLogParser fullLogParser = new FullEqLogParser(_settings);
        ICollection<EqLogFile> logFiles = await Task.Run(() => fullLogParser.GetEqLogFiles(startTime, endTime));

        string directory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetUserProfilePath() : OutputDirectory;

        string fullLogOutputFile = $"{Constants.FullGeneratedLogFileNamePrefix}{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string fullLogOutputFullPath = Path.Combine(directory, fullLogOutputFile);

        foreach (EqLogFile logFile in logFiles)
        {
            await CreateFile(fullLogOutputFullPath, logFile.GetAllLogLines());
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(fullLogOutputFullPath);
        completedDialog.ShowDialog();
    }

    private async void GetSearchTerm()
        => await ExecuteParse(GetSearchTermAsync);

    private async Task GetSearchTermAsync(DateTime startTime, DateTime endTime)
    {
        ITermParser termParser = new TermParser(_settings, SearchTermText, IsCaseSensitive);
        ICollection<EqLogFile> logFiles = await Task.Run(() => termParser.GetEqLogFiles(startTime, endTime));

        string directory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetUserProfilePath() : OutputDirectory;
        string searchTermOutputFile = $"{Constants.SearchTermFileNamePrefix}{SearchTermText}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string searchTermOutputFullPath = Path.Combine(directory, searchTermOutputFile);
        bool anySearchTermFound = false;
        foreach (EqLogFile logFile in logFiles)
        {
            if (logFile.LogEntries.Count > 0)
            {
                await CreateFile(searchTermOutputFullPath, logFile.GetAllLogLines());
                anySearchTermFound = true;
            }
        }

        if (!anySearchTermFound)
        {
            MessageBox.Show(Strings.GetString("NoSearchTermFound"), Strings.GetString("NoSearchTermFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(searchTermOutputFullPath);
        completedDialog.ShowDialog();
    }

    private string GetUserProfilePath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "EuropaDKP");

    private void OpenFileArchiveDialog()
    {
        IFileArchiveDialogViewModel fileArchiveDialog = _dialogFactory.CreateFileArchiveDialogViewModel(_settings);
        if (fileArchiveDialog.ShowDialog() != true)
            return;

        _settings.ArchiveAllEqLogFiles = fileArchiveDialog.IsAllLogsArchived;
        _settings.EqLogFileArchiveDirectory = fileArchiveDialog.EqLogArchiveDirectory;
        _settings.EqLogFileAgeToArchiveInDays = GetIntValue(fileArchiveDialog.EqLogArchiveFileAge);
        _settings.EqLogFileSizeToArchiveInMBs = GetIntValue(fileArchiveDialog.EqLogArchiveFileSize);
        _settings.EqLogFilesToArchive = fileArchiveDialog.SelectedEqLogFiles;
        _settings.GeneratedLogFilesAgeToArchiveInDays = GetIntValue(fileArchiveDialog.GeneratedLogsArchiveFileAge);
        _settings.GeneratedLogFilesArchiveDirectory = fileArchiveDialog.GeneratedLogsArchiveDirectory;

        _settings.SaveSettings();
    }

    private void OpenGeneralParser()
    {
        IGeneralEqLogParserDialogViewModel parser = _dialogFactory.CreateGeneralEqParserDialogViewModel(_dialogFactory, _settings);
        if (parser.ShowDialog() != true)
            return;
    }

    private void OpenSettingsDialog()
    {
        ILogSelectionViewModel settingsDialog = _dialogFactory.CreateSettingsViewDialogViewModel(_settings);
        if (settingsDialog.ShowDialog() != true)
            return;

        _settings.EqDirectory = settingsDialog.EqDirectory;
        _settings.SelectedLogFiles = settingsDialog.SelectedCharacterLogFiles;
        _settings.OutputDirectory = settingsDialog.OutputDirectory;
        _settings.EnableDebugOptions = settingsDialog.IsDebugOptionsEnabled;
        _settings.ApiUrl = settingsDialog.ApiUrl;
        _settings.ApiReadToken = settingsDialog.ApiReadToken;
        _settings.ApiWriteToken = settingsDialog.ApiWriteToken;
        _settings.SaveSettings();

        DebugOptionsEnabled = _settings.EnableDebugOptions;
        SetOutputDirectory();
        SetOutputFile();
        DebugOptionsEnabled = _settings.EnableDebugOptions;
    }

    private async Task OutputAnalyzerErrorsToFile(RaidEntries raidEntries)
    {
        string directory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetUserProfilePath() : OutputDirectory;

        string rawAnalyzerOutputFile = $"AnalyzerErrors-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawAnalyzerOutputFullPath = Path.Combine(directory, rawAnalyzerOutputFile);
        await CreateFile(rawAnalyzerOutputFullPath, raidEntries.AnalysisErrors);
    }

    private async Task OutputRawAnalyzerResults(RaidEntries raidEntries)
    {
        string directory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetUserProfilePath() : OutputDirectory;

        string rawAnalyzerOutputFile = $"RawAnalyzerOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawAnalyzerOutputFullPath = Path.Combine(directory, rawAnalyzerOutputFile);
        await CreateFile(rawAnalyzerOutputFullPath, raidEntries.GetAllEntries());
    }

    private async Task OutputRawParseResults(LogParseResults results)
    {
        string directory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetUserProfilePath() : OutputDirectory;

        string rawParseOutputFile = $"RawParseOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawParseOutputFullPath = Path.Combine(directory, rawParseOutputFile);
        foreach (EqLogFile logFile in results.EqLogFiles)
        {
            await CreateFile(rawParseOutputFullPath, logFile.GetAllLogLines());
        }
    }

    private async Task<RaidEntries> ParseAndAnalyzeLogFiles(DateTime startTime, DateTime endTime)
    {
        try
        {
            IDkpLogParseProcessor parseProcessor = new DkpLogParseProcessor(_settings);
            LogParseResults results = await Task.Run(() => parseProcessor.ParseLogs(startTime, endTime));

            if (IsRawParseResultsChecked)
            {
                await OutputRawParseResults(results);
            }

            ILogEntryAnalyzer logEntryAnalyzer = new LogEntryAnalyzer(_settings);
            return await Task.Run(() => logEntryAnalyzer.AnalyzeRaidLogEntries(results));
        }
        catch (EuropaDkpParserException e)
        {
            string errorMessage = $"{e.Message}{Environment.NewLine}{e.LogLine}";
            MessageBox.Show(errorMessage, Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private async void ParseConversation()
        => await ExecuteParse(ParseConversationAsync);

    private async Task ParseConversationAsync(DateTime startTime, DateTime endTime)
    {
        IConversationParser conversationParser = new ConversationParser(_settings, ConversationPlayer);
        ICollection<EqLogFile> logFiles = await Task.Run(() => conversationParser.GetEqLogFiles(startTime, endTime));

        string directory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetUserProfilePath() : OutputDirectory;
        string conversationPlayers = string.Join("-", ConversationPlayer.Split(';'));
        string conversationOutputFile = $"{Constants.ConversationFileNamePrefix}{conversationPlayers}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string conversationOutputFullPath = Path.Combine(directory, conversationOutputFile);
        bool anyConversationFound = false;
        foreach (EqLogFile logFile in logFiles)
        {
            if (logFile.LogEntries.Count > 0)
            {
                await CreateFile(conversationOutputFullPath, logFile.GetAllLogLines());
                anyConversationFound = true;
            }
        }

        if (!anyConversationFound)
        {
            MessageBox.Show(Strings.GetString("NoConversationFound"), Strings.GetString("NoConversationFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(conversationOutputFullPath);
        completedDialog.ShowDialog();
    }

    private void RefreshCommands()
    {
        StartLogParseCommand.RaiseCanExecuteChanged();
        GetRawLogFileCommand.RaiseCanExecuteChanged();
        GetConversationCommand.RaiseCanExecuteChanged();
        GetSearchTermCommand.RaiseCanExecuteChanged();
    }

    private void ResetTime()
    {
        DateTime currentTime = DateTime.Now;
        EndTimeText = currentTime.ToString(DateTimeFormat);
        StartTimeText = currentTime.AddHours(-6).ToString(DateTimeFormat);
        SetOutputFile();
    }

    private void SetOutputDirectory()
    {
        OutputDirectory = string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? GetUserProfilePath() : _settings.OutputDirectory;
    }

    private void SetOutputFile()
    {
        string directory = string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? GetUserProfilePath() : _settings.OutputDirectory;
        string outputFile = $"{Constants.GeneratedLogFileNamePrefix}{DateTime.Now:yyyyMMdd-HHmm}.txt";
        GeneratedFile = Path.Combine(directory, outputFile);
    }

    private async void StartLogParse()
        => await ExecuteParse(StartLogParseAsync);

    private async Task StartLogParseAsync(DateTime startTime, DateTime endTime)
    {
        RaidEntries raidEntries = await ParseAndAnalyzeLogFiles(startTime, endTime);

        if (raidEntries == null)
            return;

        if (IsRawAnalyzerResultsChecked)
        {
            await OutputRawAnalyzerResults(raidEntries);
        }

        if (OutputAnalyzerErrors && raidEntries.AnalysisErrors.Count > 0)
        {
            await OutputAnalyzerErrorsToFile(raidEntries);
        }

        if (raidEntries.AttendanceEntries.Any(x => x.PossibleError != PossibleError.None))
        {
            IAttendanceErrorDisplayDialogViewModel attendanceErrorDialog = _dialogFactory.CreateAttendanceErrorDisplayDialogViewModel(_settings, raidEntries);
            if (attendanceErrorDialog.ShowDialog() == false)
                return;
        }

        if (raidEntries.DkpEntries.Any(x => x.PossibleError != PossibleError.None))
        {
            IDkpErrorDisplayDialogViewModel dkpErrorDialog = _dialogFactory.CreateDkpErrorDisplayDialogViewModel(_settings, raidEntries);
            if (dkpErrorDialog.ShowDialog() == false)
                return;
        }

        if (raidEntries.PossibleLinkdeads.Count > 0)
        {
            IPossibleLinkdeadErrorDialogViewModel possibleLDDialog = _dialogFactory.CreatePossibleLinkdeadErrorDialogViewModel(raidEntries);
            possibleLDDialog.ShowDialog();
        }

        IBonusDkpAnalyzer bonusDkp = new BonusDkpAnalyzer(_settings);
        bonusDkp.AddBonusAttendance(raidEntries);

        IFinalSummaryDialogViewModel finalSummaryDialog = _dialogFactory.CreateFinalSummaryDialogViewModel(_dialogFactory, raidEntries, _settings.IsApiConfigured);
        if (finalSummaryDialog.ShowDialog() == false)
            return;

        IOutputGenerator generator = new FileOutputGenerator();
        ICollection<string> fileContents = generator.GenerateOutput(raidEntries);
        bool success = await CreateFile(GeneratedFile, fileContents);
        if (!success)
            return;

        if (finalSummaryDialog.UploadToServer && _settings.IsApiConfigured)
        {
            IRaidUploadDialogViewModel raidUpload = _dialogFactory.CreateRaidUploadDialogViewModel(raidEntries, _settings);
            raidUpload.ShowDialog();
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(GeneratedFile);
        completedDialog.DkpSpentEntries = string.Join(Environment.NewLine, raidEntries.GetAllDkpspentEntries());
        completedDialog.ShowDialog();
    }

    private bool ValidateTimeSettings(out DateTime startTime, out DateTime endTime)
    {
        endTime = DateTime.MinValue;
        if (!DateTime.TryParse(StartTimeText, out startTime))
        {
            MessageBox.Show(Strings.GetString("StartTimeErrorMessage"), Strings.GetString("StartTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (!DateTime.TryParse(EndTimeText, out endTime))
        {
            MessageBox.Show(Strings.GetString("EndTimeErrorMessage"), Strings.GetString("EndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (startTime > endTime)
        {
            MessageBox.Show(Strings.GetString("StartEndTimeErrorMessage"), Strings.GetString("StartEndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }
}

public interface IMainDisplayViewModel : IEuropaViewModel
{
    string ConversationPlayer { get; set; }

    bool DebugOptionsEnabled { get; }

    string EndTimeText { get; set; }

    string GeneratedFile { get; set; }

    DelegateCommand GetAllCommunicationCommand { get; }

    DelegateCommand GetConversationCommand { get; }

    DelegateCommand GetRawLogFileCommand { get; }

    DelegateCommand GetSearchTermCommand { get; }

    bool IsCaseSensitive { get; set; }

    bool IsRawAnalyzerResultsChecked { get; set; }

    bool IsRawParseResultsChecked { get; set; }

    DelegateCommand OpenFileArchiveDialogCommand { get; }

    DelegateCommand OpenGeneralParserCommand { get; }

    DelegateCommand OpenSettingsDialogCommand { get; }

    bool OutputAnalyzerErrors { get; set; }

    string OutputDirectory { get; }

    DelegateCommand ResetTimeCommand { get; }

    string SearchTermText { get; set; }

    DelegateCommand StartLogParseCommand { get; }

    string StartTimeText { get; set; }
}
