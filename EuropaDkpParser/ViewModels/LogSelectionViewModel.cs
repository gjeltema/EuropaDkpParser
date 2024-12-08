// -----------------------------------------------------------------------
// LogSelectionViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows.Forms;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class LogSelectionViewModel : DialogViewModelBase, ILogSelectionViewModel
{
    private readonly IDkpDataRetriever _dkpDataRetriever;
    private readonly IDkpParserSettings _settings;
    private string _apiReadToken;
    private string _apiUrl;
    private string _apiWriteToken;
    private bool _dkpspentAucEnable;
    private bool _dkpspentGuEnable;
    private bool _dkpspentOocEnable;
    private string _eqDirectory;
    private bool _includeTellsInRawLog;
    private bool _isDebugOptionsEnabled;
    private string _logFileMatchPattern;
    private string _outputDirectory;
    private string _selectedLogFileToAdd;
    private string _selectedLogFileToParse;
    private bool _showAfkReview;
    private bool _showPogress;
    private bool _useAdvancedDialog;
    private bool _useLightMode;

    internal LogSelectionViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("SettingsDialogTitleText");
        Height = 600;
        Width = 700;

        _settings = settings;

        _dkpDataRetriever = new DkpDataRetriever(settings);

        SelectEqDirectoryCommand = new DelegateCommand(SelectEqDirectory);
        SelectOutputDirectoryCommand = new DelegateCommand(SelectOutputDirectory);
        AddLogFileToListCommand = new DelegateCommand(AddLogFile, () => !string.IsNullOrWhiteSpace(SelectedLogFileToAdd))
            .ObservesProperty(() => SelectedLogFileToAdd);
        RemoveLogFileFromListCommand = new DelegateCommand(RemoveLogFileFromList, () => !string.IsNullOrWhiteSpace(SelectedLogFileToParse))
            .ObservesProperty(() => SelectedLogFileToParse);
        RetrieveAndSaveDkpCharactersCommand = new DelegateCommand(RetrieveAndSaveDkpCharacters, () => !ShowProgress)
            .ObservesProperty(() => ShowProgress);

        _eqDirectory = _settings.EqDirectory;
        OutputDirectory = _settings.OutputDirectory;
        SelectedCharacterLogFiles = new List<string>(_settings.SelectedLogFiles);
        IsDebugOptionsEnabled = _settings.EnableDebugOptions;
        _logFileMatchPattern = _settings.LogFileMatchPattern;

        ApiUrl = _settings.ApiUrl;
        ApiReadToken = _settings.ApiReadToken;
        ApiWriteToken = _settings.ApiWriteToken;

        UseLightMode = _settings.UseLightMode;
        ShowAfkReview = _settings.ShowAfkReview;
        UseAdvancedDialog = _settings.UseAdvancedDialog;
        IncludeTellsInRawLog = _settings.IncludeTellsInRawLog;

        DkpspentAucEnable = _settings.DkpspentAucEnabled;
        DkpspentGuEnable = _settings.DkpspentGuEnabled;
        DkpspentOocEnable = _settings.DkpspentOocEnabled;

        SetAllCharacterLogFiles();
    }

    public DelegateCommand AddLogFileToListCommand { get; }

    public ICollection<string> AllCharacterLogFiles { get; private set; }

    public string ApiReadToken
    {
        get => _apiReadToken;
        set => SetProperty(ref _apiReadToken, value);
    }

    public string ApiUrl
    {
        get => _apiUrl;
        set => SetProperty(ref _apiUrl, value);
    }

    public string ApiWriteToken
    {
        get => _apiWriteToken;
        set => SetProperty(ref _apiWriteToken, value);
    }

    public bool DkpspentAucEnable
    {
        get => _dkpspentAucEnable;
        set => SetProperty(ref _dkpspentAucEnable, value);
    }

    public bool DkpspentGuEnable
    {
        get => _dkpspentGuEnable;
        set => SetProperty(ref _dkpspentGuEnable, value);
    }

    public bool DkpspentOocEnable
    {
        get => _dkpspentOocEnable;
        set => SetProperty(ref _dkpspentOocEnable, value);
    }

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

    public bool IncludeTellsInRawLog
    {
        get => _includeTellsInRawLog;
        set => SetProperty(ref _includeTellsInRawLog, value);
    }

    public bool IsDebugOptionsEnabled
    {
        get => _isDebugOptionsEnabled;
        set => SetProperty(ref _isDebugOptionsEnabled, value);
    }

    public string LogFileMatchPattern
    {
        get => _logFileMatchPattern;
        set
        {
            SetProperty(ref _logFileMatchPattern, value);
            SetAllCharacterLogFiles();
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public DelegateCommand RemoveLogFileFromListCommand { get; }

    public DelegateCommand RetrieveAndSaveDkpCharactersCommand { get; }

    public ICollection<string> SelectedCharacterLogFiles { get; private set; }

    public string SelectedLogFileToAdd
    {
        get => _selectedLogFileToAdd;
        set => SetProperty(ref _selectedLogFileToAdd, value);
    }

    public string SelectedLogFileToParse
    {
        get => _selectedLogFileToParse;
        set => SetProperty(ref _selectedLogFileToParse, value);
    }

    public DelegateCommand SelectEqDirectoryCommand { get; }

    public DelegateCommand SelectOutputDirectoryCommand { get; }

    public bool ShowAfkReview
    {
        get => _showAfkReview;
        set => SetProperty(ref _showAfkReview, value);
    }

    public bool ShowProgress
    {
        get => _showPogress;
        private set => SetProperty(ref _showPogress, value);
    }

    public bool UseAdvancedDialog
    {
        get => _useAdvancedDialog;
        set => SetProperty(ref _useAdvancedDialog, value);
    }

    public bool UseLightMode
    {
        get => _useLightMode;
        set => SetProperty(ref _useLightMode, value);
    }

    public void UpdateSettings(IDkpParserSettings settings)
    {
        _settings.EqDirectory = EqDirectory;
        _settings.SelectedLogFiles = SelectedCharacterLogFiles;

        _settings.OutputDirectory = OutputDirectory;
        TryCreateDirectory(_settings.OutputDirectory);

        _settings.EnableDebugOptions = IsDebugOptionsEnabled;
        _settings.ApiUrl = ApiUrl;
        _settings.ApiReadToken = ApiReadToken;
        _settings.ApiWriteToken = ApiWriteToken;
        _settings.ShowAfkReview = ShowAfkReview;
        _settings.LogFileMatchPattern = LogFileMatchPattern;
        _settings.UseAdvancedDialog = UseAdvancedDialog;
        _settings.IncludeTellsInRawLog = IncludeTellsInRawLog;
        _settings.DkpspentAucEnabled = DkpspentAucEnable;
        _settings.DkpspentGuEnabled = DkpspentGuEnable;
        _settings.DkpspentOocEnabled = DkpspentOocEnable;
        _settings.UseLightMode = UseLightMode;
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


        SelectedCharacterLogFiles = new List<string>(SelectedCharacterLogFiles);
        SelectedCharacterLogFiles.Add(SelectedLogFileToAdd);
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

    private async void RetrieveAndSaveDkpCharacters()
        => await RetrieveAndSaveDkpCharactersAsync();

    private async Task RetrieveAndSaveDkpCharactersAsync()
    {
        if (ShowProgress)
            return;

        try
        {
            ShowProgress = true;
            ICollection<DkpUserCharacter> dkpCharacters = await _dkpDataRetriever.GetUserCharacters();
            _settings.CharactersOnDkpServer.SaveValues(dkpCharacters);
        }
        finally
        {
            ShowProgress = false;
        }
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

    string ApiReadToken { get; set; }

    string ApiUrl { get; set; }

    string ApiWriteToken { get; set; }

    bool DkpspentAucEnable { get; set; }

    bool DkpspentGuEnable { get; set; }

    bool DkpspentOocEnable { get; set; }

    string EqDirectory { get; set; }

    bool IncludeTellsInRawLog { get; set; }

    bool IsDebugOptionsEnabled { get; set; }

    string LogFileMatchPattern { get; set; }

    string OutputDirectory { get; set; }

    DelegateCommand RemoveLogFileFromListCommand { get; }

    DelegateCommand RetrieveAndSaveDkpCharactersCommand { get; }

    ICollection<string> SelectedCharacterLogFiles { get; }

    string SelectedLogFileToAdd { get; set; }

    string SelectedLogFileToParse { get; set; }

    DelegateCommand SelectEqDirectoryCommand { get; }

    DelegateCommand SelectOutputDirectoryCommand { get; }

    bool ShowAfkReview { get; set; }

    bool ShowProgress { get; }

    bool UseAdvancedDialog { get; set; }

    bool UseLightMode { get; set; }

    void UpdateSettings(IDkpParserSettings settings);
}
