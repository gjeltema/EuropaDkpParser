// -----------------------------------------------------------------------
// MainDisplayViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class MainDisplayViewModel : EuropaViewModelBase, IMainDisplayViewModel
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;
    private string _conversationPlayer;
    private string _endTimeText;
    private string _generatedFile;
    private bool _isOutputRawParseResultsChecked;
    private bool _isRawAnalyzerResultsChecked;
    private string _outputDirectory;
    private bool _performingParse = false;
    private string _startTimeText;

    internal MainDisplayViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
        OpenSettingsDialogCommand = new DelegateCommand(OpenSettingsDialog);
        StartLogParseCommand = new DelegateCommand(StartLogParse, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(GeneratedFile))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => GeneratedFile);
        GetRawLogFileCommand = new DelegateCommand(GetRawLogFilesParse, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(GeneratedFile))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => GeneratedFile);
        ResetTimeCommand = new DelegateCommand(ResetTime);
        GetConversationCommand = new DelegateCommand(ParseConversation, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(GeneratedFile))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => GeneratedFile);

        ResetTime();

        SetOutputDirectory();
        SetOutputFile();
    }

    public string ConversationPlayer
    {
        get => _conversationPlayer;
        set => SetProperty(ref _conversationPlayer, value);
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

    public DelegateCommand GetConversationCommand { get; }

    public DelegateCommand GetRawLogFileCommand { get; }

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

    public DelegateCommand OpenSettingsDialogCommand { get; }

    public string OutputDirectory
    {
        get => _outputDirectory;
        private set => SetProperty(ref _outputDirectory, value);
    }

    public DelegateCommand ResetTimeCommand { get; }

    public DelegateCommand StartLogParseCommand { get; }

    public string StartTimeText
    {
        get => _startTimeText;
        set => SetProperty(ref _startTimeText, value);
    }

    private async void GetRawLogFilesParse()
       => await GetRawLogFilesParseAsync();

    private async Task GetRawLogFilesParseAsync()
    {
        if (!ValidateTimeSettings(out DateTime startTime, out DateTime endTime))
            return;

        try
        {
            _performingParse = true;
            RefreshCommands();

            IFullEqLogParser fullLogParser = new FullEqLogParser(_settings);
            ICollection<EqLogFile> logFiles = await Task.Run(() => fullLogParser.GetEqLogFiles(startTime, endTime));

            string directory = string.IsNullOrWhiteSpace(_settings.EqDirectory) ? Directory.GetCurrentDirectory() : _settings.EqDirectory;
            if (!string.IsNullOrWhiteSpace(GeneratedFile))
                directory = Path.GetDirectoryName(GeneratedFile);

            string fullLogOutputFile = $"FullLogOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
            string fullLogOutputFullPath = Path.Combine(directory, fullLogOutputFile);
            foreach (EqLogFile logFile in logFiles)
            {
                File.AppendAllLines(fullLogOutputFullPath, logFile.GetAllLogLines());
            }

            ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialog(fullLogOutputFullPath, Strings.GetString("SuccessfulCompleteMessage"));
            completedDialog.ShowDialog();
        }
        finally
        {
            _performingParse = false;
            RefreshCommands();
        }
    }

    private void OpenSettingsDialog()
    {
        ILogSelectionViewModel settingsDialog = _dialogFactory.CreateSettingsViewDialog(_settings);
        if (settingsDialog.ShowDialog() != true)
            return;

        _settings.EqDirectory = settingsDialog.EqDirectory;
        _settings.SelectedLogFiles = settingsDialog.SelectedCharacterLogFiles;
        _settings.OutputDirectory = settingsDialog.OutputDirectory;
        _settings.SaveSettings();

        SetOutputDirectory();
        SetOutputFile();
    }

    private void OutputRawAnalyzerResults(RaidEntries raidEntries)
    {
        string directory = string.IsNullOrWhiteSpace(_settings.EqDirectory) ? Directory.GetCurrentDirectory() : _settings.EqDirectory;
        if (!string.IsNullOrWhiteSpace(GeneratedFile))
            directory = Path.GetDirectoryName(GeneratedFile);

        string rawAnalyzerOutputFile = $"RawAnalyzerOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawAnalyzerOutputFullPath = Path.Combine(directory, rawAnalyzerOutputFile);
        File.AppendAllLines(rawAnalyzerOutputFullPath, raidEntries.GetAllEntries());
    }

    private void OutputRawParseResults(LogParseResults results)
    {
        string directory = string.IsNullOrWhiteSpace(_settings.EqDirectory) ? Directory.GetCurrentDirectory() : _settings.EqDirectory;
        if (!string.IsNullOrWhiteSpace(GeneratedFile))
            directory = Path.GetDirectoryName(GeneratedFile);

        string rawParseOutputFile = $"RawParseOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawParseOutputFullPath = Path.Combine(directory, rawParseOutputFile);
        foreach (EqLogFile logFile in results.EqLogFiles)
        {
            File.AppendAllLines(rawParseOutputFullPath, logFile.GetAllLogLines());
        }
    }

    private async void ParseConversation()
        => await ParseConversationAsync();

    private async Task ParseConversationAsync()
    {
        if (!ValidateTimeSettings(out DateTime startTime, out DateTime endTime))
            return;

        try
        {
            _performingParse = true;
            RefreshCommands();

            IConversationParser conversationParser = new ConversationParser(_settings, ConversationPlayer);
            ICollection<EqLogFile> logFiles = await Task.Run(() => conversationParser.GetEqLogFiles(startTime, endTime));

            string directory = string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? Directory.GetCurrentDirectory() : _settings.OutputDirectory;
            string conversationOutputFile = $"ConversationOutput-{ConversationPlayer}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
            string conversationOutputFullPath = Path.Combine(directory, conversationOutputFile);
            bool anyConversationFound = false;
            foreach (EqLogFile logFile in logFiles)
            {
                if (logFile.LogEntries.Count > 0)
                {
                    File.AppendAllLines(conversationOutputFullPath, logFile.GetAllLogLines());
                    anyConversationFound = true;
                }
            }

            if (!anyConversationFound)
            {
                MessageBox.Show(Strings.GetString("NoConversationFound"), Strings.GetString("NoConversationFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialog(conversationOutputFullPath, Strings.GetString("SuccessfulCompleteMessage"));
            completedDialog.ShowDialog();
        }
        finally
        {
            _performingParse = false;
            RefreshCommands();
        }
    }

    private void RefreshCommands()
    {
        StartLogParseCommand.RaiseCanExecuteChanged();
        GetRawLogFileCommand.RaiseCanExecuteChanged();
        GetConversationCommand.RaiseCanExecuteChanged();
    }

    private void ResetTime()
    {
        DateTime currentTime = DateTime.Now;
        _endTimeText = currentTime.ToString(DateTimeFormat);
        _startTimeText = currentTime.AddHours(-6).ToString(DateTimeFormat);
        SetOutputFile();
    }

    private void SetOutputDirectory()
    {
        OutputDirectory = string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? Directory.GetCurrentDirectory() : _settings.OutputDirectory;
    }

    private void SetOutputFile()
    {
        string directory = string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? Directory.GetCurrentDirectory() : _settings.OutputDirectory;
        string outputFile = $"RaidLog-{DateTime.Now:yyyyMMdd-HHmm}.txt";
        GeneratedFile = Path.Combine(directory, outputFile);
    }

    private async void StartLogParse()
        => await StartLogParseAsync();

    private async Task StartLogParseAsync()
    {
        if (!ValidateTimeSettings(out DateTime startTime, out DateTime endTime))
            return;

        try
        {
            _performingParse = true;
            RefreshCommands();

            ILogParseProcessor parseProcessor = new LogParseProcessor(_settings);
            LogParseResults results = await Task.Run(() => parseProcessor.ParseLogs(startTime, endTime));

            if (IsRawParseResultsChecked)
            {
                await Task.Run(() => OutputRawParseResults(results));
            }

            ILogEntryAnalyzer logEntryAnalyzer = new LogEntryAnalyzer(_settings);
            RaidEntries raidEntries = await Task.Run(() => logEntryAnalyzer.AnalyzeRaidLogEntries(results));

            if (IsRawAnalyzerResultsChecked)
            {
                await Task.Run(() => OutputRawAnalyzerResults(raidEntries));
            }

            if (raidEntries.AttendanceEntries.Any(x => x.PossibleError != PossibleError.None))
            {
                IAttendanceErrorDisplayDialogViewModel attendanceErrorDialog = _dialogFactory.CreateAttendanceErrorDisplayDialog(_settings, raidEntries);
                if (attendanceErrorDialog.ShowDialog() == false)
                    return;
            }

            if (raidEntries.DkpEntries.Any(x => x.PossibleError != PossibleError.None))
            {
                IDkpErrorDisplayDialogViewModel dkpErrorDialog = _dialogFactory.CreateDkpErrorDisplayDialogViewModel(_settings, raidEntries);
                if (dkpErrorDialog.ShowDialog() == false)
                    return;
            }

            IFinalSummaryDialogViewModel finalSummaryDialog = _dialogFactory.CreateFinalSummaryDialog(raidEntries);
            if (finalSummaryDialog.ShowDialog() == false)
                return;

            IOutputGenerator generator = new FileOutputGenerator(GeneratedFile);
            await generator.GenerateOutput(raidEntries);

            ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialog(GeneratedFile, Strings.GetString("SuccessfulCompleteMessage"));
            completedDialog.ShowDialog();
        }
        finally
        {
            _performingParse = false;
            RefreshCommands();
            SetOutputFile();
        }
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

    string EndTimeText { get; set; }

    string GeneratedFile { get; set; }

    DelegateCommand GetConversationCommand { get; }

    DelegateCommand GetRawLogFileCommand { get; }

    bool IsRawAnalyzerResultsChecked { get; set; }

    bool IsRawParseResultsChecked { get; set; }

    DelegateCommand OpenSettingsDialogCommand { get; }

    string OutputDirectory { get; }

    DelegateCommand ResetTimeCommand { get; }

    DelegateCommand StartLogParseCommand { get; }

    string StartTimeText { get; set; }
}
