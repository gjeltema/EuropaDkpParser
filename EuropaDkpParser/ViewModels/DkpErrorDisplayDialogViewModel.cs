// -----------------------------------------------------------------------
// DkpErrorDisplayDialogViewModel.cs Copyright 2025 Craig Gjeltema
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
    private string _actionCompletedMessage;
    private string _auctioneer;
    private DkpEntry _currentEntry;
    private string _dkpSpent;
    private ICollection<DkpEntry> _duplicateDkpspentEntries;
    private string _errorMessageText;
    private bool _isDuplicateDkpSpentCallError;
    private bool _isFilterByItemChecked;
    private bool _isFilterByNameChecked;
    private bool _isMalformedDkpspentCall;
    private bool _isNoPlayerLootedError;
    private bool _isPlayerNameTypoError;
    private bool _isZeroDkpError;
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

        AllPlayers = _raidEntries.AllCharactersInRaid
            .Select(x => x.CharacterName)
            .Union(_settings.CharactersOnDkpServer.AllUserCharacters.Select(x => x.Name))
            .Order()
            .ToList();
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
        FixZeroDkpErrorCommand = new DelegateCommand(FixZeroDkpError, () => !string.IsNullOrWhiteSpace(DkpSpent) && DkpSpent != "0")
            .ObservesProperty(() => DkpSpent);
        RemoveDuplicateSelectionCommand = new DelegateCommand(RemoveDuplicateDkpspentCall, () => IsDuplicateDkpSpentCallError && SelectedDuplicateDkpEntry != null)
            .ObservesProperty(() => IsDuplicateDkpSpentCallError).ObservesProperty(() => SelectedDuplicateDkpEntry);
        RemoveDkpEntryCommand = new DelegateCommand(RemoveDkpEntry);
        FixMalformedDkpEntryCommand = new DelegateCommand(FixMalformedDkpEntry, () => !string.IsNullOrWhiteSpace(PlayerName) && !string.IsNullOrWhiteSpace(ItemName) && !string.IsNullOrWhiteSpace(DkpSpent))
            .ObservesProperty(() => PlayerName).ObservesProperty(() => ItemName).ObservesProperty(() => DkpSpent);

        AdvanceToNextError();
    }

    public string ActionCompletedMessage
    {
        get => _actionCompletedMessage;
        set => SetProperty(ref _actionCompletedMessage, value);
    }

    public ICollection<string> AllPlayers { get; }

    public string Auctioneer
    {
        get => _auctioneer;
        set => SetProperty(ref _auctioneer, value);
    }

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

    public DelegateCommand FixMalformedDkpEntryCommand { get; }

    public DelegateCommand FixNoLootedMessageManualCommand { get; }

    public DelegateCommand FixNoLootedMessageUsingSelectionCommand { get; }

    public DelegateCommand FixPlayerTypoManualCommand { get; }

    public DelegateCommand FixPlayerTypoUsingSelectionCommand { get; }

    public DelegateCommand FixZeroDkpErrorCommand { get; }

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

    public bool IsMalformedDkpspentCall
    {
        get => _isMalformedDkpspentCall;
        private set => SetProperty(ref _isMalformedDkpspentCall, value);
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

    public bool IsZeroDkpError
    {
        get => _isZeroDkpError;
        private set => SetProperty(ref _isZeroDkpError, value);
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

    public DelegateCommand RemoveDkpEntryCommand { get; }

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
        // Clean up previous error entries
        if (DuplicateDkpspentEntries.Count > 0)
        {
            foreach (DkpEntry entry in DuplicateDkpspentEntries)
            {
                entry.PossibleError = PossibleError.None;
            }
        }

        // If the user just clicks "Next" for a malformed error, then remove it.
        if (_currentEntry?.PossibleError == PossibleError.MalformedDkpSpentLine)
        {
            RemoveDkpEntry();
        }

        ActionCompletedMessage = string.Empty;

GOTO_NEXT_ENTRY:

        if (_currentEntry != null)
            _currentEntry.PossibleError = PossibleError.None;

        SetNextButtonText();

        // Initialize next error
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

        IsNoPlayerLootedError = false;
        IsPlayerNameTypoError = false;
        IsDuplicateDkpSpentCallError = false;
        IsZeroDkpError = false;
        IsMalformedDkpspentCall = false;

        if (_currentEntry.PossibleError == PossibleError.PlayerLootedMessageNotFound)
        {
            // Bypassing this error.  With many alts getting items, and raids moving rapidly to the next target where the attendance
            // taker isnt around to see the "looted" messages, this is basically spam now without any actual value.
            goto GOTO_NEXT_ENTRY;

            //if (_raidEntries.PlayerLootedEntries.Count == 0)
            //    goto GOTO_NEXT_ENTRY; // switch to a while loop if more than one error type may need skipping.

            //IsNoPlayerLootedError = true;
            //ErrorMessageText = Strings.GetString("PlayerLootedItemEntryNotFound");
        }
        else if (_currentEntry.PossibleError == PossibleError.DkpSpentPlayerNameTypo)
        {
            IsPlayerNameTypoError = true;
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
            IsDuplicateDkpSpentCallError = true;

            ErrorMessageText = Strings.GetString("DuplicateDkpSpentCall");

            SetDuplicateDkpSpentEntries();
        }
        else if (_currentEntry.PossibleError == PossibleError.ZeroDkp)
        {
            IsZeroDkpError = true;

            ErrorMessageText = Strings.GetString("ZeroDkpSpentCall");
        }
        else if (_currentEntry.PossibleError == PossibleError.MalformedDkpSpentLine)
        {
            IsMalformedDkpspentCall = true;

            Auctioneer = _currentEntry.Auctioneer;

            ErrorMessageText = Strings.GetString("MalformedDkpSpentError");
        }
    }

    private void FixMalformedDkpEntry()
    {
        if (string.IsNullOrEmpty(PlayerName) || string.IsNullOrEmpty(DkpSpent) || string.IsNullOrEmpty(ItemName))
            return;

        if (!int.TryParse(DkpSpent, out int parsedDkp))
        {
            MessageBox.Show(string.Format(Strings.GetString("DkpSpentErrorFormatText"), DkpSpent.ToString()), Strings.GetString("DkpSpentError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _currentEntry.PlayerName = PlayerName;
        _currentEntry.Item = ItemName;
        _currentEntry.DkpSpent = parsedDkp;
        _currentEntry.Auctioneer = Auctioneer;

        _currentEntry.PossibleError = PossibleError.None;

        ActionCompletedMessage = Strings.GetString("FixedMalformedDkpMessage");
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

        _currentEntry.PossibleError = PossibleError.None;

        ActionCompletedMessage = Strings.GetString("FixedPlayerNotLootedMessage");
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

        _currentEntry.PossibleError = PossibleError.None;

        ActionCompletedMessage = Strings.GetString("FixedPlayerNotLootedMessage");
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

        _currentEntry.PossibleError = PossibleError.None;

        ActionCompletedMessage = Strings.GetString("FixedPlayerTypoMessage");
    }

    private void FixPlayerTypoUsingSelection()
    {
        _currentEntry.PlayerName = SelectedPlayerName;
        PlayerName = _currentEntry.PlayerName;

        _currentEntry.PossibleError = PossibleError.None;

        ActionCompletedMessage = Strings.GetString("FixedPlayerTypoMessage");
    }

    private void FixZeroDkpError()
    {
        if (!int.TryParse(DkpSpent, out int parsedDkp))
        {
            MessageBox.Show(string.Format(Strings.GetString("DkpSpentErrorFormatText"), DkpSpent.ToString()), Strings.GetString("DkpSpentError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _currentEntry.DkpSpent = parsedDkp;

        _currentEntry.PossibleError = PossibleError.None;

        ActionCompletedMessage = Strings.GetString("FixedZeroDkpMessage");
    }

    private void RemoveDkpEntry()
    {
        _raidEntries.DkpEntries.Remove(_currentEntry);

        ActionCompletedMessage = Strings.GetString("RemovedPlayerNotLootedMessage");
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
            .OrderBy(x => x.Timestamp)
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
    string ActionCompletedMessage { get; }

    ICollection<string> AllPlayers { get; }

    string Auctioneer { get; set; }

    string DkpSpent { get; set; }

    ICollection<DkpEntry> DuplicateDkpspentEntries { get; }

    string ErrorMessageText { get; }

    DelegateCommand FixMalformedDkpEntryCommand { get; }

    DelegateCommand FixNoLootedMessageManualCommand { get; }

    DelegateCommand FixNoLootedMessageUsingSelectionCommand { get; }

    DelegateCommand FixPlayerTypoManualCommand { get; }

    DelegateCommand FixPlayerTypoUsingSelectionCommand { get; }

    DelegateCommand FixZeroDkpErrorCommand { get; }

    bool IsDuplicateDkpSpentCallError { get; }

    bool IsFilterByItemChecked { get; set; }

    bool IsFilterByNameChecked { get; set; }

    bool IsMalformedDkpspentCall { get; }

    bool IsNoPlayerLootedError { get; }

    bool IsPlayerNameTypoError { get; }

    bool IsZeroDkpError { get; }

    string ItemName { get; set; }

    string ItemNameAndDkp { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string NextButtonText { get; }

    ICollection<PlayerLooted> PlayerLootedEntries { get; }

    string PlayerName { get; set; }

    string RawLogLine { get; }

    DelegateCommand RemoveDkpEntryCommand { get; }

    DelegateCommand RemoveDuplicateSelectionCommand { get; }

    DkpEntry SelectedDuplicateDkpEntry { get; set; }

    PlayerLooted SelectedPlayerLooted { get; set; }

    string SelectedPlayerName { get; set; }

    string Timestamp { get; }
}
