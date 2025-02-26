// -----------------------------------------------------------------------
// GeneralEqLogParserDialogViewModel.cs Copyright 2025 Craig Gjeltema
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
    private bool _allTells;
    private bool _auction;
    private string _caseInsensitiveSearchTerm;
    private ICollection<string> _caseInsensitiveSearchTerms;
    private string _caseSensitiveSearchTerm;
    private ICollection<string> _caseSensitiveSearchTerms;
    private bool _channel;
    private bool _checkAll;
    private bool _dies;
    private string _endTimeText;
    private bool _factionStanding;
    private bool _group;
    private bool _guild;
    private bool _joinRaid;
    private bool _ooc;
    private string _peopleConversingWith;
    private bool _raidSay;
    private bool _say;
    private bool _shout;
    private string _startTimeText;
    private bool _who;
    private bool _you;

    public GeneralEqLogParserDialogViewModel(IDialogViewFactory viewFactory, IDialogFactory dialogFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("GeneralParserDialogTitleText");
        _dialogFactory = dialogFactory;
        _settings = settings;

        CaseSensitiveSearchTerms = [];
        CaseInsensitiveSearchTerms = [];

        DateTime currentTime = DateTime.Now;
        EndTimeText = currentTime.ToString(DateTimeFormat);
        StartTimeText = currentTime.AddHours(-6).ToString(DateTimeFormat);

        StartSearchCommand = new DelegateCommand(StartSearch);
        AddCaseSensitiveSearchTermCommand = new DelegateCommand(AddTermToCaseSensitive, () => !string.IsNullOrWhiteSpace(CaseSensitiveSearchTerm))
            .ObservesProperty(() => CaseSensitiveSearchTerm);
        AddCaseInsensitiveSearchTermCommand = new DelegateCommand(AddTermToCaseInsensitive, () => !string.IsNullOrWhiteSpace(CaseInsensitiveSearchTerm))
            .ObservesProperty(() => CaseInsensitiveSearchTerm);
    }

    public DelegateCommand AddCaseInsensitiveSearchTermCommand { get; }

    public DelegateCommand AddCaseSensitiveSearchTermCommand { get; }

    public bool AllTells
    {
        get => _allTells;
        set => SetProperty(ref _allTells, value);
    }

    public bool Auction
    {
        get => _auction;
        set => SetProperty(ref _auction, value);
    }

    public string CaseInsensitiveSearchTerm
    {
        get => _caseInsensitiveSearchTerm;
        set => SetProperty(ref _caseInsensitiveSearchTerm, value);
    }

    public ICollection<string> CaseInsensitiveSearchTerms
    {
        get => _caseInsensitiveSearchTerms;
        set => SetProperty(ref _caseInsensitiveSearchTerms, value);
    }

    public string CaseSensitiveSearchTerm
    {
        get => _caseSensitiveSearchTerm;
        set => SetProperty(ref _caseSensitiveSearchTerm, value);
    }

    public ICollection<string> CaseSensitiveSearchTerms
    {
        get => _caseSensitiveSearchTerms;
        set => SetProperty(ref _caseSensitiveSearchTerms, value);
    }

    public bool Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    public bool CheckAll
    {
        get => _checkAll;
        set
        {
            SetProperty(ref _checkAll, value);
            AllTells = value;
            Auction = value;
            Channel = value;
            Dies = value;
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
        }
    }

    public bool Dies
    {
        get => _dies;
        set => SetProperty(ref _dies, value);
    }

    public string EndTimeText
    {
        get => _endTimeText;
        set => SetProperty(ref _endTimeText, value);
    }

    public bool FactionStanding
    {
        get => _factionStanding;
        set => SetProperty(ref _factionStanding, value);
    }

    public bool Group
    {
        get => _you;
        set => SetProperty(ref _group, value);
    }

    public bool Guild
    {
        get => _guild;
        set => SetProperty(ref _guild, value);
    }

    public bool JoinRaid
    {
        get => _joinRaid;
        set => SetProperty(ref _joinRaid, value);
    }

    public bool Ooc
    {
        get => _ooc;
        set => SetProperty(ref _ooc, value);
    }

    public string PeopleConversingWith
    {
        get => _peopleConversingWith;
        set => SetProperty(ref _peopleConversingWith, value);
    }

    public bool RaidSay
    {
        get => _raidSay;
        set => SetProperty(ref _raidSay, value);
    }

    public bool Say
    {
        get => _say;
        set => SetProperty(ref _say, value);
    }

    public bool Shout
    {
        get => _shout;
        set => SetProperty(ref _shout, value);
    }

    public DelegateCommand StartSearchCommand { get; }

    public string StartTimeText
    {
        get => _startTimeText;
        set => SetProperty(ref _startTimeText, value);
    }

    public bool Who
    {
        get => _who;
        set => SetProperty(ref _who, value);
    }

    public bool You
    {
        get => _you;
        set => SetProperty(ref _you, value);
    }

    private void AddTermToCaseInsensitive()
    {
        if (string.IsNullOrWhiteSpace(CaseInsensitiveSearchTerm))
            return;

        CaseInsensitiveSearchTerms.Add(CaseInsensitiveSearchTerm);
        CaseInsensitiveSearchTerms = CaseInsensitiveSearchTerms.Order().ToList();
    }

    private void AddTermToCaseSensitive()
    {
        if (string.IsNullOrWhiteSpace(CaseSensitiveSearchTerm))
            return;

        CaseSensitiveSearchTerms.Add(CaseSensitiveSearchTerm);
        CaseSensitiveSearchTerms = CaseSensitiveSearchTerms.Order().ToList();
    }

    private async Task<bool> CreateFile(string fileToWriteTo, IEnumerable<string> fileContents)
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
            Channel = Channel,
            Dies = Dies,
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
            CaseInsensitiveSearchTerms = CaseInsensitiveSearchTerms,
            CaseSensitiveSearchTerms = CaseSensitiveSearchTerms,
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
                await CreateFile(outputFilePath, logFile.GetAllLogLines());
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
    public bool AllTells { get; set; }

    public bool Auction { get; set; }

    public bool Channel { get; set; }

    public bool CheckAll { get; set; }

    public bool Dies { get; set; }

    public bool FactionStanding { get; set; }

    public bool Group { get; set; }

    public bool Guild { get; set; }

    public bool JoinRaid { get; set; }

    public bool Ooc { get; set; }

    public bool RaidSay { get; set; }

    public bool Say { get; set; }

    public bool Shout { get; set; }

    public bool Who { get; set; }

    public bool You { get; set; }

    DelegateCommand AddCaseInsensitiveSearchTermCommand { get; }

    DelegateCommand AddCaseSensitiveSearchTermCommand { get; }

    string CaseInsensitiveSearchTerm { get; set; }

    ICollection<string> CaseInsensitiveSearchTerms { get; }

    string CaseSensitiveSearchTerm { get; set; }

    ICollection<string> CaseSensitiveSearchTerms { get; }

    string EndTimeText { get; set; }

    string PeopleConversingWith { get; set; }

    DelegateCommand StartSearchCommand { get; }

    string StartTimeText { get; set; }
}

