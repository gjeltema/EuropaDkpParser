// -----------------------------------------------------------------------
// DkpParserSettings.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class DkpParserSettings : IDkpParserSettings
{
    private const string ApiReadTokenSection = "API_READ_TOKEN";
    private const string ApiUrlSection = "API_URL";
    private const string ApiWriteTokenSection = "API_WRITE_TOKEN";
    private const string ArchiveAllOrSelectedEqLogFileSection = "ARCHIVE_ALL_EQ_LOG_FILES";
    private const string ArchiveEqLogFileAgeSection = "ARCHIVE_EQ_LOG_FILE_AGE";
    private const string ArchiveEqLogFileDirectorySection = "ARCHIVE_EQ_LOG_FILE_DIRECTORY";
    private const string ArchiveEqLogFilesSizeSection = "ARCHIVE_EQ_LOG_FILE_SIZE";
    private const string ArchiveGeneratedLogFileAgeSection = "ARCHIVE_GEN_LOGS_AGE";
    private const string ArchiveGeneratedLogFileDirectorySection = "ARCHIVE_GEN_LOG_DIRECTORY";
    private const string ArchiveSelectedFilesToArchiveSection = "ARCHIVE_SELECTED_EQ_LOG_FILES";
    private const int DefaultWindowLocation = 200;
    private const char Delimiter = '=';
    private const string EnableDebugOptionsSection = "ENABLE_DEBUG";
    private const string EnableDkpBonusAttendance = "ENABLE_DKP_BONUS";
    private const string EqDirectorySection = "EQ_DIRECTORY";
    private const string OutputDirectorySection = "OUTPUT_DIRECTORY";
    private const string SectionEnding = "_END";
    private const string SelectedLogFilesSection = "SELECTED_LOG_FILES";
    private const string WindowLocationSection = "WINDOW_LOCATION";
    private readonly string _raidValuesFileName;
    private readonly string _settingsFilePath;

    public DkpParserSettings(string settingsFilePath, string raidValuesFileName)
    {
        _settingsFilePath = settingsFilePath;
        _raidValuesFileName = raidValuesFileName;
    }

    public bool AddBonusDkpRaid { get; set; }

    public string ApiReadToken { get; set; }

    public string ApiUrl { get; set; }

    public string ApiWriteToken { get; set; }

    public bool ArchiveAllEqLogFiles { get; set; }

    public ICollection<string> BossMobs { get; private set; } = [];

    public bool EnableDebugOptions { get; set; }

    public string EqDirectory { get; set; } = string.Empty;

    public int EqLogFileAgeToArchiveInDays { get; set; }

    public string EqLogFileArchiveDirectory { get; set; }

    public int EqLogFileSizeToArchiveInMBs { get; set; }

    public ICollection<string> EqLogFilesToArchive { get; set; } = [];

    public int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    public string GeneratedLogFilesArchiveDirectory { get; set; }

    public bool IsApiConfigured
        => !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(ApiReadToken) && !string.IsNullOrEmpty(ApiWriteToken);

    public int MainWindowX { get; set; } = DefaultWindowLocation;

    public int MainWindowY { get; set; } = DefaultWindowLocation;

    public string OutputDirectory { get; set; }

    public IRaidValues RaidValue { get; private set; }

    public ICollection<string> SelectedLogFiles { get; set; } = [];

    public void LoadSettings()
    {
        RaidValue = new RaidValues(_raidValuesFileName);
        RaidValue.LoadSettings();

        if (!File.Exists(_settingsFilePath))
        {
            SaveSettings();
            return;
        }

        string[] fileContents = File.ReadAllLines(_settingsFilePath);
        SetWindowLocation(fileContents);
        SetEqDirectory(fileContents);
        SetOutputDirectory(fileContents);
        SetSelectedLogFiles(fileContents);
        SetSimpleArchiveSettings(fileContents);
        SetDebugOptions(fileContents);
        SetApiValues(fileContents);
        SetMiscSettings(fileContents);
    }

    public void SaveSettings()
    {
        var settingsFileContent = new List<string>(8)
        {
            WindowLocationSection,
            MainWindowX.ToString(),
            MainWindowY.ToString(),
            WindowLocationSection + SectionEnding,
            CreateFileEntry(EqDirectorySection, EqDirectory),
            CreateFileEntry(OutputDirectorySection, OutputDirectory),
            CreateFileEntry(EnableDebugOptionsSection, EnableDebugOptions),
            CreateFileEntry(ArchiveAllOrSelectedEqLogFileSection, ArchiveAllEqLogFiles),
            CreateFileEntry(ArchiveEqLogFileDirectorySection, EqLogFileArchiveDirectory),
            CreateFileEntry(ArchiveEqLogFileAgeSection, EqLogFileAgeToArchiveInDays),
            CreateFileEntry(ArchiveEqLogFilesSizeSection, EqLogFileSizeToArchiveInMBs),
            CreateFileEntry(ArchiveGeneratedLogFileDirectorySection, GeneratedLogFilesArchiveDirectory),
            CreateFileEntry(ArchiveGeneratedLogFileAgeSection, GeneratedLogFilesAgeToArchiveInDays),
            CreateFileEntry(ApiReadTokenSection, ApiReadToken),
            CreateFileEntry(ApiWriteTokenSection, ApiWriteToken),
            CreateFileEntry(ApiUrlSection, ApiUrl),
            CreateFileEntry(EnableDkpBonusAttendance, AddBonusDkpRaid)
        };

        AddCollection(settingsFileContent, SelectedLogFiles, SelectedLogFilesSection);

        AddCollection(settingsFileContent, EqLogFilesToArchive, ArchiveSelectedFilesToArchiveSection);

        File.WriteAllLines(_settingsFilePath, settingsFileContent);
    }

    private static int GetStartingIndex(string[] fileContents, string configurationSectionName)
        => Array.FindIndex(fileContents, x => x.StartsWith(configurationSectionName));

    private void AddCollection(List<string> settingsFileContent, IEnumerable<string> contentsToAdd, string sectionName)
    {
        settingsFileContent.Add(sectionName);

        settingsFileContent.AddRange(contentsToAdd);

        settingsFileContent.Add(sectionName + SectionEnding);
    }

    private string CreateFileEntry<T>(string settingName, T settingValue)
        => $"{settingName}{Delimiter}{settingValue}";

    private ICollection<string> GetAllEntriesInSection(string[] fileContents, string key)
    {
        List<string> entries = new();
        int index = GetStartingIndex(fileContents, key);
        if (!IsValidIndex(index, fileContents))
            return entries;

        string sectionEnd = key + SectionEnding;
        index++;
        string entry = fileContents[index];
        while (entry != sectionEnd)
        {
            entries.Add(entry);
            index++;
            entry = fileContents[index];
        }

        return entries;
    }

    private bool GetBoolValue(string[] fileContents, string key, bool defaultValue = false)
    {
        int index = GetStartingIndex(fileContents, key);
        if (!IsValidIndex(index, fileContents))
            return defaultValue;

        string setting = fileContents[index];
        string[] split = setting.Split(Delimiter);
        if (split.Length > 1)
            return split[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase);

        index++;
        string settingValue = fileContents[index];
        if (settingValue == key + SectionEnding)
            return defaultValue;

        return settingValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
    }

    private int GetIntValue(string[] fileContents, string key, int defaultValue = 0)
    {
        int index = GetStartingIndex(fileContents, key);
        if (!IsValidIndex(index, fileContents))
            return defaultValue;

        string setting = fileContents[index];
        string[] split = setting.Split(Delimiter);
        if (split.Length > 1)
        {
            if (int.TryParse(split[1], out int parsedValue))
            {
                return parsedValue;
            }
            else
            {
                return defaultValue;
            }
        }

        index++;
        string settingValue = fileContents[index];
        if (settingValue == key + SectionEnding)
            return defaultValue;

        if (int.TryParse(settingValue, out int intValue))
        {
            return intValue;
        }

        return defaultValue;
    }

    private string GetStringValue(string[] fileContents, string key, string defaultValue = "")
    {
        int index = GetStartingIndex(fileContents, key);
        if (!IsValidIndex(index, fileContents))
            return defaultValue;

        string setting = fileContents[index];
        string[] split = setting.Split(Delimiter);
        if (split.Length > 1)
        {
            return split[1];
        }

        index++;
        string settingValue = fileContents[index];

        if (settingValue == key + SectionEnding)
            return defaultValue;

        return settingValue;
    }

    private int GetWindowLoc(string setting)
    {
        if (setting == WindowLocationSection + SectionEnding)
            return DefaultWindowLocation;

        if (int.TryParse(setting, out int windowLoc))
        {
            return windowLoc;
        }

        return DefaultWindowLocation;
    }

    private bool IsValidIndex(int indexOfSection, ICollection<string> fileContents)
        => 0 <= indexOfSection && indexOfSection < fileContents.Count - 1;

    private void SetApiValues(string[] fileContents)
    {
        ApiReadToken = GetStringValue(fileContents, ApiReadTokenSection);
        ApiWriteToken = GetStringValue(fileContents, ApiWriteTokenSection);
        ApiUrl = GetStringValue(fileContents, ApiUrlSection);
    }

    private void SetDebugOptions(string[] fileContents)
    {
        EnableDebugOptions = GetBoolValue(fileContents, EnableDebugOptionsSection);
    }

    private void SetEqDirectory(string[] fileContents)
    {
        EqDirectory = GetStringValue(fileContents, EqDirectorySection);
    }

    private void SetMiscSettings(string[] fileContents)
    {
        AddBonusDkpRaid = GetBoolValue(fileContents, EnableDkpBonusAttendance);
    }

    private void SetOutputDirectory(string[] fileContents)
    {
        OutputDirectory = GetStringValue(fileContents, OutputDirectorySection);
        if (string.IsNullOrWhiteSpace(OutputDirectory) && !string.IsNullOrWhiteSpace(EqDirectory))
        {
            OutputDirectory = EqDirectory;
        }
    }

    private void SetSelectedLogFiles(string[] fileContents)
    {
        SelectedLogFiles = GetAllEntriesInSection(fileContents, SelectedLogFilesSection);
    }

    private void SetSimpleArchiveSettings(string[] fileContents)
    {
        EqLogFileArchiveDirectory = GetStringValue(fileContents, ArchiveEqLogFileDirectorySection);
        GeneratedLogFilesArchiveDirectory = GetStringValue(fileContents, ArchiveGeneratedLogFileDirectorySection);
        EqLogFileAgeToArchiveInDays = GetIntValue(fileContents, ArchiveEqLogFileAgeSection);
        EqLogFileSizeToArchiveInMBs = GetIntValue(fileContents, ArchiveEqLogFilesSizeSection);
        GeneratedLogFilesAgeToArchiveInDays = GetIntValue(fileContents, ArchiveGeneratedLogFileAgeSection);

        ArchiveAllEqLogFiles = GetBoolValue(fileContents, ArchiveAllOrSelectedEqLogFileSection, true);

        EqLogFilesToArchive = GetAllEntriesInSection(fileContents, ArchiveSelectedFilesToArchiveSection);
    }

    private void SetWindowLocation(string[] fileContents)
    {
        int index = GetStartingIndex(fileContents, WindowLocationSection);
        if (!IsValidIndex(index, fileContents))
            return;

        index++;
        string setting = fileContents[index];
        MainWindowX = GetWindowLoc(setting);
        index++;
        setting = fileContents[index];
        MainWindowY = GetWindowLoc(setting);
    }
}

public interface IDkpParserSettings
{
    bool AddBonusDkpRaid { get; set; }

    string ApiReadToken { get; set; }

    string ApiUrl { get; set; }

    string ApiWriteToken { get; set; }

    bool ArchiveAllEqLogFiles { get; set; }

    bool EnableDebugOptions { get; set; }

    string EqDirectory { get; set; }

    int EqLogFileAgeToArchiveInDays { get; set; }

    string EqLogFileArchiveDirectory { get; set; }

    int EqLogFileSizeToArchiveInMBs { get; set; }

    ICollection<string> EqLogFilesToArchive { get; set; }

    int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    string GeneratedLogFilesArchiveDirectory { get; set; }

    bool IsApiConfigured { get; }

    int MainWindowX { get; set; }

    int MainWindowY { get; set; }

    string OutputDirectory { get; set; }

    IRaidValues RaidValue { get; }

    ICollection<string> SelectedLogFiles { get; set; }

    void LoadSettings();

    void SaveSettings();
}
