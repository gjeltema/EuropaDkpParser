// -----------------------------------------------------------------------
// DkpErrorDisplayDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class DkpErrorDisplayDialogViewModel : DialogViewModelBase, IDkpErrorDisplayDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private DkpEntry _currentEntry;
    private string _dkpSpent;
    private ICollection<DkpEntry> _duplicateDkpspentEntries;
    private string _errorMessageText;
    private bool _isDuplicateDkpSpentCallError;
    private bool _isFilterByItemChecked;
    private bool _isFilterByNameChecked;
    private bool _isNoPlayerLootedError;
    private bool _isPlayerNameTypoError;
    private string _itemName;
    private string _itemNameAndDkp;
    private string _nextButtonText;
    private ICollection<PlayerLooted> _playerLootedEntries;
    private string _playerName;
    private string _rawLogLine;
    private DkpEntry _selectedDuplicateDkpEntry;
    private PlayerLooted _selectedPlayerLooted;
    private string _selectedPlayerName;
    private string _timestamp;

    internal DkpErrorDisplayDialogViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("DkpErrorDisplayDialogTitleText");

        _settings = settings;
        _raidEntries = raidEntries;

        AllPlayers = _raidEntries.AllPlayersInRaid.Select(x => x.PlayerName).Order().ToList();
        PlayerLootedEntries = _raidEntries.PlayerLootedEntries.OrderBy(x => x.Timestamp).ToList();
        DuplicateDkpspentEntries = [];

        MoveToNextErrorCommand = new DelegateCommand(AdvanceToNextError);
        FixNoLootedMessageUsingSelectionCommand = new DelegateCommand(FixNoLootedMessageUsingSelection, () => SelectedPlayerLooted != null)
            .ObservesProperty(() => SelectedPlayerLooted);
        FixNoLootedMessageManualCommand = new DelegateCommand(FixNoLootedMessageManual, () => !string.IsNullOrWhiteSpace(PlayerName) && !string.IsNullOrWhiteSpace(ItemName) && !string.IsNullOrWhiteSpace(DkpSpent))
            .ObservesProperty(() => PlayerName).ObservesProperty(() => ItemName).ObservesProperty(() => DkpSpent);
        FixPlayerTypoUsingSelectionCommand = new DelegateCommand(FixPlayerTypoUsingSelection, () => SelectedPlayerName != null)
            .ObservesProperty(() => SelectedPlayerName);
        FixPlayerTypoManualCommand = new DelegateCommand(FixPlayerTypoManual, () => !string.IsNullOrWhiteSpace(PlayerName)).ObservesProperty(() => PlayerName);
        RemoveDuplicateSelectionCommand = new DelegateCommand(RemoveDuplicateDkpspentCall, () => IsDuplicateDkpSpentCallError && SelectedDuplicateDkpEntry != null)
            .ObservesProperty(() => IsDuplicateDkpSpentCallError).ObservesProperty(() => SelectedDuplicateDkpEntry);

        AdvanceToNextError();
    }

    public ICollection<string> AllPlayers { get; }

    public string DkpSpent
    {
        get => _dkpSpent;
        set => SetProperty(ref _dkpSpent, value);
    }

    public ICollection<DkpEntry> DuplicateDkpspentEntries
    {
        get => _duplicateDkpspentEntries;
        private set => SetProperty(ref _duplicateDkpspentEntries, value);
    }

    public string ErrorMessageText
    {
        get => _errorMessageText;
        private set => SetProperty(ref _errorMessageText, value);
    }

    public DelegateCommand FixNoLootedMessageManualCommand { get; }

    public DelegateCommand FixNoLootedMessageUsingSelectionCommand { get; }

    public DelegateCommand FixPlayerTypoManualCommand { get; }

    public DelegateCommand FixPlayerTypoUsingSelectionCommand { get; }

    public bool IsDuplicateDkpSpentCallError
    {
        get => _isDuplicateDkpSpentCallError;
        private set => SetProperty(ref _isDuplicateDkpSpentCallError, value);
    }

    public bool IsFilterByItemChecked
    {
        get => _isFilterByItemChecked;
        set
        {
            SetProperty(ref _isFilterByItemChecked, value);
            if (!_isFilterByItemChecked || _currentEntry == null)
            {
                if (IsFilterByNameChecked)
                    return;
                PlayerLootedEntries = _raidEntries.PlayerLootedEntries.OrderBy(x => x.Timestamp).ToList();
                return;
            }

            IsFilterByNameChecked = false;
            var items = _raidEntries.PlayerLootedEntries.Where(x => x.ItemLooted == _currentEntry.Item).ToList();
            PlayerLootedEntries = _raidEntries.PlayerLootedEntries.Where(x => x.ItemLooted == _currentEntry.Item).OrderBy(x => x.Timestamp).ToList();
        }
    }

    public bool IsFilterByNameChecked
    {
        get => _isFilterByNameChecked;
        set
        {
            SetProperty(ref _isFilterByNameChecked, value);
            if (!_isFilterByNameChecked || _currentEntry == null)
            {
                if (IsFilterByItemChecked)
                    return;
                PlayerLootedEntries = _raidEntries.PlayerLootedEntries.OrderBy(x => x.Timestamp).ToList();
                return;
            }

            IsFilterByItemChecked = false;
            PlayerLootedEntries = _raidEntries.PlayerLootedEntries.Where(x => x.PlayerName == _currentEntry.PlayerName).OrderBy(x => x.Timestamp).ToList();
        }
    }

    public bool IsNoPlayerLootedError
    {
        get => _isNoPlayerLootedError;
        private set => SetProperty(ref _isNoPlayerLootedError, value);
    }

    public bool IsPlayerNameTypoError
    {
        get => _isPlayerNameTypoError;
        private set => SetProperty(ref _isPlayerNameTypoError, value);
    }

    public string ItemName
    {
        get => _itemName;
        set => SetProperty(ref _itemName, value);
    }

    public string ItemNameAndDkp
    {
        get => _itemNameAndDkp;
        set => SetProperty(ref _itemNameAndDkp, value);
    }

    public DelegateCommand MoveToNextErrorCommand { get; }

    public string NextButtonText
    {
        get => _nextButtonText;
        private set => SetProperty(ref _nextButtonText, value);
    }

    public ICollection<PlayerLooted> PlayerLootedEntries
    {
        get => _playerLootedEntries;
        private set => SetProperty(ref _playerLootedEntries, value);
    }

    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    public string RawLogLine
    {
        get => _rawLogLine;
        private set => SetProperty(ref _rawLogLine, value);
    }

    public DelegateCommand RemoveDuplicateSelectionCommand { get; }

    public DkpEntry SelectedDuplicateDkpEntry
    {
        get => _selectedDuplicateDkpEntry;
        set => SetProperty(ref _selectedDuplicateDkpEntry, value);
    }

    public PlayerLooted SelectedPlayerLooted
    {
        get => _selectedPlayerLooted;
        set => SetProperty(ref _selectedPlayerLooted, value);
    }

    public string SelectedPlayerName
    {
        get => _selectedPlayerName;
        set => SetProperty(ref _selectedPlayerName, value);
    }

    public string Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    private void AdvanceToNextError()
    {
        if (DuplicateDkpspentEntries.Count > 0)
        {
            foreach (DkpEntry entry in DuplicateDkpspentEntries)
            {
                entry.PossibleError = PossibleError.None;
            }
        }

        if (_currentEntry != null)
            _currentEntry.PossibleError = PossibleError.None;

        SetNextButtonText();

        _currentEntry = _raidEntries.DkpEntries.FirstOrDefault(x => x.PossibleError != PossibleError.None);
        if (_currentEntry == null)
        {
            CloseOk();
            return;
        }

        PlayerName = _currentEntry.PlayerName;
        ItemName = _currentEntry.Item;
        DkpSpent = _currentEntry.DkpSpent.ToString();
        ItemNameAndDkp = $"{_currentEntry.Item}, DKP: {_currentEntry.DkpSpent}";
        Timestamp = _currentEntry.Timestamp.ToString("HH:mm:ss");
        RawLogLine = _currentEntry.RawLogLine;

        if (_currentEntry.PossibleError == PossibleError.PlayerLootedMessageNotFound)
        {
            IsNoPlayerLootedError = true;
            IsPlayerNameTypoError = false;
            IsDuplicateDkpSpentCallError = false;
            ErrorMessageText = Strings.GetString("PlayerLootedItemEntryNotFound");
        }
        else if (_currentEntry.PossibleError == PossibleError.DkpSpentPlayerNameTypo)
        {
            IsNoPlayerLootedError = false;
            IsPlayerNameTypoError = true;
            IsDuplicateDkpSpentCallError = false;
            ErrorMessageText = Strings.GetString("PlayerNameTypo");

            for (int i = 8; i > 0; i--)
            {
                if (_currentEntry.PlayerName.Length < i)
                    continue;

                string startOfPlayerName = _currentEntry.PlayerName[..i];
                string playerInRaidName = AllPlayers.FirstOrDefault(x => x.StartsWith(startOfPlayerName));
                if (playerInRaidName != null)
                {
                    SelectedPlayerName = playerInRaidName;
                    break;
                }
            }
        }
        else if (_currentEntry.PossibleError == PossibleError.DkpDuplicateEntry)
        {
            IsNoPlayerLootedError = false;
            IsPlayerNameTypoError = false;
            IsDuplicateDkpSpentCallError = true;

            ErrorMessageText = Strings.GetString("DuplicateDkpSpentCall");

            SetDuplicateDkpSpentEntries();
        }
    }

    private void FixNoLootedMessageManual()
    {
        if (!int.TryParse(DkpSpent, out int parsedDkp))
        {
            MessageBox.Show(string.Format(Strings.GetString("DkpSpentErrorFormatText"), DkpSpent.ToString()), Strings.GetString("DkpSpentError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        _currentEntry.PlayerName = PlayerName;
        _currentEntry.Item = ItemName;
        _currentEntry.DkpSpent = parsedDkp;
    }

    private void FixNoLootedMessageUsingSelection()
    {
        if (SelectedPlayerLooted == null)
        {
            MessageBox.Show(Strings.GetString("PlayerLootNotSelectedMessage"), Strings.GetString("PlayerLootNotSelected"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        _currentEntry.PlayerName = _selectedPlayerLooted.PlayerName;
        _currentEntry.Item = _selectedPlayerLooted.ItemLooted;
    }

    private void FixPlayerTypoManual()
    {
        string updatedPlayerName = PlayerName.Trim();
        if (string.IsNullOrEmpty(updatedPlayerName))
        {
            MessageBox.Show(Strings.GetString("PlayerNameErrorMessage"), Strings.GetString("PlayerNameError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _currentEntry.PlayerName = PlayerName;
    }

    private void FixPlayerTypoUsingSelection()
    {
        _currentEntry.PlayerName = SelectedPlayerName;
        PlayerName = _currentEntry.PlayerName;
    }

    private void RemoveDuplicateDkpspentCall()
    {
        if (SelectedDuplicateDkpEntry == null)
            return;

        _raidEntries.DkpEntries.Remove(SelectedDuplicateDkpEntry);
        SetDuplicateDkpSpentEntries();
        SetNextButtonText();
    }

    private void SetDuplicateDkpSpentEntries()
    {
        DuplicateDkpspentEntries = _raidEntries.DkpEntries
            .Where(x => x.PlayerName.Equals(_currentEntry.PlayerName, StringComparison.OrdinalIgnoreCase) && x.Item == _currentEntry.Item && x.DkpSpent == _currentEntry.DkpSpent)
            .ToList();
    }

    private void SetNextButtonText()
    {
        int numberOfErrors = _raidEntries.DkpEntries.Count(x => x.PossibleError != PossibleError.None);
        NextButtonText = numberOfErrors > 1 ? Strings.GetString("Next") : Strings.GetString("Finish");
    }
}

public interface IDkpErrorDisplayDialogViewModel : IDialogViewModel
{
    public string Timestamp { get; }

    ICollection<string> AllPlayers { get; }

    string DkpSpent { get; set; }

    ICollection<DkpEntry> DuplicateDkpspentEntries { get; }

    string ErrorMessageText { get; }

    DelegateCommand FixNoLootedMessageManualCommand { get; }

    DelegateCommand FixNoLootedMessageUsingSelectionCommand { get; }

    DelegateCommand FixPlayerTypoManualCommand { get; }

    DelegateCommand FixPlayerTypoUsingSelectionCommand { get; }

    bool IsDuplicateDkpSpentCallError { get; }

    bool IsFilterByItemChecked { get; set; }

    bool IsFilterByNameChecked { get; set; }

    bool IsNoPlayerLootedError { get; }

    bool IsPlayerNameTypoError { get; }

    string ItemName { get; set; }

    string ItemNameAndDkp { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string NextButtonText { get; }

    ICollection<PlayerLooted> PlayerLootedEntries { get; }

    string PlayerName { get; set; }

    string RawLogLine { get; }

    DelegateCommand RemoveDuplicateSelectionCommand { get; }

    DkpEntry SelectedDuplicateDkpEntry { get; set; }

    PlayerLooted SelectedPlayerLooted { get; set; }

    string SelectedPlayerName { get; set; }
}
