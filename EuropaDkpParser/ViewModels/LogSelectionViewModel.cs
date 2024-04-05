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
    private readonly IDkpParserSettings _settings;
    private string _apiReadToken;
    private string _apiUrl;
    private string _apiWriteToken;
    private string _eqDirectory;
    private bool _isDebugOptionsEnabled;
    private string _outputDirectory;
    private string _selectedLogFileToAdd;
    private string _selectedLogFileToParse;

    internal LogSelectionViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("SettingsDialogTitleText");

        _settings = settings;

        SelectEqDirectoryCommand = new DelegateCommand(SelectEqDirectory);
        SelectOutputDirectoryCommand = new DelegateCommand(SelectOutputDirectory);
        AddLogFileToListCommand = new DelegateCommand(AddLogFile, () => !string.IsNullOrWhiteSpace(SelectedLogFileToAdd))
            .ObservesProperty(() => SelectedLogFileToAdd);
        RemoveLogFileFromListCommand = new DelegateCommand(RemoveLogFileFromList, () => !string.IsNullOrWhiteSpace(SelectedLogFileToParse))
            .ObservesProperty(() => SelectedLogFileToParse);

        EqDirectory = _settings.EqDirectory;
        OutputDirectory = _settings.OutputDirectory;
        SelectedCharacterLogFiles = new List<string>(_settings.SelectedLogFiles);
        IsDebugOptionsEnabled = _settings.EnableDebugOptions;

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

    public string EqDirectory
    {
        get => _eqDirectory;
        set
        {
            SetProperty(ref _eqDirectory, value);
            SetAllCharacterLogFiles();
            if (string.IsNullOrWhiteSpace(OutputDirectory))
                OutputDirectory = EqDirectory;
        }
    }

    public bool IsDebugOptionsEnabled
    {
        get => _isDebugOptionsEnabled;
        set => SetProperty(ref _isDebugOptionsEnabled, value);
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public DelegateCommand RemoveLogFileFromListCommand { get; }

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

        IEnumerable<string> logFiles = Directory.EnumerateFiles(EqDirectory, Constants.EqLogSearchPattern);
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

    string EqDirectory { get; set; }

    bool IsDebugOptionsEnabled { get; set; }

    string OutputDirectory { get; set; }

    DelegateCommand RemoveLogFileFromListCommand { get; }

    ICollection<string> SelectedCharacterLogFiles { get; }

    string SelectedLogFileToAdd { get; set; }

    string SelectedLogFileToParse { get; set; }

    DelegateCommand SelectEqDirectoryCommand { get; }

    DelegateCommand SelectOutputDirectoryCommand { get; }
}
