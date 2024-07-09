// -----------------------------------------------------------------------
// FinalSummaryDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows;
using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class FinalSummaryDialogViewModel : DialogViewModelBase, IFinalSummaryDialogViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<AttendanceEntry> _attendanceCalls;
    private ICollection<DkpEntry> _dkpSpentCalls;
    private DkpEntry _selectedDkpspent;
    private bool _uploadToServer;

    internal FinalSummaryDialogViewModel(IDialogViewFactory viewFactory, IDialogFactory dialogFactory, IDkpParserSettings settings, RaidEntries raidEntries, bool canUploadToServer)
        : base(viewFactory)
    {
        Title = Strings.GetString("LogParseSummaryDialogTitleText");
        _dialogFactory = dialogFactory;
        _settings = settings;
        _raidEntries = raidEntries;
        ShowUploadToServer = canUploadToServer;
        UploadToServer = canUploadToServer;

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
        DkpSpentCalls = new List<DkpEntry>(_raidEntries.DkpEntries.OrderBy(x => x.Timestamp));

        AddOrModifyAttendanceEntryCommand = new DelegateCommand(AddOrModifyAttendanceEntry);
        EditDkpSpentCommand = new DelegateCommand(EditDkpSpent, () => SelectedDkpspent != null).ObservesProperty(() => SelectedDkpspent);
        RemoveDkpSpentCommand = new DelegateCommand(RemoveDkpspent, () => SelectedDkpspent != null).ObservesProperty(() => SelectedDkpspent);
    }

    public DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    public ICollection<AttendanceEntry> AttendanceCalls
    {
        get => _attendanceCalls;
        private set => SetProperty(ref _attendanceCalls, value);
    }

    public ICollection<DkpEntry> DkpSpentCalls
    {
        get => _dkpSpentCalls;
        private set => SetProperty(ref _dkpSpentCalls, value);
    }

    public DelegateCommand EditDkpSpentCommand { get; }

    public DelegateCommand RemoveDkpSpentCommand { get; }

    public DkpEntry SelectedDkpspent
    {
        get => _selectedDkpspent;
        set => SetProperty(ref _selectedDkpspent, value);
    }

    public bool ShowUploadToServer { get; }

    public bool UploadToServer
    {
        get => _uploadToServer;
        set => SetProperty(ref _uploadToServer, value);
    }

    private void AddOrModifyAttendanceEntry()
    {
        IAttendanceEntryModiferDialogViewModel modifier = _dialogFactory.CreateAttendanceModifierDialogViewModel(_settings, _raidEntries);
        modifier.ShowDialog();

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
    }

    private void EditDkpSpent()
    {
        DkpEntry dkpSpentCall = SelectedDkpspent;
        IEditDkpspentDialogViewModel dkpspentEditor = _dialogFactory.CreateEditDkpspentDialogViewModel();
        dkpspentEditor.PlayerName = dkpSpentCall.PlayerName;
        dkpspentEditor.ItemName = dkpSpentCall.Item;
        dkpspentEditor.DkpSpent = dkpSpentCall.DkpSpent.ToString();

        if (dkpspentEditor.ShowDialog() != true)
            return;

        if (!int.TryParse(dkpspentEditor.DkpSpent, out int parsedDkp))
        {
            MessageBox.Show(string.Format(Strings.GetString("DkpSpentErrorFormatText"), dkpspentEditor.DkpSpent.ToString()), Strings.GetString("DkpSpentError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        dkpSpentCall.PlayerName = dkpspentEditor.PlayerName;
        dkpSpentCall.Item = dkpspentEditor.ItemName;
        dkpSpentCall.DkpSpent = parsedDkp;

        DkpSpentCalls = new List<DkpEntry>(_raidEntries.DkpEntries.OrderBy(x => x.Timestamp));
    }

    private void RemoveDkpspent()
    {
        DkpEntry dkpSpentCall = SelectedDkpspent;
        SelectedDkpspent = null;
        _raidEntries.DkpEntries.Remove(dkpSpentCall);

        DkpSpentCalls = new List<DkpEntry>(_raidEntries.DkpEntries.OrderBy(x => x.Timestamp));
    }
}

public interface IFinalSummaryDialogViewModel : IDialogViewModel
{
    DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    ICollection<AttendanceEntry> AttendanceCalls { get; }

    ICollection<DkpEntry> DkpSpentCalls { get; }

    DelegateCommand EditDkpSpentCommand { get; }

    DelegateCommand RemoveDkpSpentCommand { get; }

    DkpEntry SelectedDkpspent { get; set; }

    bool ShowUploadToServer { get; }

    bool UploadToServer { get; set; }
}
