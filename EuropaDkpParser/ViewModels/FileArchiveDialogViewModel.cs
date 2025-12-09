// -----------------------------------------------------------------------
// FileArchiveDialogViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using DkpParser;
using DkpParser.Parsers;
using EuropaDkpParser.Resources;
using Gjeltema.Logging;
using Prism.Commands;

internal sealed class FileArchiveDialogViewModel : DialogViewModelBase, IFileArchiveDialogViewModel
{
    private const string LogPrefix = $"[{nameof(FileArchiveDialogViewModel)}]";
    private readonly CharacterInventoryParser _inventoryParser;
    private readonly IDkpParserSettings _settings;
    private bool _archiveBasedOnAge;
    private bool _archiveBasedOnSize;
    private string _eqLogArchiveDirectory;
    private string _eqLogArchiveFileAge;
    private string _eqLogArchiveFileSize;
    private string _generatedLogsArchiveDirectory;
    private string _generatedLogsArchiveFileAge;
    private bool _isAllLogsArchived;
    private bool _isSelectedLogsArchived;
    private string _selectedEqLogFileToAdd;
    private string _selectedEqLogFileToRemove;

    internal FileArchiveDialogViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("FileArchiveDialogTitleText");
        Height = 570;
        Width = 700;

        _settings = settings;

        OpenEqLogArchiveDirectoryCommand = new DelegateCommand(OpenEqLogArchiveDirectory);
        OpenGeneratedLogArchiveDirectoryCommand = new DelegateCommand(OpenGeneratedLogArchiveDirectory);
        SetEqLogArchiveDirectoryCommand = new DelegateCommand(SetEqLogArchiveDirectory);
        SetGeneratedLogArchiveDirectoryCommand = new DelegateCommand(SetGeneratedLogArchiveDirectory);
        ArchiveEqLogFilesCommand = new DelegateCommand(ArchiveEqLogFiles, () => !string.IsNullOrWhiteSpace(EqLogArchiveDirectory) && (ArchiveBasedOnAge || ArchiveBasedOnSize))
            .ObservesProperty(() => ArchiveBasedOnAge).ObservesProperty(() => ArchiveBasedOnSize).ObservesProperty(() => EqLogArchiveDirectory);
        ArchiveGeneratedLogFilesCommand = new DelegateCommand(ArchiveGeneratedLogFiles, () => !string.IsNullOrWhiteSpace(GeneratedLogsArchiveDirectory))
            .ObservesProperty(() => GeneratedLogsArchiveDirectory);
        AddSelectedEqLogFileToArchiveCommand = new DelegateCommand(AddSelectedEqLogFileToArchive, () => !string.IsNullOrWhiteSpace(SelectedEqLogFileToAdd))
            .ObservesProperty(() => SelectedEqLogFileToAdd);
        RemoveSelectedEqLogFileCommand = new DelegateCommand(RemoveSelectedEqLogFile, () => !string.IsNullOrWhiteSpace(SelectedEqLogFileToRemove))
            .ObservesProperty(() => SelectedEqLogFileToRemove);
        AddInventoryDirCommand = new DelegateCommand(AddInventoryDirectory);
        AggregateInventoriesCommand = new DelegateCommand(AggregateInventoryFiles);
        OpenInventoryGeneratedFileDirectoryCommand = new DelegateCommand(OpenInventoryGeneratedDirectory);

        EqLogArchiveDirectory = _settings.EqLogFileArchiveDirectory;
        EqLogArchiveFileAge = _settings.EqLogFileAgeToArchiveInDays.ToString();
        EqLogArchiveFileSize = _settings.EqLogFileSizeToArchiveInMBs.ToString();
        GeneratedLogsArchiveDirectory = _settings.GeneratedLogFilesArchiveDirectory;
        GeneratedLogsArchiveFileAge = _settings.GeneratedLogFilesAgeToArchiveInDays.ToString();
        IsAllLogsArchived = _settings.ArchiveAllEqLogFiles;
        SelectedEqLogFiles = _settings.SelectedLogFiles.Order().ToList();

        ArchiveBasedOnAge = _settings.EqLogFileAgeToArchiveInDays != 0;
        ArchiveBasedOnSize = _settings.EqLogFileSizeToArchiveInMBs != 0;

        IsAllLogsArchived = true;

        _inventoryParser = new();

        PopulateAllLogFiles();
    }

    public DelegateCommand AddInventoryDirCommand { get; }

    public DelegateCommand AddSelectedEqLogFileToArchiveCommand { get; }

    public DelegateCommand AggregateInventoriesCommand { get; }

    public bool ArchiveBasedOnAge
    {
        get => _archiveBasedOnAge;
        set => SetProperty(ref _archiveBasedOnAge, value);
    }

    public bool ArchiveBasedOnSize
    {
        get => _archiveBasedOnSize;
        set => SetProperty(ref _archiveBasedOnSize, value);
    }

    public DelegateCommand ArchiveEqLogFilesCommand { get; }

    public DelegateCommand ArchiveGeneratedLogFilesCommand { get; }

    public string EqLogArchiveDirectory
    {
        get => _eqLogArchiveDirectory;
        set => SetProperty(ref _eqLogArchiveDirectory, value);
    }

    public string EqLogArchiveFileAge
    {
        get => _eqLogArchiveFileAge;
        set => SetProperty(ref _eqLogArchiveFileAge, value);
    }

    public string EqLogArchiveFileSize
    {
        get => _eqLogArchiveFileSize;
        set => SetProperty(ref _eqLogArchiveFileSize, value);
    }

    public string GeneratedLogsArchiveDirectory
    {
        get => _generatedLogsArchiveDirectory;
        set => SetProperty(ref _generatedLogsArchiveDirectory, value);
    }

    public string GeneratedLogsArchiveFileAge
    {
        get => _generatedLogsArchiveFileAge;
        set => SetProperty(ref _generatedLogsArchiveFileAge, value);
    }

    public bool IsAllLogsArchived
    {
        get => _isAllLogsArchived;
        set => SetProperty(ref _isAllLogsArchived, value);
    }

    public bool IsSelectedLogsArchived
    {
        get => _isSelectedLogsArchived;
        set => SetProperty(ref _isSelectedLogsArchived, value);
    }

    public DelegateCommand OpenEqLogArchiveDirectoryCommand { get; }

    public DelegateCommand OpenGeneratedLogArchiveDirectoryCommand { get; }

    public DelegateCommand OpenInventoryGeneratedFileDirectoryCommand { get; }

    public ICollection<string> PossibleEqLogFiles { get; private set; }

    public DelegateCommand RemoveSelectedEqLogFileCommand { get; }

    public ICollection<string> SelectedEqLogFiles { get; private set; }

    public string SelectedEqLogFileToAdd
    {
        get => _selectedEqLogFileToAdd;
        set => SetProperty(ref _selectedEqLogFileToAdd, value);
    }

    public string SelectedEqLogFileToRemove
    {
        get => _selectedEqLogFileToRemove;
        set => SetProperty(ref _selectedEqLogFileToRemove, value);
    }

    public DelegateCommand SetEqLogArchiveDirectoryCommand { get; }

    public DelegateCommand SetGeneratedLogArchiveDirectoryCommand { get; }

    public void UpdateSettings(IDkpParserSettings settings)
    {
        _settings.ArchiveAllEqLogFiles = IsAllLogsArchived;
        _settings.EqLogFileArchiveDirectory = EqLogArchiveDirectory;
        _settings.EqLogFileAgeToArchiveInDays = GetIntValue(EqLogArchiveFileAge);
        _settings.EqLogFileSizeToArchiveInMBs = GetIntValue(EqLogArchiveFileSize);
        _settings.EqLogFilesToArchive = SelectedEqLogFiles;
        _settings.GeneratedLogFilesAgeToArchiveInDays = GetIntValue(GeneratedLogsArchiveFileAge);
        _settings.GeneratedLogFilesArchiveDirectory = GeneratedLogsArchiveDirectory;

        _settings.SaveSettings();
    }

    private static void MoveFile(string currentFile, string destinationFile)
    {
        try
        {
            File.Move(currentFile, destinationFile);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error moving file {currentFile} to {destinationFile}: {ex.ToLogMessage()}");
            string errorMessage = string.Format(Strings.GetString("FileMoveErrorMessage"), currentFile, destinationFile, ex.Message);
            MessageBox.Show(errorMessage, Strings.GetString("FileMoveError"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void MoveOldFiles(string fromDirectory, string fileSearchString, TimeSpan maxAge, string destinationDirectory)
    {
        foreach (string foundFile in Directory.EnumerateFiles(fromDirectory, fileSearchString))
        {
            FileInfo fi = new(foundFile);
            if (fi.Exists && DateTime.Now.Subtract(fi.LastWriteTime) > maxAge)
            {
                string newFileName = $"{fi.Name[0..^4]}-{DateTime.Now.ToString(Constants.ArchiveFileNameTimeFormat)}.txt";
                string newFileLocation = Path.Combine(destinationDirectory, newFileName);
                MoveFile(fi.FullName, newFileLocation);
            }
        }
    }

    private void AddInventoryDirectory()
    {
        using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
        {
            Description = Strings.GetString("SelectInventoryDirectory"),
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(_settings.EqDirectory))
        {
            folderDialog.SelectedPath = _settings.EqDirectory;
        }

        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string selectedPath = folderDialog.SelectedPath;
            if (!selectedPath.EndsWith('\\'))
                selectedPath = selectedPath + "\\";

            if (!_settings.InventoryDirectories.Contains(selectedPath))
            {
                _settings.InventoryDirectories.Add(selectedPath);
                _settings.SaveSettings();
            }
        }
    }

    private void AddSelectedEqLogFileToArchive()
    {
        if (string.IsNullOrWhiteSpace(SelectedEqLogFileToAdd))
            return;

        if (SelectedEqLogFiles.Contains(SelectedEqLogFileToAdd))
            return;

        SelectedEqLogFiles.Add(SelectedEqLogFileToAdd);
        SelectedEqLogFiles = [.. SelectedEqLogFiles.Order()];
        RaisePropertyChanged(nameof(SelectedEqLogFiles));
    }

    private async void AggregateInventoryFiles()
        => await AggregateInventoryFilesExecute();

    private async Task AggregateInventoryFilesExecute()
    {
        Log.Debug($"{LogPrefix} Aggregating Inventory log files.");
        string fileName = Path.Combine(_settings.OutputDirectory, $"AggregatedInventory-{DateTime.Now:HH:mm:ss}.txt");
        await _inventoryParser.AggregateInventoryFromDirectories(_settings.InventoryDirectories, fileName);
    }

    private void ArchiveAttendanceFiles(TimeSpan maxAgeOfFile, string fileNamePrefix)
    {
        if (!ArchiveBasedOnAge)
            return;

        IEnumerable<string> logFilesToArchive = Directory.EnumerateFiles(_settings.EqDirectory, fileNamePrefix + "*.txt");
        foreach (string logFile in logFilesToArchive)
        {
            FileInfo fi = new(logFile);
            if (!fi.Exists)
                continue;

            if (DateTime.Now.Subtract(fi.LastWriteTime) > maxAgeOfFile)
            {
                string newFileLocation = Path.Combine(EqLogArchiveDirectory, fi.Name);
                MoveFile(fi.FullName, newFileLocation);
            }
        }
    }

    private void ArchiveEqLogFiles()
        => Task.Run(ArchiveEqLogFilesExecute);

    private void ArchiveEqLogFilesExecute()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EqLogArchiveDirectory))
            {
                MessageBox.Show(Strings.GetString("EqLogDirectoryErrorMessage"), Strings.GetString("EqLogDirectoryError"), MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"{LogPrefix} Error archiving log files - {nameof(EqLogArchiveDirectory)} is null or empty.");
                return;
            }

            int maxDays = 0;
            if (ArchiveBasedOnAge && !int.TryParse(EqLogArchiveFileAge, out maxDays))
            {
                MessageBox.Show(Strings.GetString("EqLogAgeErrorMessage"), Strings.GetString("EqLogAgeError"), MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"{LogPrefix} Error archiving log files - {nameof(EqLogArchiveFileAge)} is not a number.  Value is: {EqLogArchiveFileAge}.");
                return;
            }

            TimeSpan maxAgeOfFile = new(maxDays, 0, 0, 0);

            int maxSize = 0;
            if (ArchiveBasedOnSize && !int.TryParse(EqLogArchiveFileSize, out maxSize))
            {
                MessageBox.Show(Strings.GetString("EqLogSizeErrorMessage"), Strings.GetString("EqLogSizeError"), MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"{LogPrefix} Error archiving log files - {nameof(EqLogArchiveFileSize)} is not a number.  Value is: {EqLogArchiveFileSize}.");
                return;
            }

            maxSize = maxSize * 1024 * 1024;

            ArchiveLogFiles(maxAgeOfFile, maxSize);

            ArchiveAttendanceFiles(maxAgeOfFile, Constants.RaidDumpFileNameStart);
            ArchiveAttendanceFiles(maxAgeOfFile, Constants.RaidListFileNameStart);
            ArchiveAttendanceFiles(maxAgeOfFile, Constants.ZealAttendanceBasedFileName);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error archiving log and attendance files: {ex.ToLogMessage()}");
        }
    }

    private async void ArchiveGeneratedLogFiles()
        => await Task.Run(ArchiveGeneratedLogFilesExecute);

    private void ArchiveGeneratedLogFilesExecute()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(GeneratedLogsArchiveDirectory))
            {
                MessageBox.Show(Strings.GetString("GeneratedDirectoryErrorMessage"), Strings.GetString("GeneratedDirectoryError"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(GeneratedLogsArchiveFileAge, out int maxDays))
            {
                MessageBox.Show(Strings.GetString("GeneratedLogAgeErrorMessage"), Strings.GetString("GeneratedLogAgeError"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TimeSpan maxAgeOfFile = new(maxDays, 0, 0, 0);
            MoveOldFiles(_settings.OutputDirectory, Constants.GeneratedLogFileNamePrefix + "*.txt", maxAgeOfFile, GeneratedLogsArchiveDirectory);
            MoveOldFiles(_settings.OutputDirectory, Constants.FullGeneratedLogFileNamePrefix + "*.txt", maxAgeOfFile, GeneratedLogsArchiveDirectory);
            MoveOldFiles(_settings.OutputDirectory, Constants.ConversationFileNamePrefix + "*.txt", maxAgeOfFile, GeneratedLogsArchiveDirectory);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error archiving generated log files: {ex.ToLogMessage()}");
        }
    }

    private void ArchiveLogFiles(TimeSpan maxAgeOfFile, int maxSize)
    {
        if (!ArchiveBasedOnSize)
            return;

        IEnumerable<string> logFilesToArchive = SelectedEqLogFiles;
        if (IsAllLogsArchived)
        {
            logFilesToArchive = Directory.EnumerateFiles(_settings.EqDirectory, _settings.LogFileMatchPattern);
        }

        foreach (string logFile in logFilesToArchive)
        {
            FileInfo fi = new(logFile);
            if (!fi.Exists)
                continue;

            if (ArchiveBasedOnSize && fi.Length > maxSize)
            {
                string newFileName = $"{fi.Name[0..^4]}-{DateTime.Now.ToString(Constants.ArchiveFileNameTimeFormat)}.txt";
                string newFileLocation = Path.Combine(EqLogArchiveDirectory, newFileName);
                MoveFile(fi.FullName, newFileLocation);
            }
        }
    }

    private int GetIntValue(string inputValue)
    {
        if (int.TryParse(inputValue, out int parsedValue))
        {
            return parsedValue;
        }
        return 0;
    }

    private void OpenEqLogArchiveDirectory()
    {
        string directory = Path.GetDirectoryName(EqLogArchiveDirectory);
        Process.Start("explorer.exe", directory);
    }

    private void OpenGeneratedLogArchiveDirectory()
    {
        string directory = Path.GetDirectoryName(GeneratedLogsArchiveDirectory);
        Process.Start("explorer.exe", directory);
    }

    private void OpenInventoryGeneratedDirectory()
        => Process.Start("explorer.exe", _settings.OutputDirectory);

    private void PopulateAllLogFiles()
    {
        if (string.IsNullOrWhiteSpace(_settings.EqDirectory))
        {
            PossibleEqLogFiles = [];
            return;
        }

        IEnumerable<string> logFiles = Directory.EnumerateFiles(_settings.EqDirectory, _settings.LogFileMatchPattern);
        PossibleEqLogFiles = new List<string>(logFiles);
        if (PossibleEqLogFiles.Count > 0)
            SelectedEqLogFileToAdd = PossibleEqLogFiles.First();

        RaisePropertyChanged(nameof(PossibleEqLogFiles));
    }

    private void RemoveSelectedEqLogFile()
    {
        if (string.IsNullOrWhiteSpace(SelectedEqLogFileToRemove))
            return;

        SelectedEqLogFiles.Remove(SelectedEqLogFileToRemove);
        SelectedEqLogFiles = new List<string>(SelectedEqLogFiles.Order());
        RaisePropertyChanged(nameof(SelectedEqLogFiles));
    }

    private void SetEqLogArchiveDirectory()
    {
        using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
        {
            Description = Strings.GetString("SelectEqLogFileFolder"),
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(EqLogArchiveDirectory))
        {
            folderDialog.SelectedPath = EqLogArchiveDirectory;
        }

        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string selectedPath = folderDialog.SelectedPath;
            if (!selectedPath.EndsWith('\\'))
                selectedPath = selectedPath + "\\";
            EqLogArchiveDirectory = selectedPath;
        }
    }

    private void SetGeneratedLogArchiveDirectory()
    {
        using var folderDialog = new System.Windows.Forms.FolderBrowserDialog()
        {
            Description = Strings.GetString("SelectEqLogFileFolder"),
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(GeneratedLogsArchiveDirectory))
        {
            folderDialog.SelectedPath = GeneratedLogsArchiveDirectory;
        }

        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string selectedPath = folderDialog.SelectedPath;
            if (!selectedPath.EndsWith('\\'))
                selectedPath = selectedPath + "\\";
            GeneratedLogsArchiveDirectory = selectedPath;
        }
    }
}

public interface IFileArchiveDialogViewModel : IDialogViewModel
{
    DelegateCommand AddInventoryDirCommand { get; }

    DelegateCommand AddSelectedEqLogFileToArchiveCommand { get; }

    DelegateCommand AggregateInventoriesCommand { get; }

    bool ArchiveBasedOnAge { get; set; }

    bool ArchiveBasedOnSize { get; set; }

    DelegateCommand ArchiveEqLogFilesCommand { get; }

    DelegateCommand ArchiveGeneratedLogFilesCommand { get; }

    string EqLogArchiveDirectory { get; set; }

    string EqLogArchiveFileAge { get; set; }

    string EqLogArchiveFileSize { get; set; }

    string GeneratedLogsArchiveDirectory { get; set; }

    string GeneratedLogsArchiveFileAge { get; set; }

    bool IsAllLogsArchived { get; set; }

    bool IsSelectedLogsArchived { get; set; }

    DelegateCommand OpenEqLogArchiveDirectoryCommand { get; }

    DelegateCommand OpenGeneratedLogArchiveDirectoryCommand { get; }

    DelegateCommand OpenInventoryGeneratedFileDirectoryCommand { get; }

    ICollection<string> PossibleEqLogFiles { get; }

    DelegateCommand RemoveSelectedEqLogFileCommand { get; }

    ICollection<string> SelectedEqLogFiles { get; }

    string SelectedEqLogFileToAdd { get; set; }

    string SelectedEqLogFileToRemove { get; set; }

    DelegateCommand SetEqLogArchiveDirectoryCommand { get; }

    DelegateCommand SetGeneratedLogArchiveDirectoryCommand { get; }

    void UpdateSettings(IDkpParserSettings settings);
}
