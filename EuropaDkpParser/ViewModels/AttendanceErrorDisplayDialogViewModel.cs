// -----------------------------------------------------------------------
// AttendanceErrorDisplayDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using Prism.Commands;

internal sealed class AttendanceErrorDisplayDialogViewModel : DialogViewModelBase, IAttendanceErrorDisplayDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private AttendanceEntry _currentEntry;
    private string _errorLogEntry;
    private string _errorMessageText;
    private bool _moreErrorsRemaining;
    private string _selectedBossName;

    internal AttendanceErrorDisplayDialogViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings, RaidEntries raidEntries)
        : base(viewFactory)
    {
        _settings = settings;
        _raidEntries = raidEntries;
    }

    public ICollection<string> ApprovedBossNames { get; }

    public string ErrorLogEntry
    {
        get => _errorLogEntry;
        set => SetProperty(ref _errorLogEntry, value);
    }

    public string ErrorMessageText
    {
        get => _errorMessageText;
        set => SetProperty(ref _errorMessageText, value);
    }

    public DelegateCommand FinishReviewingErrorsCommand { get; }

    public DelegateCommand FixErrorCommand { get; }

    public bool MoreErrorsRemaining
    {
        get => _moreErrorsRemaining;
        set => SetProperty(ref _moreErrorsRemaining, value);
    }

    public DelegateCommand MoveToNextErrorCommand { get; }

    public string SelectedBossName
    {
        get => _selectedBossName;
        set => SetProperty(ref _selectedBossName, value);
    }

    public void AdvanceToNextError()
    {
        _currentEntry = _raidEntries.AttendanceEntries.FirstOrDefault(x => x.PossibleError != PossibleError.None);
        if (_currentEntry == null)
        {
            MoreErrorsRemaining = false;
        }
    }
}

public interface IAttendanceErrorDisplayDialogViewModel : IDialogViewModel
{
    ICollection<string> ApprovedBossNames { get; }

    string ErrorLogEntry { get; }

    string ErrorMessageText { get; }

    DelegateCommand FinishReviewingErrorsCommand { get; }

    DelegateCommand FixErrorCommand { get; }

    bool MoreErrorsRemaining { get; }

    DelegateCommand MoveToNextErrorCommand { get; }

    string SelectedBossName { get; set; }

    void AdvanceToNextError();
}
