// -----------------------------------------------------------------------
// LogSelectionViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows.Forms;
using DkpParser;
using EuropaDkpParser.Resources;
using Gjeltema.Logging;
using Prism.Commands;

internal sealed class LogSelectionViewModel : DialogViewModelBase, ILogSelectionViewModel
{
    private readonly IDkpParserSettings _settings;
    private string _eqDirectory;
    private string _logFileMatchPattern;

    internal LogSelectionViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("SettingsDialogTitleText");
        Height = 600;
        Width = 700;

        _settings = settings;

        SelectEqDirectoryCommand = new DelegateCommand(SelectEqDirectory);
        SelectOutputDirectoryCommand = new DelegateCommand(SelectOutputDirectory);
        AddLogFileToListCommand = new DelegateCommand(AddLogFile, () => !string.IsNullOrWhiteSpace(SelectedLogFileToAdd))
            .ObservesProperty(() => SelectedLogFileToAdd);
        RemoveLogFileFromListCommand = new DelegateCommand(RemoveLogFileFromList, () => !string.IsNullOrWhiteSpace(SelectedLogFileToParse))
            .ObservesProperty(() => SelectedLogFileToParse);

        _eqDirectory = _settings.EqDirectory;
        OutputDirectory = _settings.OutputDirectory;
        SelectedCharacterLogFiles = [.. _settings.SelectedLogFiles];
        _logFileMatchPattern = _settings.LogFileMatchPattern;

        LoggingLevels = [.. Enum.GetNames<LogLevel>()];

        ApiMusterUrl = _settings.ApiMusterUrl;
        ApiMusterToken = _settings.ApiMusterToken;

        UseLightMode = _settings.UseLightMode;
        ShowAfkReview = _settings.ShowAfkReview;
        IncludeTellsInRawLog = _settings.IncludeTellsInRawLog;

        DkpspentGuEnable = _settings.DkpspentGuEnabled;

        OverlayFontSize = _settings.OverlayFontSize.ToString();
        OverlayFontColor = _settings.OverlayFontColor;
        OverlayBackgroundColor = _settings.OverlayBackgroundColor;

        SelectedLoggingLevel = _settings.LoggingLevel.ToString();

        MezBreaksToShow = _settings.MezBreaksToShow;

        SetAllCharacterLogFiles();
    }

    public DelegateCommand AddLogFileToListCommand { get; }

    public ICollection<string> AllCharacterLogFiles { get; private set; }

    public string ApiMusterToken { get; set => SetProperty(ref field, value); }

    public string ApiMusterUrl { get; set => SetProperty(ref field, value); }

    public bool DkpspentGuEnable { get; set => SetProperty(ref field, value); }

    public string EqDirectory
    {
        get => _eqDirectory;
        set
        {
            SetProperty(ref _eqDirectory, value);
            SetAllCharacterLogFiles();
            if (string.IsNullOrWhiteSpace(OutputDirectory))
                OutputDirectory = Path.Combine(value, "Generated");
        }
    }

    public bool IncludeTellsInRawLog { get; set => SetProperty(ref field, value); }

    public string LogFileMatchPattern
    {
        get => _logFileMatchPattern;
        set
        {
            SetProperty(ref _logFileMatchPattern, value);
            SetAllCharacterLogFiles();
        }
    }

    public ICollection<string> LoggingLevels { get; }

    public int MezBreaksToShow { get; set => SetProperty(ref field, value); }

    public string OutputDirectory { get; set => SetProperty(ref field, value); }

    public string OverlayBackgroundColor { get; set => SetProperty(ref field, value); }

    public string OverlayFontColor { get; set => SetProperty(ref field, value); }

    public string OverlayFontSize { get; set => SetProperty(ref field, value); }

    public DelegateCommand RemoveLogFileFromListCommand { get; }

    public DelegateCommand RetrieveAndSaveDkpCharactersCommand { get; }

    public ICollection<string> SelectedCharacterLogFiles { get; private set; }

    public string SelectedLogFileToAdd { get; set => SetProperty(ref field, value); }

    public string SelectedLogFileToParse { get; set => SetProperty(ref field, value); }

    public string SelectedLoggingLevel { get; set => SetProperty(ref field, value); }

    public DelegateCommand SelectEqDirectoryCommand { get; }

    public DelegateCommand SelectOutputDirectoryCommand { get; }

    // Unused for now.  May re-add later if/when I fully implement the AFK Review dialog.
    public bool ShowAfkReview { get; set => SetProperty(ref field, value); }

    public bool ShowProgress { get; private set => SetProperty(ref field, value); }

    public bool UseLightMode { get; set => SetProperty(ref field, value); }

    public void UpdateSettings(IDkpParserSettings settings)
    {
        _settings.EqDirectory = EqDirectory;
        _settings.SelectedLogFiles = SelectedCharacterLogFiles;

        _settings.OutputDirectory = OutputDirectory;
        TryCreateDirectory(_settings.OutputDirectory);

        _settings.ApiMusterUrl = ApiMusterUrl;
        _settings.ApiMusterToken = ApiMusterToken;
        _settings.ShowAfkReview = ShowAfkReview;
        _settings.LogFileMatchPattern = LogFileMatchPattern;
        _settings.IncludeTellsInRawLog = IncludeTellsInRawLog;
        _settings.DkpspentGuEnabled = DkpspentGuEnable;
        _settings.UseLightMode = UseLightMode;
        _settings.OverlayFontColor = OverlayFontColor;
        _settings.OverlayBackgroundColor = OverlayBackgroundColor;
        if (int.TryParse(OverlayFontSize, out int fontSize))
        {
            _settings.OverlayFontSize = fontSize;
        }

        _settings.MezBreaksToShow = MezBreaksToShow;
        _settings.LoggingLevel = Log.ConvertToLogLevel(SelectedLoggingLevel);
        Log.Logger.Default.LoggingLevel = _settings.LoggingLevel;

        _settings.SaveSettings();
    }

    private static void TryCreateDirectory(string directoryName)
    {
        try
        {
            Task.Run(() => Directory.CreateDirectory(directoryName));
        }
        catch
        {
        }
    }

    private void AddLogFile()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFileToAdd))
            return;

        if (SelectedCharacterLogFiles.Contains(SelectedLogFileToAdd))
            return;


        SelectedCharacterLogFiles = [.. SelectedCharacterLogFiles, SelectedLogFileToAdd];
        RaisePropertyChanged(nameof(SelectedCharacterLogFiles));
    }

    private void RemoveLogFileFromList()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFileToParse))
            return;

        SelectedCharacterLogFiles.Remove(SelectedLogFileToParse);
        SelectedCharacterLogFiles = new List<string>(SelectedCharacterLogFiles);
        RaisePropertyChanged(nameof(SelectedCharacterLogFiles));
    }

    private void SelectEqDirectory()
    {
        using var folderDialog = new FolderBrowserDialog()
        {
            Description = Strings.GetString("SelectEqLogFileFolder"),
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(EqDirectory))
        {
            folderDialog.SelectedPath = EqDirectory;
        }

        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            EqDirectory = folderDialog.SelectedPath;
        }
    }

    private void SelectOutputDirectory()
    {
        using var folderDialog = new FolderBrowserDialog()
        {
            Description = Strings.GetString("SelectOutputLogFileFolder"),
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(OutputDirectory))
        {
            folderDialog.SelectedPath = OutputDirectory;
        }

        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputDirectory = folderDialog.SelectedPath;
        }
    }

    private void SetAllCharacterLogFiles()
    {
        if (string.IsNullOrWhiteSpace(EqDirectory))
        {
            AllCharacterLogFiles = [];
            return;
        }

        IEnumerable<string> logFiles = Directory.EnumerateFiles(EqDirectory, LogFileMatchPattern);
        AllCharacterLogFiles = new List<string>(logFiles);
        if (AllCharacterLogFiles.Count > 0)
            SelectedLogFileToAdd = AllCharacterLogFiles.First();

        RaisePropertyChanged(nameof(AllCharacterLogFiles));
    }
}

public interface ILogSelectionViewModel : IDialogViewModel
{
    DelegateCommand AddLogFileToListCommand { get; }

    ICollection<string> AllCharacterLogFiles { get; }

    string ApiMusterToken { get; set; }

    string ApiMusterUrl { get; set; }

    bool DkpspentGuEnable { get; set; }

    string EqDirectory { get; set; }

    bool IncludeTellsInRawLog { get; set; }

    string LogFileMatchPattern { get; set; }

    ICollection<string> LoggingLevels { get; }

    int MezBreaksToShow { get; set; }

    string OutputDirectory { get; set; }

    string OverlayBackgroundColor { get; set; }

    string OverlayFontColor { get; set; }

    string OverlayFontSize { get; set; }

    DelegateCommand RemoveLogFileFromListCommand { get; }

    ICollection<string> SelectedCharacterLogFiles { get; }

    string SelectedLogFileToAdd { get; set; }

    string SelectedLogFileToParse { get; set; }

    string SelectedLoggingLevel { get; set; }

    DelegateCommand SelectEqDirectoryCommand { get; }

    DelegateCommand SelectOutputDirectoryCommand { get; }

    bool ShowAfkReview { get; set; }

    bool ShowProgress { get; }

    bool UseLightMode { get; set; }

    void UpdateSettings(IDkpParserSettings settings);
}
