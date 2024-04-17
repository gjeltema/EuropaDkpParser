

namespace EuropaDkpParser.ViewModels;

using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class GeneralEqLogParserDialogViewModel : DialogViewModelBase, IGeneralEqLogParserDialogViewModel
{
    public GeneralEqLogParserDialogViewModel(IDialogViewFactory viewFactory) 
        : base(viewFactory)
    {
        Title = Strings.GetString("GeneralParserDialogTitleText");
        CaseSensitiveSearchTerms = [];
        CaseInsensitiveSearchTerms = [];

        StartSearchCommand = new DelegateCommand(StartSearch);
        AddCaseSensitiveSearchTermCommand = new DelegateCommand(AddTermToCaseSensitive, () => !string.IsNullOrWhiteSpace(CaseSensitiveSearchTerm))
            .ObservesProperty(() => CaseSensitiveSearchTerm);
        AddCaseInsensitiveSearchTermCommand = new DelegateCommand(AddTermToCaseInsensitive, () => !string.IsNullOrWhiteSpace(CaseInsensitiveSearchTerm))
            .ObservesProperty(() => CaseInsensitiveSearchTerm);
    }

    private void StartSearch()
    {

    }

    private void AddTermToCaseSensitive()
    {
        if(!string.IsNullOrWhiteSpace(CaseSensitiveSearchTerm))
            CaseSensitiveSearchTerms.Add(CaseSensitiveSearchTerm);
    }

    private void AddTermToCaseInsensitive()
    {
        if (!string.IsNullOrWhiteSpace(CaseInsensitiveSearchTerm))
            CaseInsensitiveSearchTerms.Add(CaseInsensitiveSearchTerm);
    }

    private ICollection<string> _caseSensitiveSearchTerms;
    public ICollection<string> CaseSensitiveSearchTerms
    {
        get => _caseSensitiveSearchTerms;
        set => SetProperty(ref _caseSensitiveSearchTerms, value);
    }
    private ICollection<string> _caseInsensitiveSearchTerms;
    public ICollection<string> CaseInsensitiveSearchTerms
    {
        get => _caseInsensitiveSearchTerms;
        set => SetProperty(ref _caseInsensitiveSearchTerms, value);
    }
    public DelegateCommand AddCaseSensitiveSearchTermCommand { get; }
    public DelegateCommand AddCaseInsensitiveSearchTermCommand { get; }
   private  string _peopleConversingWith;
    public string PeopleConversingWith
    {
        get => _peopleConversingWith;
        set => SetProperty(ref _peopleConversingWith, value);
    }
    private bool _allTells;
    public bool AllTells
    {
        get => _allTells;
        set => SetProperty(ref _allTells, value);
    }
    private bool _auction;
    public bool Auction
    {
        get => _auction;
        set => SetProperty(ref _auction, value);
    }
    private bool _channel;
    public bool Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }
    private bool _dies;
    public bool Dies
    {
        get => _dies;
        set => SetProperty(ref _dies, value);
    }
    private bool _factionStanding;
    public bool FactionStanding
    {
        get => _factionStanding;
        set => SetProperty(ref _factionStanding, value);
    }
    private bool _guild;
    public bool Guild
    {
        get => _guild;
        set => SetProperty(ref _guild, value);
    }
    private bool _joinRaid;
    public bool JoinRaid
    {
        get => _joinRaid;
        set => SetProperty(ref _joinRaid, value);
    }
    private bool _ooc;
    public bool Ooc
    {
        get => _ooc;
        set => SetProperty(ref _ooc, value);
    }
    private bool _raidSay;
    public bool RaidSay
    {
        get => _raidSay;
        set => SetProperty(ref _raidSay, value);
    }
    private bool _say;
    public bool Say
    {
        get => _say;
        set => SetProperty(ref _say, value);
    }
    private bool _shout;
    public bool Shout
    {
        get => _shout;
        set => SetProperty(ref _shout, value);
    }
    private bool _who;
    public bool Who
    {
        get => _who;
        set => SetProperty(ref _who, value);
    }
    private bool _you;
    public bool You
    {
        get => _you;
        set => SetProperty(ref _you, value);
    }
    private bool _checkAll;
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
            JoinRaid = value;
            Ooc = value;
            RaidSay = value;
            Say = value;
            Shout = value;
            Who = value;
            You = value;
        }
    }
    private string _caseSensitiveSearchTerm;
    public string CaseSensitiveSearchTerm
    {
        get => _caseSensitiveSearchTerm;
        set => SetProperty(ref _caseSensitiveSearchTerm, value);
    }
    private string _caseInsensitiveSearchTerm;
    public string CaseInsensitiveSearchTerm
    {
        get => _caseInsensitiveSearchTerm;
        set => SetProperty(ref _caseInsensitiveSearchTerm, value);
    }
    public DelegateCommand StartSearchCommand { get; }
}

public interface IGeneralEqLogParserDialogViewModel : IDialogViewModel
{
    ICollection<string> CaseSensitiveSearchTerms { get; }

    ICollection<string> CaseInsensitiveSearchTerms { get; }

    DelegateCommand AddCaseSensitiveSearchTermCommand { get; }
    DelegateCommand AddCaseInsensitiveSearchTermCommand { get; }

    string PeopleConversingWith { get; set; }

    public bool AllTells { get; set; }

    public bool Auction { get; set; }

    public bool Channel { get; set; }

    public bool Dies { get; set; }

    public bool FactionStanding { get; set; }

    public bool Guild { get; set; }

    public bool JoinRaid { get; set; }

    public bool Ooc { get; set; }

    public bool RaidSay { get; set; }

    public bool Say { get; set; }

    public bool Shout { get; set; }

    public bool Who { get; set; }

    public bool You { get; set; }

    public bool CheckAll { get; set; }

    string CaseSensitiveSearchTerm { get; set;}
    string CaseInsensitiveSearchTerm { get; set;}

    DelegateCommand StartSearchCommand { get; }
}

