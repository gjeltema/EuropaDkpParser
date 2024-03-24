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
    private string _endTimeText;
    private bool _isOutputRawParseResultsChecked;
    private bool _isRawAnalyzerResultsChecked;
    private string _outputFile;
    private bool _performingParse = false;
    private string _startTimeText;

    internal MainDisplayViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
        OpenSettingsDialogCommand = new DelegateCommand(OpenSettingsDialog);
        StartLogParseCommand = new DelegateCommand(StartLogParse, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(OutputFile))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => OutputFile);
        ResetTimeCommand = new DelegateCommand(ResetTime);

        ResetTime();

        SetOutputFile();
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

    public string OutputFile
    {
        get => _outputFile;
        set => SetProperty(ref _outputFile, value);
    }

    public DelegateCommand ResetTimeCommand { get; }

    public DelegateCommand StartLogParseCommand { get; }

    public string StartTimeText
    {
        get => _startTimeText;
        set => SetProperty(ref _startTimeText, value);
    }

    private void OpenSettingsDialog()
    {
        ILogSelectionViewModel settingsDialog = _dialogFactory.CreateSettingsViewDialog(_settings);
        if (settingsDialog.ShowDialog() != true)
            return;

        _settings.EqDirectory = settingsDialog.EqDirectory;
        _settings.SelectedLogFiles = settingsDialog.SelectedCharacterLogFiles;
        _settings.SaveSettings();
    }

    private void OutputRawAnalyzerResults(RaidEntries raidEntries)
    {
        string directory = string.IsNullOrWhiteSpace(_settings.EqDirectory) ? Directory.GetCurrentDirectory() : _settings.EqDirectory;
        if (!string.IsNullOrWhiteSpace(OutputFile))
            directory = Path.GetDirectoryName(OutputFile);

        string rawAnalyzerOutputFile = $"RawAnalyzerOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawAnalyzerOutputFullPath = Path.Combine(directory, rawAnalyzerOutputFile);
        File.AppendAllLines(rawAnalyzerOutputFullPath, raidEntries.GetAllEntries());
    }

    private void OutputRawParseResults(LogParseResults results)
    {
        string directory = string.IsNullOrWhiteSpace(_settings.EqDirectory) ? Directory.GetCurrentDirectory() : _settings.EqDirectory;
        if (!string.IsNullOrWhiteSpace(OutputFile))
            directory = Path.GetDirectoryName(OutputFile);

        string rawParseOutputFile = $"RawParseOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawParseOutputFullPath = Path.Combine(directory, rawParseOutputFile);
        foreach (EqLogFile logFile in results.EqLogFiles)
        {
            File.AppendAllLines(rawParseOutputFullPath, logFile.GetAllLogLines());
        }
    }

    private void ResetTime()
    {
        DateTime currentTime = DateTime.Now;
        _endTimeText = currentTime.ToString(DateTimeFormat);
        _startTimeText = currentTime.AddHours(-6).ToString(DateTimeFormat);
    }

    private void SetOutputFile()
    {
        string directory = string.IsNullOrWhiteSpace(_settings.EqDirectory) ? Directory.GetCurrentDirectory() : _settings.EqDirectory;
        if (!string.IsNullOrWhiteSpace(OutputFile))
            directory = Path.GetDirectoryName(OutputFile);

        string outputFile = $"RaidLog-{DateTime.Now:yyyyMMdd-HHmm}.txt";
        OutputFile = Path.Combine(directory, outputFile);
    }

    private async void StartLogParse()
        => await StartLogParseAsync();

    private async Task StartLogParseAsync()
    {
        if (!DateTime.TryParse(StartTimeText, out DateTime startTime))
        {
            MessageBox.Show(Strings.GetString("StartTimeErrorMessage"), Strings.GetString("StartTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!DateTime.TryParse(EndTimeText, out DateTime endTime))
        {
            MessageBox.Show(Strings.GetString("EndTimeErrorMessage"), Strings.GetString("EndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (startTime > endTime)
        {
            MessageBox.Show(Strings.GetString("StartEndTimeErrorMessage"), Strings.GetString("StartEndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _performingParse = true;
            StartLogParseCommand.RaiseCanExecuteChanged();

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

            IOutputGenerator generator = new FileOutputGenerator(OutputFile);
            await generator.GenerateOutput(raidEntries);

            ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialog(OutputFile, Strings.GetString("SuccessfulCompleteMessage"));
            completedDialog.ShowDialog();
        }
        finally
        {
            _performingParse = false;
            StartLogParseCommand.RaiseCanExecuteChanged();
            SetOutputFile();
        }
    }
}

public interface IMainDisplayViewModel : IEuropaViewModel
{
    string EndTimeText { get; set; }

    bool IsRawAnalyzerResultsChecked { get; set; }

    bool IsRawParseResultsChecked { get; set; }

    DelegateCommand OpenSettingsDialogCommand { get; }

    string OutputFile { get; set; }

    DelegateCommand ResetTimeCommand { get; }

    DelegateCommand StartLogParseCommand { get; }

    string StartTimeText { get; set; }
}
