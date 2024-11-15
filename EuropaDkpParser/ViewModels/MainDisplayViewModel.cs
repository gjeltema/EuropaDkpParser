// -----------------------------------------------------------------------
// MainDisplayViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows;
using DkpParser;
using EuropaDkpParser.Utility;
using Microsoft.Win32;
using Prism.Commands;

internal sealed class MainDisplayViewModel : EuropaViewModelBase, IMainDisplayViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly DkpLogGenerator _logGenerator;
    private readonly ParsedFileGenerator _parsedFileGenerator;
    private readonly IDkpParserSettings _settings;
    private bool _ableToUpload;
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
        _logGenerator = new(settings, dialogFactory);
        _parsedFileGenerator = new(settings, dialogFactory);

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
        UploadGeneratedLogCommand = new DelegateCommand(UploadGeneratedLog);

        ResetTime();

        SetOutputDirectory();
        SetOutputFile();
        DebugOptionsEnabled = _settings.EnableDebugOptions;
        AbleToUpload = _settings.IsApiConfigured;
    }

    public bool AbleToUpload
    {
        get => _ableToUpload;
        set => SetProperty(ref _ableToUpload, value);
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
                    StartTimeText = endTime.AddHours(-6).ToString(Constants.TimePickerDisplayDateTimeFormat);
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

    public DelegateCommand UploadGeneratedLogCommand { get; }

    private async Task ExecuteParse(Func<DateTime, DateTime, Task> parseToExecute)
    {
        if (!_logGenerator.ValidateTimeSettings(StartTimeText, EndTimeText, out DateTime startTime, out DateTime endTime))
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
        => await _parsedFileGenerator.GetAllCommunicationAsync(startTime, endTime, GetOutputPath());

    private string GetOutputPath()
        => string.IsNullOrWhiteSpace(OutputDirectory) ? _logGenerator.GetUserProfilePath() : OutputDirectory;

    private async void GetRawLogFilesParse()
        => await ExecuteParse(GetRawLogFilesParseAsync);

    private async Task GetRawLogFilesParseAsync(DateTime startTime, DateTime endTime)
        => await _logGenerator.GetRawLogFilesParseAsync(startTime, endTime, GetOutputPath());

    private async void GetSearchTerm()
        => await ExecuteParse(GetSearchTermAsync);

    private async Task GetSearchTermAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.GetSearchTermAsync(startTime, endTime, SearchTermText, IsCaseSensitive, GetOutputPath());

    private void OpenFileArchiveDialog()
    {
        IFileArchiveDialogViewModel fileArchiveDialog = _dialogFactory.CreateFileArchiveDialogViewModel(_settings);
        if (fileArchiveDialog.ShowDialog() != true)
            return;

        fileArchiveDialog.UpdateSettings(_settings);
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
        if (settingsDialog.ShowDialog(600, 700) != true)
            return;

        settingsDialog.UpdateSettings(_settings);

        SetOutputDirectory();
        SetOutputFile();
        DebugOptionsEnabled = _settings.EnableDebugOptions;
        AbleToUpload = _settings.IsApiConfigured;
    }

    private async void ParseConversation()
        => await ExecuteParse(ParseConversationAsync);

    private async Task ParseConversationAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.ParseConversationAsync(startTime, endTime, ConversationPlayer, GetOutputPath());

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
        EndTimeText = currentTime.ToString(Constants.TimePickerDisplayDateTimeFormat);
        StartTimeText = currentTime.AddHours(-6).ToString(Constants.TimePickerDisplayDateTimeFormat);
        SetOutputFile();
    }

    private void SetOutputDirectory()
    {
        OutputDirectory = string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? _logGenerator.GetUserProfilePath() : _settings.OutputDirectory;
    }

    private void SetOutputFile()
    {
        string directory = string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? _logGenerator.GetUserProfilePath() : _settings.OutputDirectory;
        string outputFile = $"{Constants.GeneratedLogFileNamePrefix}{DateTime.Now:yyyyMMdd-HHmm}.txt";
        GeneratedFile = Path.Combine(directory, outputFile);
    }

    private async void StartLogParse()
        => await ExecuteParse(StartLogParseAsync);

    private async Task StartLogParseAsync(DateTime startTime, DateTime endTime)
    {
        DkpLogGenerationSessionSettings sessionSettings = new()
        {
            StartTime = startTime,
            EndTime = endTime,
            IsRawAnalyzerResultsChecked = IsRawAnalyzerResultsChecked,
            IsRawParseResultsChecked = IsRawParseResultsChecked,
            OutputAnalyzerErrors = OutputAnalyzerErrors,
            OutputDirectory = OutputDirectory,
            GeneratedFile = GeneratedFile,
            OutputPath = GetOutputPath()
        };

        await _logGenerator.StartLogParseAsync(sessionSettings);

        SetOutputFile();
    }

    private async void UploadGeneratedLog()
    {
        try
        {
            _performingParse = true;
            RefreshCommands();

            await UploadGeneratedLogAsync();
        }
        finally
        {
            _performingParse = false;
            RefreshCommands();
        }
    }

    private async Task UploadGeneratedLogAsync()
    {
        var fileDialog = new OpenFileDialog()
        {
            Title = "Select Generated Log File"
        };

        if (fileDialog.ShowDialog() != true)
            return;

        string generatedLogFile = fileDialog.FileName;
        if (!File.Exists(generatedLogFile))
        {
            MessageBox.Show($"{generatedLogFile} does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await _logGenerator.UploadGeneratedLogFile(generatedLogFile);
    }
}

public interface IMainDisplayViewModel : IEuropaViewModel
{
    bool AbleToUpload { get; set; }

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

    DelegateCommand UploadGeneratedLogCommand { get; }
}
