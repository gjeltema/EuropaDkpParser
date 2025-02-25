// -----------------------------------------------------------------------
// DkpParserSettings.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;
using Gjeltema.Logging;

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
    private const string DkpspentGuildEnableSection = "DKPSPENT_GU_ENABLE";
    private const string EnableDebugOptionsSection = "ENABLE_DEBUG";
    private const string EnableDkpBonusAttendance = "ENABLE_DKP_BONUS";
    private const string EqDirectorySection = "EQ_DIRECTORY";
    private const string IncludeTellsInRawLogSection = "INCLUDE_TELLS_IN_RAW_LOG";
    private const string LogLevelSection = "LOG_LEVEL";
    private const string LogMatchPatternSection = "LOG_MATCH_PATTERN";
    private const string LogPrefix = $"[{nameof(DkpParserSettings)}]";
    private const string OutputDirectorySection = "OUTPUT_DIRECTORY";
    private const string OverlayFontColorSection = "OVERLAY_FONT_COLOR";
    private const string OverlayFontSizeSection = "OVERLAY_FONT_SIZE";
    private const string OverlayLocationXSection = "OVERLAY_LOCATION_X";
    private const string OverlayLocationYSection = "OVERLAY_LOCATION_Y";
    private const string SectionEnding = "_END";
    private const string SelectedLogFilesSection = "SELECTED_LOG_FILES";
    private const string ShowAfkReviewSection = "SHOW_AFK_REVIEW";
    private const string UseLightModeSection = "USE_LIGHT_MODE";
    private const string WindowLocationSection = "WINDOW_LOCATION";
    private readonly string _dkpCharactersFileName;
    private readonly string _itemLinkValuesFileName;
    private readonly string _raidValuesFileName;
    private readonly string _settingsFilePath;
    private readonly string _zoneIdMapFileName;

    public DkpParserSettings(string settingsFilePath, string raidValuesFileName, string itemLinkValuesFileName, string dkpCharactersFileName, string zoneIdMapFileName)
    {
        _settingsFilePath = settingsFilePath;
        _raidValuesFileName = raidValuesFileName;
        _itemLinkValuesFileName = itemLinkValuesFileName;
        _dkpCharactersFileName = dkpCharactersFileName;
        _zoneIdMapFileName = zoneIdMapFileName;
    }

    public bool AddBonusDkpRaid { get; set; }

    public string ApiReadToken { get; set; }

    public string ApiUrl { get; set; }

    public string ApiWriteToken { get; set; }

    public bool ArchiveAllEqLogFiles { get; set; }

    public ICollection<string> BossMobs { get; private set; } = [];

    public DkpServerCharacters CharactersOnDkpServer { get; private set; }

    public bool DkpspentGuEnabled { get; set; }

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

    public LogLevel LoggingLevel { get; set; }

    public int MainWindowX { get; set; } = DefaultWindowLocation;

    public int MainWindowY { get; set; } = DefaultWindowLocation;

    public string OutputDirectory { get; set; }

    public string OverlayFontColor { get; set; }

    public int OverlayFontSize { get; set; }

    public int OverlayLocationX { get; set; }

    public int OverlayLocationY { get; set; }

    public IRaidValues RaidValue { get; private set; }

    public ICollection<string> SelectedLogFiles { get; set; } = [];

    public bool ShowAfkReview { get; set; }

    public bool UseLightMode { get; set; }

    public IDictionary<int, string> ZoneIdMapping { get; private set; }

    public void LoadAllSettings()
    {
        LoadBaseSettings();

        LoadOtherFileSettings();
    }

    public bool LoadBaseSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            SaveSettings();
            return false;
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
        IncludeTellsInRawLog = GetBoolValue(fileContents, IncludeTellsInRawLogSection);
        DkpspentGuEnabled = GetBoolValue(fileContents, DkpspentGuildEnableSection, true);
        UseLightMode = GetBoolValue(fileContents, UseLightModeSection);

        OverlayLocationX = GetIntValue(fileContents, OverlayLocationXSection, 100);
        OverlayLocationY = GetIntValue(fileContents, OverlayLocationYSection, 100);
        OverlayFontColor = GetStringValue(fileContents, OverlayFontColorSection, "#CCCCCC");
        OverlayFontSize = GetIntValue(fileContents, OverlayFontSizeSection, 20);

        int loggingLevelRaw = GetIntValue(fileContents, LogLevelSection, (int)LogLevel.Error);
        LoggingLevel = (LogLevel)loggingLevelRaw;

        return true;
    }

    public void LoadOtherFileSettings()
    {
        RaidValue = new RaidValues(_raidValuesFileName);
        RaidValue.LoadSettings();

        ItemLinkIds = new(_itemLinkValuesFileName);
        ItemLinkIds.LoadValues();

        CharactersOnDkpServer = new(_dkpCharactersFileName);
        CharactersOnDkpServer.LoadValues();

        InitializeZoneIdMapping();
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
            CreateFileEntry(IncludeTellsInRawLogSection, IncludeTellsInRawLog),
            CreateFileEntry(DkpspentGuildEnableSection, DkpspentGuEnabled),
            CreateFileEntry(UseLightModeSection, UseLightMode),
            CreateFileEntry(OverlayLocationXSection, OverlayLocationX),
            CreateFileEntry(OverlayLocationYSection, OverlayLocationY),
            CreateFileEntry(OverlayFontColorSection, OverlayFontColor),
            CreateFileEntry(OverlayFontSizeSection, OverlayFontSize),
            CreateFileEntry(LogLevelSection, (int)LoggingLevel),
        };

        AddCollection(settingsFileContent, SelectedLogFiles, SelectedLogFilesSection);

        AddCollection(settingsFileContent, EqLogFilesToArchive, ArchiveSelectedFilesToArchiveSection);

        try
        {
            File.WriteAllLines(_settingsFilePath, settingsFileContent);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error writing to file {_settingsFilePath}: {ex.ToLogMessage()}");
        }
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

    private void InitializeZoneIdMapping()
    {
        if (!File.Exists(_zoneIdMapFileName))
        {
            Log.Error($"{LogPrefix} ZoneID file does not exist: {_zoneIdMapFileName}");
            return;
        }

        string[] fileContents = File.ReadAllLines(_zoneIdMapFileName);
        ZoneIdMapping = new Dictionary<int, string>(fileContents.Length);

        foreach (string zoneIdMapping in fileContents)
        {
            string[] parts = zoneIdMapping.Split('|');
            string zoneName = parts[1];
            int zoneId = int.Parse(parts[2]);
            ZoneIdMapping[zoneId] = zoneName;
        }
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

    bool DkpspentGuEnabled { get; set; }

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

    LogLevel LoggingLevel { get; set; }

    int MainWindowX { get; set; }

    int MainWindowY { get; set; }

    string OutputDirectory { get; set; }

    string OverlayFontColor { get; set; }

    int OverlayFontSize { get; set; }

    int OverlayLocationX { get; set; }

    int OverlayLocationY { get; set; }

    IRaidValues RaidValue { get; }

    ICollection<string> SelectedLogFiles { get; set; }

    bool ShowAfkReview { get; set; }

    bool UseLightMode { get; set; }

    IDictionary<int, string> ZoneIdMapping { get; }

    void LoadAllSettings();

    bool LoadBaseSettings();

    void LoadOtherFileSettings();

    void SaveSettings();
}
