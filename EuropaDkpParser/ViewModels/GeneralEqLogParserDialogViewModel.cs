// -----------------------------------------------------------------------
// GeneralEqLogParserDialogViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows;
using DkpParser;
using DkpParser.Parsers;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class GeneralEqLogParserDialogViewModel : DialogViewModelBase, IGeneralEqLogParserDialogViewModel
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;

    public GeneralEqLogParserDialogViewModel(IDialogViewFactory viewFactory, IDialogFactory dialogFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("GeneralParserDialogTitleText");
        _dialogFactory = dialogFactory;
        _settings = settings;

        CaseSensitiveSearchTerms = [];
        CaseInsensitiveSearchTerms = [];
        ExclusionTerms = [];
        SearchTerms = [];

        DateTime currentTime = DateTime.Now;
        EndTimeText = currentTime.ToString(DateTimeFormat);
        StartTimeText = currentTime.AddHours(-6).ToString(DateTimeFormat);

        StartSearchCommand = new DelegateCommand(StartSearch);
        AddCaseSensitiveSearchTermCommand = new DelegateCommand(AddTermToCaseSensitive, () => !string.IsNullOrWhiteSpace(SearchTermToAdd))
            .ObservesProperty(() => SearchTermToAdd);
        AddCaseInsensitiveSearchTermCommand = new DelegateCommand(AddTermToCaseInsensitive, () => !string.IsNullOrWhiteSpace(SearchTermToAdd))
            .ObservesProperty(() => SearchTermToAdd);
        AddExcludedSearchTermCommand = new DelegateCommand(AddExcludedSearchTerm, () => !string.IsNullOrWhiteSpace(ExclusionTermToAdd))
            .ObservesProperty(() => ExclusionTermToAdd);
    }

    public DelegateCommand AddCaseInsensitiveSearchTermCommand { get; }

    public DelegateCommand AddCaseSensitiveSearchTermCommand { get; }

    public DelegateCommand AddExcludedSearchTermCommand { get; }

    public bool AllTells { get; set => SetProperty(ref field, value); }

    public bool Auction { get; set => SetProperty(ref field, value); }

    public string Channels { get; set => SetProperty(ref field, value); }

    public bool CheckAll
    {
        get;
        set
        {
            SetProperty(ref field, value);
            AllTells = value;
            Auction = value;
            YouSlain = value;
            FactionStanding = value;
            Guild = value;
            Group = value;
            JoinRaid = value;
            Ooc = value;
            RaidSay = value;
            Say = value;
            Shout = value;
            Who = value;
            You = value;
            YourHeals = value;
            OthersHealed = value;
            Twitches = value;
            Rampage = value;
            Looted = value;
            OtherDeath = value;
            YourInterrupts = value;
            OtherInterrupts = value;
        }
    }

    public string EndTimeText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (DateTime.TryParse(value, out DateTime endTime))
                {
                    StartTimeText = endTime.AddHours(-6).ToString(Constants.TimePickerDisplayDateTimeFormat);
                }
            }
        }
    }

    public ICollection<string> ExclusionTerms { get; private set => SetProperty(ref field, value); }

    public string ExclusionTermToAdd { get; set => SetProperty(ref field, value); }

    public bool FactionStanding { get; set => SetProperty(ref field, value); }

    public bool Group { get; set => SetProperty(ref field, value); }

    public bool Guild { get; set => SetProperty(ref field, value); }

    public bool JoinRaid { get; set => SetProperty(ref field, value); }

    public bool Looted { get; set => SetProperty(ref field, value); }

    public bool Ooc { get; set => SetProperty(ref field, value); }

    public bool OtherDeath { get; set => SetProperty(ref field, value); }

    public bool OtherInterrupts { get; set => SetProperty(ref field, value); }

    public bool OthersHealed { get; set => SetProperty(ref field, value); }

    public string PeopleConversingWith { get; set => SetProperty(ref field, value); }

    public bool RaidSay { get; set => SetProperty(ref field, value); }

    public bool Rampage { get; set => SetProperty(ref field, value); }

    public bool Say { get; set => SetProperty(ref field, value); }

    public ICollection<string> SearchTerms { get; set => SetProperty(ref field, value); }

    public string SearchTermToAdd { get; set => SetProperty(ref field, value); }

    public bool Shout { get; set => SetProperty(ref field, value); }

    public DelegateCommand StartSearchCommand { get; }

    public string StartTimeText { get; set => SetProperty(ref field, value); }

    public bool Twitches { get; set => SetProperty(ref field, value); }

    public bool Who { get; set => SetProperty(ref field, value); }

    public bool You { get; set => SetProperty(ref field, value); }

    public bool YourHeals { get; set => SetProperty(ref field, value); }

    public bool YourInterrupts { get; set => SetProperty(ref field, value); }

    public bool YouSlain { get; set => SetProperty(ref field, value); }

    private ICollection<string> CaseInsensitiveSearchTerms { get; init; }

    private ICollection<string> CaseSensitiveSearchTerms { get; init; }

    private void AddExcludedSearchTerm()
    {
        if (string.IsNullOrWhiteSpace(ExclusionTermToAdd))
            return;

        ExclusionTerms.Add(ExclusionTermToAdd);
        ExclusionTerms = ExclusionTerms.Order().ToList();
        ExclusionTermToAdd = string.Empty;
    }

    private void AddTermToCaseInsensitive()
    {
        if (string.IsNullOrWhiteSpace(SearchTermToAdd))
            return;

        CaseInsensitiveSearchTerms.Add(SearchTermToAdd);
        SearchTerms = CaseInsensitiveSearchTerms.Select(x => x + "*").Concat(CaseSensitiveSearchTerms).Order().ToList();
        SearchTermToAdd = string.Empty;
    }

    private void AddTermToCaseSensitive()
    {
        if (string.IsNullOrWhiteSpace(SearchTermToAdd))
            return;

        CaseSensitiveSearchTerms.Add(SearchTermToAdd);
        SearchTerms = CaseSensitiveSearchTerms.Concat(CaseInsensitiveSearchTerms.Select(x => x + "*")).Order().ToList();
        SearchTermToAdd = string.Empty;
    }

    private async Task<bool> CreateFileAsync(string fileToWriteTo, IEnumerable<string> fileContents)
    {
        try
        {
            await Task.Run(() => File.AppendAllLines(fileToWriteTo, fileContents));
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Strings.GetString("LogGenerationErrorMessage") + ex.ToString(), Strings.GetString("LogGenerationError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private async void StartSearch()
        => await StartSearchAsync();

    private async Task StartSearchAsync()
    {
        if (!DateTime.TryParse(StartTimeText, out DateTime startTime))
        {
            MessageBox.Show(Strings.GetString("StartTimeErrorMessage"), Strings.GetString("StartTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!DateTime.TryParse(EndTimeText, out DateTime endTime))
        {
            MessageBox.Show(Strings.GetString("EndTimeErrorMessage"), Strings.GetString("EndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        GeneralEqLogParserSettings settings = new()
        {
            AllTells = AllTells,
            Auction = Auction,
            YouSlain = YouSlain,
            FactionStanding = FactionStanding,
            Guild = Guild,
            JoinRaid = JoinRaid,
            Ooc = Ooc,
            Group = Group,
            RaidSay = RaidSay,
            Say = Say,
            Shout = Shout,
            Who = Who,
            You = You,
            YourHeals = YourHeals,
            OthersHealed = OthersHealed,
            YourInterrupts = YourInterrupts,
            OtherInterrupts = OtherInterrupts,
            Twitches = Twitches,
            Rampage = Rampage,
            Looted = Looted,
            OtherDeath = OtherDeath,
            Channels = Channels?.Split(';'),
            CaseInsensitiveSearchTerms = SearchTerms,
            CaseSensitiveSearchTerms = CaseSensitiveSearchTerms,
            ExclusionTerms = ExclusionTerms,
            PeopleConversingWith = PeopleConversingWith?.Split(';')
        };

        GeneralEqLogParser parser = new();
        ICollection<EqLogFile> logFiles = parser.GetLogFiles(settings, _settings.SelectedLogFiles, startTime, endTime);

        string directory = _settings.OutputDirectory;
        directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "EuropaDKP")
            : directory;

        string outputFile = $"GeneralParse-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string outputFilePath = Path.Combine(directory, outputFile);

        bool anyEntriesFound = false;
        foreach (EqLogFile logFile in logFiles)
        {
            if (logFile.LogEntries.Count > 0)
            {
                await CreateFileAsync(outputFilePath, logFile.GetAllLogLines());
                anyEntriesFound = true;
            }
        }

        if (!anyEntriesFound)
        {
            MessageBox.Show(Strings.GetString("NoEntriesFound"), Strings.GetString("NoEntriesFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(outputFilePath);
        completedDialog.ShowDialog();
    }
}

public interface IGeneralEqLogParserDialogViewModel : IDialogViewModel
{
    DelegateCommand AddCaseInsensitiveSearchTermCommand { get; }

    DelegateCommand AddCaseSensitiveSearchTermCommand { get; }

    DelegateCommand AddExcludedSearchTermCommand { get; }

    bool AllTells { get; set; }

    bool Auction { get; set; }

    string Channels { get; set; }

    bool CheckAll { get; set; }

    string EndTimeText { get; set; }

    ICollection<string> ExclusionTerms { get; }

    string ExclusionTermToAdd { get; set; }

    bool FactionStanding { get; set; }

    bool Group { get; set; }

    bool Guild { get; set; }

    bool JoinRaid { get; set; }

    bool Looted { get; set; }

    bool Ooc { get; set; }

    bool OtherDeath { get; set; }

    bool OtherInterrupts { get; set; }

    bool OthersHealed { get; set; }

    string PeopleConversingWith { get; set; }

    bool RaidSay { get; set; }

    bool Rampage { get; set; }

    bool Say { get; set; }

    ICollection<string> SearchTerms { get; }

    string SearchTermToAdd { get; set; }

    bool Shout { get; set; }

    DelegateCommand StartSearchCommand { get; }

    string StartTimeText { get; set; }

    bool Twitches { get; set; }

    bool Who { get; set; }

    bool You { get; set; }

    bool YourHeals { get; set; }

    bool YourInterrupts { get; set; }

    bool YouSlain { get; set; }
}

