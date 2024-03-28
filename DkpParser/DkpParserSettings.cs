// -----------------------------------------------------------------------
// DkpParserSettings.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System;
using System.IO;

public sealed class DkpParserSettings : IDkpParserSettings
{
    private const string ArchiveAllOrSelectedEqLogFileSection = "ARCHIVE_ALL_EQ_LOG_FILES";
    private const string ArchiveEqLogFileAgeSection = "ARCHIVE_EQ_LOG_FILE_AGE";
    private const string ArchiveEqLogFileDirectorySection = "ARCHIVE_EQ_LOG_FILE_DIRECTORY";
    private const string ArchiveEqLogFilesSizeSection = "ARCHIVE_EQ_LOG_FILE_SIZE";
    private const string ArchiveGeneratedLogFileAgeSection = "ARCHIVE_GEN_LOGS_AGE";
    private const string ArchiveGeneratedLogFileDirectorySection = "ARCHIVE_GEN_LOG_DIRECTORY";
    private const string ArchiveSelectedFilesToArchiveSection = "ARCHIVE_SELECTED_EQ_LOG_FILES";
    private const int DefaultWindowLocation = 200;
    private const string EnableDebugOptionsSection = "ENABLE_DEBUG";
    private const string EqDirectorySection = "EQ_DIRECTORY";
    private const string OutputDirectorySection = "OUTPUT_DIRECTORY";
    private const string SectionEnding = "_END";
    private const string SelectedLogFilesSection = "SELECTED_LOG_FILES";
    private const string WindowLocationSection = "WINDOW_LOCATION";
    private readonly string _bossMobsFilePath;
    private readonly string _settingsFilePath;

    public DkpParserSettings(string settingsFilePath, string bossMobsFilePath)
    {
        _settingsFilePath = settingsFilePath;
        _bossMobsFilePath = bossMobsFilePath;
    }

    public bool ArchiveAllEqLogFiles { get; set; }

    public ICollection<string> BossMobs { get; private set; } = [];

    public bool EnableDebugOptions { get; set; }

    public string EqDirectory { get; set; } = string.Empty;

    public int EqLogFileAgeToArchiveInDays { get; set; }

    public string EqLogFileArchiveDirectory { get; set; }

    public int EqLogFileSizeToArchiveInMBs { get; set; }

    public ICollection<string> EqLogFilesToArchive { get; set; }

    public int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    public string GeneratedLogFilesArchiveDirectory { get; set; }

    public int MainWindowX { get; set; } = DefaultWindowLocation;

    public int MainWindowY { get; set; } = DefaultWindowLocation;

    public string OutputDirectory { get; set; }

    public ICollection<string> SelectedLogFiles { get; set; } = [];

    public void LoadAllSettings()
    {
        LoadSettings();
        LoadBossMobs();
    }

    public void LoadBossMobs()
    {
        if (!File.Exists(_bossMobsFilePath))
            return;

        string[] fileContents = File.ReadAllLines(_bossMobsFilePath);
        foreach (string bossName in fileContents)
            BossMobs.Add(bossName.Trim());
    }

    public void LoadSettings()
    {
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
    }

    public void SaveBossMobs()
    {
        File.WriteAllLines(_bossMobsFilePath, BossMobs);
    }

    public void SaveSettings()
    {
        var settingsFileContent = new List<string>(8)
        {
            WindowLocationSection,
            MainWindowX.ToString(),
            MainWindowY.ToString(),
            WindowLocationSection + SectionEnding,
            EqDirectorySection,
            EqDirectory,
            EqDirectorySection + SectionEnding,
            OutputDirectorySection,
            OutputDirectory,
            OutputDirectorySection + SectionEnding,
            EnableDebugOptionsSection,
            EnableDebugOptions.ToString(),
            EnableDebugOptionsSection + SectionEnding,
            ArchiveAllOrSelectedEqLogFileSection,
            ArchiveAllEqLogFiles.ToString(),
            ArchiveAllOrSelectedEqLogFileSection + SectionEnding,
            ArchiveEqLogFileDirectorySection,
            EqLogFileArchiveDirectory,
            ArchiveEqLogFileDirectorySection + SectionEnding,
            ArchiveEqLogFileAgeSection,
            EqLogFileAgeToArchiveInDays.ToString(),
            ArchiveEqLogFileAgeSection + SectionEnding,
            ArchiveEqLogFilesSizeSection,
            EqLogFileSizeToArchiveInMBs.ToString(),
            ArchiveEqLogFilesSizeSection + SectionEnding,
            ArchiveGeneratedLogFileDirectorySection,
            GeneratedLogFilesArchiveDirectory,
            ArchiveGeneratedLogFileDirectorySection + SectionEnding,
            ArchiveGeneratedLogFileAgeSection,
            GeneratedLogFilesAgeToArchiveInDays.ToString(),
            ArchiveGeneratedLogFileAgeSection + SectionEnding,
        };

        settingsFileContent.Add(SelectedLogFilesSection);
        if (SelectedLogFiles.Count > 0)
            settingsFileContent.AddRange(SelectedLogFiles);

        settingsFileContent.Add(SelectedLogFilesSection + SectionEnding);

        settingsFileContent.Add(ArchiveSelectedFilesToArchiveSection);
        if (EqLogFilesToArchive.Count > 0)
            settingsFileContent.AddRange(EqLogFilesToArchive);

        settingsFileContent.Add(ArchiveSelectedFilesToArchiveSection + SectionEnding);

        File.WriteAllLines(_settingsFilePath, settingsFileContent);
    }

    private static int GetStartingIndex(IList<string> fileContents, string configurationSectionName)
        => fileContents.IndexOf(configurationSectionName);

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

    private int GetIntValue(string[] fileContents, string key, int defaultValue = 0)
    {
        int index = GetStartingIndex(fileContents, key);
        if (!IsValidIndex(index, fileContents))
            return 0;

        index++;
        string setting = fileContents[index];
        if (setting == key + SectionEnding)
            return defaultValue;

        if (int.TryParse(setting, out int intValue))
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

        index++;
        string setting = fileContents[index];

        if (setting == key + SectionEnding)
            return defaultValue;

        return setting;
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

    private bool IsValidIndex(int indexOfSection, IList<string> fileContents)
        => 0 <= indexOfSection && indexOfSection < fileContents.Count - 1;

    private void SetDebugOptions(string[] fileContents)
    {
        string debugOptionsStringValue = GetStringValue(fileContents, EnableDebugOptionsSection);
        EnableDebugOptions = debugOptionsStringValue.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private void SetEqDirectory(string[] fileContents)
    {
        EqDirectory = GetStringValue(fileContents, EqDirectorySection);
    }

    private void SetOutputDirectory(string[] fileContents)
    {
        int index = GetStartingIndex(fileContents, OutputDirectorySection);
        if (!IsValidIndex(index, fileContents))
        {
            if (!string.IsNullOrWhiteSpace(EqDirectory))
            {
                OutputDirectory = EqDirectory;
            }
        }

        index++;
        string setting = fileContents[index];
        OutputDirectory = setting;
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

        string archiveAllStringValue = GetStringValue(fileContents, ArchiveAllOrSelectedEqLogFileSection, "false");
        ArchiveAllEqLogFiles = !archiveAllStringValue.Equals("false", StringComparison.OrdinalIgnoreCase);

        EqLogFilesToArchive = GetAllEntriesInSection(fileContents, ArchiveSelectedFilesToArchiveSection);
    }

    private void SetWindowLocation(IList<string> fileContents)
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
    bool ArchiveAllEqLogFiles { get; set; }

    ICollection<string> BossMobs { get; }

    bool EnableDebugOptions { get; set; }

    string EqDirectory { get; set; }

    int EqLogFileAgeToArchiveInDays { get; set; }

    string EqLogFileArchiveDirectory { get; set; }

    int EqLogFileSizeToArchiveInMBs { get; set; }

    ICollection<string> EqLogFilesToArchive { get; set; }

    int GeneratedLogFilesAgeToArchiveInDays { get; set; }

    string GeneratedLogFilesArchiveDirectory { get; set; }

    int MainWindowX { get; set; }

    int MainWindowY { get; set; }

    string OutputDirectory { get; set; }

    ICollection<string> SelectedLogFiles { get; set; }

    void LoadAllSettings();

    void LoadBossMobs();

    void LoadSettings();

    void SaveBossMobs();

    void SaveSettings();
}
