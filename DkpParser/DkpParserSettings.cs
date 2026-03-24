// -----------------------------------------------------------------------
// DkpParserSettings.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
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
    private const string AuctionOverlayHeightSection = "AUCTION_OVERLAY_HEIGHT";
    private const string AuctionOverlayWidthSection = "AUCTION_OVERLAY_WIDTH";
    private const string AuctionOverlayXLocSection = "AUCTION_OVERLAY_X";
    private const string AuctionOverlayYLocSection = "AUCTION_OVERLAY_Y";
    private const string DefaultMatchPattern = "*eqlog*.txt";
    private const string DefaultOverlayFontColor = "#CCCCCC";
    private const int DefaultWindowLocation = 200;
    private const char Delimiter = '=';
    private const string DkpspentGuildEnableSection = "DKPSPENT_GU_ENABLE";
    private const string EnableDkpBonusAttendance = "ENABLE_DKP_BONUS";
    private const string EnableZealDetailLoggingSection = "ENABLE_ZEAL_DETAIL_LOGS";
    private const string EqDirectorySection = "EQ_DIRECTORY";
    private const string IncludeTellsInRawLogSection = "INCLUDE_TELLS_IN_RAW_LOG";
    private const string InventoryDirectoriesSection = "INVENTORY_DIRECTORIES";
    private const string LogLevelSection = "LOG_LEVEL";
    private const string LogMatchPatternSection = "LOG_MATCH_PATTERN";
    private const string LogPrefix = $"[{nameof(DkpParserSettings)}]";
    private const string MezBreaksToShowSection = "MEZ_BREAKS_TO_SHOW";
    private const string OutputDirectorySection = "OUTPUT_DIRECTORY";
    private const string OverlayFontColorSection = "OVERLAY_FONT_COLOR";
    private const string OverlayFontSizeSection = "OVERLAY_FONT_SIZE";
    private const string OverlayLocationXSection = "OVERLAY_LOCATION_X";
    private const string OverlayLocationYSection = "OVERLAY_LOCATION_Y";
    private const string ReadyCheckHeightSection = "READY_CHECK_HEIGHT";
    private const string ReadyCheckWidthSection = "READY_CHECK_WIDTH";
    private const string ReadyCheckXLocSection = "READY_CHECK_X";
    private const string ReadyCheckYLocSection = "READY_CHECK_Y";
    private const string SectionEnding = "_END";
    private const string SelectedLogFilesSection = "SELECTED_LOG_FILES";
    private const string ShowAfkReviewSection = "SHOW_AFK_REVIEW";
    private const string SpellTrackerSection = "SPELL_TRACKING";
    private const string SpellTrackerXLocSection = "SPELL_TRACKER_X";
    private const string SpellTrackerYLocSection = "SPELL_TRACKER_Y";
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

    public int AuctionOverlayHeight { get; set; }

    public int AuctionOverlayWidth { get; set; }

    public int AuctionOverlayXLoc { get; set; }

    public int AuctionOverlayYLoc { get; set; }

    public DkpServerCharacters CharactersOnDkpServer { get; private set; }

    public bool DkpspentGuEnabled { get; set; } = true;

    public bool EnableZealDetailLogging { get; set; }

    public string EqDirectory { get; set; } = string.Empty;

    public int EqLogFileAgeToArchiveInDays { get; set; } = 90;

    public string EqLogFileArchiveDirectory { get; set; }

    public int EqLogFileSizeToArchiveInMBs { get; set; } = 1000;

    public ICollection<string> EqLogFilesToArchive { get; set; } = [];

    public int GeneratedLogFilesAgeToArchiveInDays { get; set; } = 30;

    public string GeneratedLogFilesArchiveDirectory { get; set; }

    public bool IncludeTellsInRawLog { get; set; }

    public ICollection<string> InventoryDirectories { get; private set; } = [];

    public bool IsApiConfigured
        => !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(ApiReadToken) && !string.IsNullOrEmpty(ApiWriteToken);

    public ItemLinkValues ItemLinkIds { get; private set; }

    public string LogFileMatchPattern { get; set; } = DefaultMatchPattern;

    public LogLevel LoggingLevel { get; set; } = LogLevel.Info;

    public int MainWindowX { get; set; } = DefaultWindowLocation;

    public int MainWindowY { get; set; } = DefaultWindowLocation;

    public int MezBreaksToShow { get; set; } = 4;

    public string OutputDirectory { get; set; }

    public string OverlayFontColor { get; set; } = DefaultOverlayFontColor;

    public int OverlayFontSize { get; set; } = 20;

    public int OverlayLocationX { get; set; } = 100;

    public int OverlayLocationY { get; set; } = 100;

    public IRaidValues RaidValue { get; private set; }

    public int ReadyCheckOverlayHeight { get; set; }

    public int ReadyCheckOverlayWidth { get; set; }

    public int ReadyCheckOverlayXLoc { get; set; }

    public int ReadyCheckOverlayYLoc { get; set; }

    public ICollection<string> SelectedLogFiles { get; set; } = [];

    public bool ShowAfkReview { get; set; }

    public ICollection<SpellTrackingConfiguration> SpellTrackers { get; } = [];

    public int SpellTrackerXLoc { get; set; }

    public int SpellTrackerYLoc { get; set; }

    public bool UseLightMode { get; set; }

    public IDictionary<int, string> ZoneIdMapping { get; private set; }

    public string GetCharacterNameFromLogFileName(string logFilePath)
    {
        string[] parts = logFilePath.Split('_');
        if (parts.Length == 3)
        {
            string fileCharName = parts[1];
            return fileCharName;
        }

        return null;
    }

    public string GetLogFileForCharacter(string characterName)
    {
        string logFilePath = SelectedLogFiles.FirstOrDefault(x => LogFileCharNameMatchesCharName(x, characterName));
        return logFilePath;
    }

    public void LoadAllSettings()
    {
        LoadBaseSettings();

        LoadOtherFileSettings();
    }

    public bool LoadBaseSettings()
    {
        bool fileExists = File.Exists(_settingsFilePath);
        if (!fileExists)
        {
            SaveSettings();
        }

        string[] fileContents = File.ReadAllLines(_settingsFilePath);
        SetWindowLocation(fileContents);
        SetDirectories(fileContents);
        SetSimpleArchiveSettings(fileContents);
        SetSpellTrackers(fileContents);

        InventoryDirectories = GetAllEntriesInSection(fileContents, InventoryDirectoriesSection);
        SelectedLogFiles = GetAllEntriesInSection(fileContents, SelectedLogFilesSection);

        ApiReadToken = GetStringValue(fileContents, ApiReadTokenSection);
        if (string.IsNullOrWhiteSpace(ApiReadToken))
            ApiReadToken = "";
        ApiWriteToken = GetStringValue(fileContents, ApiWriteTokenSection);

        SetApiUrl(fileContents);
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
        OverlayFontColor = GetStringValue(fileContents, OverlayFontColorSection, DefaultOverlayFontColor);
        OverlayFontSize = GetIntValue(fileContents, OverlayFontSizeSection, 20);

        AuctionOverlayXLoc = GetIntValue(fileContents, AuctionOverlayXLocSection, 100);
        AuctionOverlayYLoc = GetIntValue(fileContents, AuctionOverlayYLocSection, 100);
        AuctionOverlayWidth = GetIntValue(fileContents, AuctionOverlayWidthSection, 430);
        AuctionOverlayHeight = GetIntValue(fileContents, AuctionOverlayHeightSection, 410);

        ReadyCheckOverlayXLoc = GetIntValue(fileContents, ReadyCheckXLocSection, 100);
        ReadyCheckOverlayYLoc = GetIntValue(fileContents, ReadyCheckYLocSection, 100);
        ReadyCheckOverlayHeight = GetIntValue(fileContents, ReadyCheckHeightSection, 300);
        ReadyCheckOverlayWidth = GetIntValue(fileContents, ReadyCheckWidthSection, 400);

        EnableZealDetailLogging = GetBoolValue(fileContents, EnableZealDetailLoggingSection, false);

        int loggingLevelRaw = GetIntValue(fileContents, LogLevelSection, (int)LogLevel.Info);
        LoggingLevel = (LogLevel)loggingLevelRaw;

        MezBreaksToShow = GetIntValue(fileContents, MezBreaksToShowSection, 4);

        return fileExists;
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
            CreateFileEntry(AuctionOverlayXLocSection, AuctionOverlayXLoc),
            CreateFileEntry(AuctionOverlayYLocSection, AuctionOverlayYLoc),
            CreateFileEntry(AuctionOverlayWidthSection, AuctionOverlayWidth),
            CreateFileEntry(AuctionOverlayHeightSection, AuctionOverlayHeight),
            CreateFileEntry(ReadyCheckXLocSection, ReadyCheckOverlayXLoc),
            CreateFileEntry(ReadyCheckYLocSection, ReadyCheckOverlayYLoc),
            CreateFileEntry(ReadyCheckHeightSection, ReadyCheckOverlayHeight),
            CreateFileEntry(ReadyCheckWidthSection, ReadyCheckOverlayWidth),
            CreateFileEntry(MezBreaksToShowSection, MezBreaksToShow),
            CreateFileEntry(SpellTrackerXLocSection, SpellTrackerXLoc) ,
            CreateFileEntry(SpellTrackerYLocSection, SpellTrackerYLoc) ,
            CreateFileEntry(EnableZealDetailLoggingSection, EnableZealDetailLogging),
            CreateFileEntry(LogLevelSection, (int)LoggingLevel),
        };

        AddCollection(settingsFileContent, SelectedLogFiles, SelectedLogFilesSection);

        AddCollection(settingsFileContent, EqLogFilesToArchive, ArchiveSelectedFilesToArchiveSection);

        AddCollection(settingsFileContent, InventoryDirectories, InventoryDirectoriesSection);

        AddCollection(settingsFileContent, SpellTrackers.Select(SpellTrackerToConfigString), SpellTrackerSection);

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

    private static string SpellTrackerToConfigString(SpellTrackingConfiguration config)
        => $"{config.CasterCharacterName}|{config.SpellName}|{config.DisplayName}|{config.CastTime}|{config.EstimatedDuration}|{config.SpellLandedSearchString}|{config.SpellFadedSearchString}|{config.DisplayColor}";

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
            string rawValue = split[1];
            if (string.IsNullOrWhiteSpace(rawValue))
                return defaultValue;
            return rawValue;
        }

        index++;
        string settingValue = fileContents[index];

        if (settingValue == key + SectionEnding)
            return defaultValue;

        return settingValue.Trim();
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
            FileInfo fi = new(_zoneIdMapFileName);
            throw new FileNotFoundException($"{_zoneIdMapFileName} does not exist.", fi.FullName);
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
        => 0 <= indexOfSection && indexOfSection < fileContents.Count;

    private bool LogFileCharNameMatchesCharName(string logFilePath, string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return false;

        string logFileCharName = GetCharacterNameFromLogFileName(logFilePath);
        return characterName.Equals(logFileCharName, StringComparison.OrdinalIgnoreCase);
    }

    private void SetApiUrl(string[] fileContents)
    {
        string rawApiUrl = GetStringValue(fileContents, ApiUrlSection);
        if (string.IsNullOrEmpty(rawApiUrl))
            return;

        // People keep forgetting to add the ? at the end of the URL, so just adding it
        if (rawApiUrl.Contains("http://", StringComparison.OrdinalIgnoreCase) && !rawApiUrl.EndsWith('?'))
            rawApiUrl += '?';

        ApiUrl = rawApiUrl;
    }

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

    private void SetSpellTrackers(string[] fileContents)
    {
        ICollection<string> spellTrackerSettings = GetAllEntriesInSection(fileContents, SpellTrackerSection);
        foreach (string setting in spellTrackerSettings)
        {
            string[] settings = setting.Split('|');
            if (settings.Length != 8)
                continue;

            SpellTrackingConfiguration config = new()
            {
                CasterCharacterName = settings[0],
                SpellName = settings[1],
                DisplayName = settings[2],
                CastTime = int.Parse(settings[3]),
                EstimatedDuration = int.Parse(settings[4]),
                SpellLandedSearchString = settings[5],
                SpellFadedSearchString = settings[6],
                DisplayColor = settings[7]
            };
            SpellTrackers.Add(config);
        }

        SpellTrackerXLoc = GetIntValue(fileContents, SpellTrackerXLocSection);
        SpellTrackerYLoc = GetIntValue(fileContents, SpellTrackerYLocSection);
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

    int AuctionOverlayHeight { get; set; }

    int AuctionOverlayWidth { get; set; }

    int AuctionOverlayXLoc { get; set; }

    int AuctionOverlayYLoc { get; set; }

    DkpServerCharacters CharactersOnDkpServer { get; }

    bool DkpspentGuEnabled { get; set; }

    bool EnableZealDetailLogging { get; set; }

    string EqDirectory { get; set; }

    int EqLogFileAgeToArchiveInDays { get; set; }

    string EqLogFileArchiveDirectory { get; set; }

    int EqLogFileSizeToArchiveInMBs { get; set; }

    ICollection<string> EqLogFilesToArchive { get; set; }

    int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    string GeneratedLogFilesArchiveDirectory { get; set; }

    bool IncludeTellsInRawLog { get; set; }

    ICollection<string> InventoryDirectories { get; }

    bool IsApiConfigured { get; }

    ItemLinkValues ItemLinkIds { get; }

    string LogFileMatchPattern { get; set; }

    LogLevel LoggingLevel { get; set; }

    int MainWindowX { get; set; }

    int MainWindowY { get; set; }

    int MezBreaksToShow { get; set; }

    string OutputDirectory { get; set; }

    string OverlayFontColor { get; set; }

    int OverlayFontSize { get; set; }

    int OverlayLocationX { get; set; }

    int OverlayLocationY { get; set; }

    IRaidValues RaidValue { get; }

    int ReadyCheckOverlayHeight { get; set; }

    int ReadyCheckOverlayWidth { get; set; }

    int ReadyCheckOverlayXLoc { get; set; }

    int ReadyCheckOverlayYLoc { get; set; }

    ICollection<string> SelectedLogFiles { get; set; }

    bool ShowAfkReview { get; set; }

    ICollection<SpellTrackingConfiguration> SpellTrackers { get; }

    int SpellTrackerXLoc { get; set; }

    int SpellTrackerYLoc { get; set; }

    bool UseLightMode { get; set; }

    IDictionary<int, string> ZoneIdMapping { get; }

    string GetCharacterNameFromLogFileName(string logFilePath);

    string GetLogFileForCharacter(string characterName);

    void LoadAllSettings();

    bool LoadBaseSettings();

    void LoadOtherFileSettings();

    void SaveSettings();
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class SpellTrackingConfiguration
{
    public string CasterCharacterName { get; init; }

    public int CastTime { get; init; }

    public string DisplayColor { get; init; }

    public string DisplayName { get; init; }

    public int EstimatedDuration { get; init; }

    public string SpellFadedSearchString { get; init; }

    public string SpellLandedSearchString { get; init; }

    public string SpellName { get; init; }

    private string DebugText
        => $"{CasterCharacterName} {SpellName} {CastTime} {EstimatedDuration}";

    public override string ToString()
        => DebugText;
}
