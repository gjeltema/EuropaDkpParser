// -----------------------------------------------------------------------
// MainDisplayViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows;
using DkpParser;
using Prism.Commands;

internal sealed class MainDisplayViewModel : EuropaViewModelBase, IMainDisplayViewModel
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;
    private string _endTimeText;
    private string _outputFile;
    private string _startTimeText;

    internal MainDisplayViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
        OpenSettingsDialogCommand = new DelegateCommand(OpenSettingsDialog);
        StartLogParseCommand = new DelegateCommand(StartLogParse, () => !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(OutputFile))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => OutputFile);
        ResetTimeCommand = new DelegateCommand(ResetTime);

        ResetTime();

        string defaultOutputDirectory = string.IsNullOrWhiteSpace(_settings.EqDirectory) ? Directory.GetCurrentDirectory() : _settings.EqDirectory;
        string outputFile = $"RaidLog-{DateTime.Now:yyyyMMdd-HHmm}.txt";
        OutputFile = Path.Combine(defaultOutputDirectory, outputFile);
    }

    public string EndTimeText
    {
        get => _endTimeText;
        set
        {
            if(SetProperty(ref _endTimeText, value))
            {
                if (DateTime.TryParse(value, out DateTime endTime))
                {
                    StartTimeText = endTime.AddHours(-6).ToString(DateTimeFormat);
                }
            }
        }
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

    private void ResetTime()
    {
        DateTime currentTime = DateTime.Now;
        _endTimeText = currentTime.ToString(DateTimeFormat);
        _startTimeText = currentTime.AddHours(-5).ToString(DateTimeFormat);
    }

    private async void StartLogParse()
        => await StartLogParseAsync();

    private async Task StartLogParseAsync()
    {
        if (!DateTime.TryParse(StartTimeText, out DateTime startTime))
        {
            MessageBox.Show("Start Time is not in a valid format.", "Start Time error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!DateTime.TryParse(EndTimeText, out DateTime endTime))
        {
            MessageBox.Show("End Time is not in a valid format.", "End Time error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (startTime > endTime)
        {
            MessageBox.Show("Start Time is after End Time.", "Time error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ILogParseProcessor parseProcessor = new LogParseProcessor(_settings);
        LogParseResults results = await Task.Run(() => parseProcessor.ParseLogs(startTime, endTime));

        ILogEntryAnalyzer logEntryAnalyzer = new LogEntryAnalyzer();
        RaidEntries raidEntries = await Task.Run(() => logEntryAnalyzer.AnalyzeRaidLogEntries(results));


    }
}

public interface IMainDisplayViewModel : IEuropaViewModel
{
    string EndTimeText { get; set; }

    DelegateCommand OpenSettingsDialogCommand { get; }

    string OutputFile { get; set; }

    DelegateCommand ResetTimeCommand { get; }

    DelegateCommand StartLogParseCommand { get; }

    string StartTimeText { get; set; }
}
