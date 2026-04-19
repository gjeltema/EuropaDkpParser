// -----------------------------------------------------------------------
// FinalSummaryDialogViewModel.cs Copyright 2026 Craig Gjeltema
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

    internal FinalSummaryDialogViewModel(IDialogViewFactory viewFactory, IDialogFactory dialogFactory, IDkpParserSettings settings, RaidEntries raidEntries, bool canUploadToServer)
        : base(viewFactory)
    {
        Title = Strings.GetString("LogParseSummaryDialogTitleText");

        Height = 610;

        _dialogFactory = dialogFactory;
        _settings = settings;
        _raidEntries = raidEntries;
        ShowUploadToServer = canUploadToServer;
        UploadToServer = canUploadToServer;

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
        DkpSpentCalls = new List<DkpEntry>(_raidEntries.DkpEntries.OrderBy(x => x.Timestamp));
        Transfers = new List<DkpTransfer>(_raidEntries.Transfers.OrderBy(x => x.Timestamp));

        AddOrModifyAttendanceEntryCommand = new DelegateCommand(AddOrModifyAttendanceEntry);
        EditDkpSpentCommand = new DelegateCommand(EditDkpSpent, () => SelectedDkpspent != null).ObservesProperty(() => SelectedDkpspent);
        EditAttendeesCommand = new DelegateCommand(EditAttendees);
        RemoveDkpSpentCommand = new DelegateCommand(RemoveDkpspent, () => SelectedDkpspent != null).ObservesProperty(() => SelectedDkpspent);
        RemoveTransferCommand = new DelegateCommand(RemoveTransfer, () => SelectedTransfer != null).ObservesProperty(() => SelectedTransfer);
    }

    public DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    public ICollection<AttendanceEntry> AttendanceCalls { get; private set => SetProperty(ref field, value); }

    public ICollection<DkpEntry> DkpSpentCalls { get; private set => SetProperty(ref field, value); }

    public DelegateCommand EditAttendeesCommand { get; }

    public DelegateCommand EditDkpSpentCommand { get; }

    public bool HasTransfers
        => Transfers.Count > 0;

    public DelegateCommand RemoveDkpSpentCommand { get; }

    public DelegateCommand RemoveTransferCommand { get; }

    public DkpEntry SelectedDkpspent { get; set => SetProperty(ref field, value); }

    public DkpTransfer SelectedTransfer { get; set => SetProperty(ref field, value); }

    public bool ShowUploadToServer { get; }

    public ICollection<DkpTransfer> Transfers { get; private set => SetProperty(ref field, value); }

    public bool UploadToServer { get; set => SetProperty(ref field, value); }

    private void AddOrModifyAttendanceEntry()
    {
        IAttendanceEntryModiferDialogViewModel modifier = _dialogFactory.CreateAttendanceModifierDialogViewModel(_settings, _raidEntries);
        modifier.ShowDialog();

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
    }

    private void EditAttendees()
    {
        IAttendeesModifierDialogViewModel modifier = _dialogFactory.CreateAttendeesModifierDialogViewModel(_raidEntries);
        modifier.ShowDialog();
    }

    private void EditDkpSpent()
    {
        DkpEntry dkpSpentCall = SelectedDkpspent;
        IEditDkpspentDialogViewModel dkpspentEditor = _dialogFactory.CreateEditDkpspentDialogViewModel();
        dkpspentEditor.PlayerName = dkpSpentCall.CharacterName;
        dkpspentEditor.ItemName = dkpSpentCall.Item;
        dkpspentEditor.DkpSpent = dkpSpentCall.DkpSpent.ToString();

        if (dkpspentEditor.ShowDialog() != true)
            return;

        if (!int.TryParse(dkpspentEditor.DkpSpent, out int parsedDkp))
        {
            MessageBox.Show(string.Format(Strings.GetString("DkpSpentErrorFormatText"), dkpspentEditor.DkpSpent.ToString()), Strings.GetString("DkpSpentError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        dkpSpentCall.CharacterName = dkpspentEditor.PlayerName;
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

    private void RemoveTransfer()
    {
        DkpTransfer selected = SelectedTransfer;
        if (selected == null)
            return;

        _raidEntries.Transfers.Remove(selected);
        Transfers = new List<DkpTransfer>(_raidEntries.Transfers.OrderBy(x => x.Timestamp));
        RaisePropertyChanged(nameof(HasTransfers));
    }
}

public interface IFinalSummaryDialogViewModel : IDialogViewModel
{
    DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    ICollection<AttendanceEntry> AttendanceCalls { get; }

    ICollection<DkpEntry> DkpSpentCalls { get; }

    DelegateCommand EditAttendeesCommand { get; }

    DelegateCommand EditDkpSpentCommand { get; }

    bool HasTransfers { get; }

    DelegateCommand RemoveDkpSpentCommand { get; }

    DelegateCommand RemoveTransferCommand { get; }

    DkpEntry SelectedDkpspent { get; set; }

    DkpTransfer SelectedTransfer { get; set; }

    bool ShowUploadToServer { get; }

    ICollection<DkpTransfer> Transfers { get; }

    bool UploadToServer { get; set; }
}
