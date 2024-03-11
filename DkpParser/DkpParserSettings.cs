// -----------------------------------------------------------------------
// DkpParserSettings.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class DkpParserSettings : IDkpParserSettings
{
    private const int DefaultWindowLocation = 200;
    private const string EqDirectorySection = "EQ_DIRECTORY";
    private const string SectionEnding = "_END";
    private const string SelectedLogFilesSection = "SELECTED_LOG_FILES";
    private const string WindowLocation = "WINDOW_LOCATION";

    public ICollection<string> BossMobs { get; private set; } = [];

    public string EqDirectory { get; set; } = string.Empty;

    public int MainWindowX { get; set; } = DefaultWindowLocation;

    public int MainWindowY { get; set; } = DefaultWindowLocation;

    public ICollection<string> SelectedLogFiles { get; private set; } = [];

    public void LoadAllSettings(string settingsFilePath, string bossMobsFilePath)
    {
        LoadSettings(settingsFilePath);
        LoadBossMobs(bossMobsFilePath);
    }

    public void LoadBossMobs(string bossMobsFilePath)
    {
        if (!File.Exists(bossMobsFilePath))
            return;

        string[] fileContents = File.ReadAllLines(bossMobsFilePath);
        BossMobs = fileContents;
    }

    public void LoadSettings(string settingsFilePath)
    {
        if (!File.Exists(settingsFilePath))
        {
            SaveSettings(settingsFilePath);
            return;
        }

        string[] fileContents = File.ReadAllLines(settingsFilePath);
        SetWindowLocation(fileContents);
        SetEqDirectory(fileContents);
        SetSelectedLogFiles(fileContents);
    }

    public void SaveBossMobs(string bossMobsFilePath)
    {
        File.WriteAllLines(bossMobsFilePath, BossMobs);
    }

    public void SaveSettings(string settingsFilePath)
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

        File.WriteAllLines(settingsFilePath, settingsFileContent);
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
        => indexOfSection == -1 || indexOfSection == fileContents.Count - 1;

    private void SetEqDirectory(string[] fileContents)
    {
        int index = GetStartingIndex(fileContents, EqDirectory);
        if (IsValidIndex(index, fileContents))
        {
            index++;
            string setting = fileContents[index];
            EqDirectory = setting;
        }
    }

    private void SetSelectedLogFiles(string[] fileContents)
    {
        int index = GetStartingIndex(fileContents, EqDirectory);
        if (!IsValidIndex(index, fileContents))
            return;


        SelectedLogFiles.Clear();

        string sectionEnd = EqDirectory + SectionEnding;
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

    string EqDirectory { get; }

    int MainWindowX { get; }

    int MainWindowY { get; }

    ICollection<string> SelectedLogFiles { get; }

    void LoadAllSettings(string settingsFilePath, string bossMobsFilePath);

    void LoadBossMobs(string bossMobsFilePath);

    void LoadSettings(string settingsFilePath);
}
