// -----------------------------------------------------------------------
// DkpParserSettings.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class DkpParserSettings : IDkpParserSettings
{
    public DkpParserSettings(string settingsFilePath, string bossMobsFilePath)
    {
        _settingsFilePath = settingsFilePath;
        _bossMobsFilePath = bossMobsFilePath;
    }

    private const int DefaultWindowLocation = 200;
    private const string EqDirectorySection = "EQ_DIRECTORY";
    private const string SectionEnding = "_END";
    private const string SelectedLogFilesSection = "SELECTED_LOG_FILES";
    private const string WindowLocation = "WINDOW_LOCATION";
    private readonly string _settingsFilePath;
    private readonly string _bossMobsFilePath;

    public ICollection<string> BossMobs { get; private set; } = [];

    public string EqDirectory { get; set; } = string.Empty;

    public int MainWindowX { get; set; } = DefaultWindowLocation;

    public int MainWindowY { get; set; } = DefaultWindowLocation;

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
        BossMobs = fileContents;
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
        SetSelectedLogFiles(fileContents);
    }

    public void SaveBossMobs()
    {
        File.WriteAllLines(_bossMobsFilePath, BossMobs);
    }

    public void SaveSettings()
    {
        var settingsFileContent = new List<string>(8)
        {
            WindowLocation,
            MainWindowX.ToString(),
            MainWindowY.ToString(),
            WindowLocation + SectionEnding,
            EqDirectorySection,
            EqDirectory,
            EqDirectorySection + SectionEnding
        };

        settingsFileContent.Add(SelectedLogFilesSection);
        if (SelectedLogFiles.Count > 0)
            settingsFileContent.AddRange(SelectedLogFiles);

        settingsFileContent.Add(SelectedLogFilesSection + SectionEnding);

        File.WriteAllLines(_settingsFilePath, settingsFileContent);
    }

    private static int GetStartingIndex(IList<string> fileContents, string configurationSectionName)
        => fileContents.IndexOf(configurationSectionName);

    private int GetWindowLoc(string setting)
    {
        if (setting == WindowLocation + SectionEnding)
            return DefaultWindowLocation;

        if (int.TryParse(setting, out int windowLoc))
        {
            return windowLoc;
        }

        return DefaultWindowLocation;
    }

    private bool IsValidIndex(int indexOfSection, IList<string> fileContents)
        => 0 <= indexOfSection && indexOfSection < fileContents.Count - 1;

    private void SetEqDirectory(string[] fileContents)
    {
        int index = GetStartingIndex(fileContents, EqDirectorySection);
        if (!IsValidIndex(index, fileContents))
            return;

        index++;
        string setting = fileContents[index];
        EqDirectory = setting;
    }

    private void SetSelectedLogFiles(string[] fileContents)
    {
        int index = GetStartingIndex(fileContents, SelectedLogFilesSection);
        if (!IsValidIndex(index, fileContents))
            return;

        SelectedLogFiles.Clear();

        string sectionEnd = SelectedLogFilesSection + SectionEnding;
        index++;
        string entry = fileContents[index];
        while (entry != sectionEnd)
        {
            SelectedLogFiles.Add(entry);
            index++;
            entry = fileContents[index];
        }
    }

    private void SetWindowLocation(IList<string> fileContents)
    {
        int index = GetStartingIndex(fileContents, WindowLocation);
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
    ICollection<string> BossMobs { get; }

    string EqDirectory { get; set; }

    int MainWindowX { get; set; }

    int MainWindowY { get; set; }

    ICollection<string> SelectedLogFiles { get; set; }

    void LoadAllSettings();

    void LoadBossMobs();

    void LoadSettings();

    void SaveBossMobs();

    void SaveSettings();
}
