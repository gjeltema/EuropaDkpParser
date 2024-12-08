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
    private const string DefaultMatchPattern = "*eqlog*.txt";
    private const int DefaultWindowLocation = 200;
    private const char Delimiter = '=';
    private const string DkpspentAucEnableSection = "DKPSPENT_AUC_ENABLE";
    private const string DkpspentGuildEnableSection = "DKPSPENT_GU_ENABLE";
    private const string DkpspentOocEnableSection = "DKPSPENT_OOC_ENABLE";
    private const string EnableDebugOptionsSection = "ENABLE_DEBUG";
    private const string EnableDkpBonusAttendance = "ENABLE_DKP_BONUS";
    private const string EqDirectorySection = "EQ_DIRECTORY";
    private const string IncludeTellsInRawLogSection = "INCLUDE_TELLS_IN_RAW_LOG";
    private const string LogMatchPatternSection = "LOG_MATCH_PATTERN";
    private const string OutputDirectorySection = "OUTPUT_DIRECTORY";
    private const string SectionEnding = "_END";
    private const string SelectedLogFilesSection = "SELECTED_LOG_FILES";
    private const string ShowAfkReviewSection = "SHOW_AFK_REVIEW";
    private const string UseAdvancedDialogSection = "USE_ADVANCED_DIALOG";
    private const string UseLightModeSection = "USE_LIGHT_MODE";
    private const string WindowLocationSection = "WINDOW_LOCATION";
    private readonly string _dkpCharactersFileName;
    private readonly string _itemLinkValuesFileName;
    private readonly string _raidValuesFileName;
    private readonly string _settingsFilePath;

    public DkpParserSettings(string settingsFilePath, string raidValuesFileName, string itemLinkValuesFileName, string dkpCharactersFileName)
    {
        _settingsFilePath = settingsFilePath;
        _raidValuesFileName = raidValuesFileName;
        _itemLinkValuesFileName = itemLinkValuesFileName;
        _dkpCharactersFileName = dkpCharactersFileName;
    }

    public bool AddBonusDkpRaid { get; set; }

    public string ApiReadToken { get; set; }

    public string ApiUrl { get; set; }

    public string ApiWriteToken { get; set; }

    public bool ArchiveAllEqLogFiles { get; set; }

    public ICollection<string> BossMobs { get; private set; } = [];

    public DkpServerCharacters CharactersOnDkpServer { get; private set; }

    public bool DkpspentAucEnabled { get; set; }

    public bool DkpspentGuEnabled { get; set; }

    public bool DkpspentOocEnabled { get; set; }

    public bool EnableDebugOptions { get; set; }

    public string EqDirectory { get; set; } = string.Empty;

    public int EqLogFileAgeToArchiveInDays { get; set; }

    public string EqLogFileArchiveDirectory { get; set; }

    public int EqLogFileSizeToArchiveInMBs { get; set; }

    public ICollection<string> EqLogFilesToArchive { get; set; } = [];

    public int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    public string GeneratedLogFilesArchiveDirectory { get; set; }

    public bool IncludeTellsInRawLog { get; set; }

    public bool IsApiConfigured
        => !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(ApiReadToken) && !string.IsNullOrEmpty(ApiWriteToken);

    public ItemLinkValues ItemLinkIds { get; private set; }

    public string LogFileMatchPattern { get; set; }

    public int MainWindowX { get; set; } = DefaultWindowLocation;

    public int MainWindowY { get; set; } = DefaultWindowLocation;

    public string OutputDirectory { get; set; }

    public IRaidValues RaidValue { get; private set; }

    public ICollection<string> SelectedLogFiles { get; set; } = [];

    public bool ShowAfkReview { get; set; }

    public bool UseAdvancedDialog { get; set; }

    public bool UseLightMode { get; set; }

    public void LoadAllSettings()
    {
        RaidValue = new RaidValues(_raidValuesFileName);
        RaidValue.LoadSettings();

        ItemLinkIds = new(_itemLinkValuesFileName);
        ItemLinkIds.LoadValues();

        CharactersOnDkpServer = new(_dkpCharactersFileName);
        CharactersOnDkpServer.LoadValues();

        if (!File.Exists(_settingsFilePath))
        {
            SaveSettings();
            return;
        }

        string[] fileContents = File.ReadAllLines(_settingsFilePath);
        SetWindowLocation(fileContents);
        SetDirectories(fileContents);
        SetSimpleArchiveSettings(fileContents);

        SelectedLogFiles = GetAllEntriesInSection(fileContents, SelectedLogFilesSection);
        EnableDebugOptions = GetBoolValue(fileContents, EnableDebugOptionsSection);
        ApiReadToken = GetStringValue(fileContents, ApiReadTokenSection);
        if (string.IsNullOrWhiteSpace(ApiReadToken))
            ApiReadToken = "";
        ApiWriteToken = GetStringValue(fileContents, ApiWriteTokenSection);
        ApiUrl = GetStringValue(fileContents, ApiUrlSection);
        if (string.IsNullOrWhiteSpace(ApiUrl))
            ApiUrl = "";
        AddBonusDkpRaid = GetBoolValue(fileContents, EnableDkpBonusAttendance);
        ShowAfkReview = GetBoolValue(fileContents, ShowAfkReviewSection);
        LogFileMatchPattern = GetStringValue(fileContents, LogMatchPatternSection, DefaultMatchPattern);
        UseAdvancedDialog = GetBoolValue(fileContents, UseAdvancedDialogSection);
        IncludeTellsInRawLog = GetBoolValue(fileContents, IncludeTellsInRawLogSection);
        DkpspentAucEnabled = GetBoolValue(fileContents, DkpspentAucEnableSection, true);
        DkpspentGuEnabled = GetBoolValue(fileContents, DkpspentGuildEnableSection, true);
        DkpspentOocEnabled = GetBoolValue(fileContents, DkpspentOocEnableSection, true);
        UseLightMode = GetBoolValue(fileContents, UseLightModeSection);
    }

    public void SaveSettings()
    {
        var settingsFileContent = new List<string>(50)
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
            CreateFileEntry(EnableDkpBonusAttendance, AddBonusDkpRaid),
            CreateFileEntry(ShowAfkReviewSection, ShowAfkReview),
            CreateFileEntry(LogMatchPatternSection, LogFileMatchPattern),
            CreateFileEntry(UseAdvancedDialogSection, UseAdvancedDialog),
            CreateFileEntry(IncludeTellsInRawLogSection, IncludeTellsInRawLog),
            CreateFileEntry(DkpspentAucEnableSection, DkpspentAucEnabled),
            CreateFileEntry(DkpspentGuildEnableSection, DkpspentGuEnabled),
            CreateFileEntry(DkpspentOocEnableSection, DkpspentOocEnabled),
            CreateFileEntry(UseLightModeSection, UseLightMode)
        };

        AddCollection(settingsFileContent, SelectedLogFiles, SelectedLogFilesSection);

        AddCollection(settingsFileContent, EqLogFilesToArchive, ArchiveSelectedFilesToArchiveSection);

        try
        {
            File.WriteAllLines(_settingsFilePath, settingsFileContent);
        }
        catch { }
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

        // Keeping this around for legacy settings files, when all settings were full sections, even simple bools.
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

    private void SetDirectories(string[] fileContents)
    {
        EqDirectory = GetStringValue(fileContents, EqDirectorySection);

        OutputDirectory = GetStringValue(fileContents, OutputDirectorySection);
        if (string.IsNullOrWhiteSpace(OutputDirectory) && !string.IsNullOrWhiteSpace(EqDirectory))
        {
            OutputDirectory = EqDirectory;
        }
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

    DkpServerCharacters CharactersOnDkpServer { get; }

    bool DkpspentAucEnabled { get; set; }

    bool DkpspentGuEnabled { get; set; }

    bool DkpspentOocEnabled { get; set; }

    bool EnableDebugOptions { get; set; }

    string EqDirectory { get; set; }

    int EqLogFileAgeToArchiveInDays { get; set; }

    string EqLogFileArchiveDirectory { get; set; }

    int EqLogFileSizeToArchiveInMBs { get; set; }

    ICollection<string> EqLogFilesToArchive { get; set; }

    int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    string GeneratedLogFilesArchiveDirectory { get; set; }

    bool IncludeTellsInRawLog { get; set; }

    bool IsApiConfigured { get; }

    ItemLinkValues ItemLinkIds { get; }

    string LogFileMatchPattern { get; set; }

    int MainWindowX { get; set; }

    int MainWindowY { get; set; }

    string OutputDirectory { get; set; }

    IRaidValues RaidValue { get; }

    ICollection<string> SelectedLogFiles { get; set; }

    bool ShowAfkReview { get; set; }

    bool UseAdvancedDialog { get; set; }

    bool UseLightMode { get; set; }

    void LoadAllSettings();

    void SaveSettings();
}
