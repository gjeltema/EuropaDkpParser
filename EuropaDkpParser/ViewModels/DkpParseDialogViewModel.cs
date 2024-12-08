// -----------------------------------------------------------------------
// DkpParseDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using DkpParser;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal class DkpParseDialogViewModel : DialogViewModelBase, IDkpParseDialogViewModel
{
    private readonly DkpLogGenerator _logGenerator;
    private readonly IDkpParserSettings _settings;
    private bool _debugOptionsEnabled;
    private string _endTimeText;
    private string _generatedFile;
    private bool _isOutputRawParseResultsChecked;
    private bool _isRawAnalyzerResultsChecked;
    private bool _outputAnalyzerErrors;
    private bool _performingParse = false;
    private string _startTimeText;
    private bool _startTimeSet = false;

    internal DkpParseDialogViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory, IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("DkpParseDialogTitleText");

        _settings = settings;

        _logGenerator = new(settings, dialogFactory);

        StartLogParseCommand = new DelegateCommand(StartLogParse, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(GeneratedFile))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => GeneratedFile);
        GetRawLogFileCommand = new DelegateCommand(GetRawLogFilesParse, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText);
        ResetTimeCommand = new DelegateCommand(ResetTime);

        ResetTime();
        SetOutputFile();
        DebugOptionsEnabled = _settings.EnableDebugOptions;
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
                if (_startTimeSet && DateTime.TryParse(value, out DateTime endTime))
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

    public bool OutputAnalyzerErrors
    {
        get => _outputAnalyzerErrors;
        set => SetProperty(ref _outputAnalyzerErrors, value);
    }

    public DelegateCommand ResetTimeCommand { get; }

    public DelegateCommand StartLogParseCommand { get; }

    public string StartTimeText
    {
        get => _startTimeText;
        set
        {
            SetProperty(ref _startTimeText, value);
            _startTimeSet = true;
        }
    }

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

    private string GetOutputPath()
        => string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? _logGenerator.GetUserProfilePath() : _settings.OutputDirectory;

    private async void GetRawLogFilesParse()
        => await ExecuteParse(GetRawLogFilesParseAsync);

    private async Task GetRawLogFilesParseAsync(DateTime startTime, DateTime endTime)
        => await _logGenerator.GetRawLogFilesParseAsync(startTime, endTime, GetOutputPath());

    private void RefreshCommands()
    {
        StartLogParseCommand.RaiseCanExecuteChanged();
        GetRawLogFileCommand.RaiseCanExecuteChanged();
    }

    private void ResetTime()
    {
        DateTime currentTime = DateTime.Now;
        EndTimeText = currentTime.ToString(Constants.TimePickerDisplayDateTimeFormat);
        StartTimeText = currentTime.AddHours(-6).ToString(Constants.TimePickerDisplayDateTimeFormat);
        SetOutputFile();
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
            OutputDirectory = _settings.OutputDirectory,
            GeneratedFile = GeneratedFile,
            OutputPath = GetOutputPath()
        };

        await _logGenerator.StartLogParseAsync(sessionSettings);

        SetOutputFile();
    }
}

public interface IDkpParseDialogViewModel : IDialogViewModel
{
    bool DebugOptionsEnabled { get; }

    string EndTimeText { get; set; }

    string GeneratedFile { get; set; }

    DelegateCommand GetRawLogFileCommand { get; }

    bool IsRawAnalyzerResultsChecked { get; set; }

    bool IsRawParseResultsChecked { get; set; }

    bool OutputAnalyzerErrors { get; set; }

    DelegateCommand ResetTimeCommand { get; }

    DelegateCommand StartLogParseCommand { get; }

    string StartTimeText { get; set; }
}
