// -----------------------------------------------------------------------
// FinalSummaryDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class FinalSummaryDialogViewModel : DialogViewModelBase, IFinalSummaryDialogViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly RaidEntries _raidEntries;
    private ICollection<AttendanceEntry> _attendanceCalls;

    internal FinalSummaryDialogViewModel(IDialogViewFactory viewFactory, IDialogFactory dialogFactory, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("LogParseSummaryDialogTitleText");
        _dialogFactory = dialogFactory;
        _raidEntries = raidEntries;

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
        DkpSpentCalls = new List<DkpEntry>(_raidEntries.DkpEntries.OrderBy(x => x.Timestamp));

        AddOrModifyAttendanceEntryCommand = new DelegateCommand(AddOrModifyAttendanceEntry);
    }

    public DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    public ICollection<AttendanceEntry> AttendanceCalls
    {
        get => _attendanceCalls;
        set => SetProperty(ref _attendanceCalls, value);
    }

    public ICollection<DkpEntry> DkpSpentCalls { get; }

    private void AddOrModifyAttendanceEntry()
    {
        IAttendanceEntryModiferDialogViewModel modifier = _dialogFactory.CreateAttendanceModifierDialogViewModel(_raidEntries);
        modifier.ShowDialog();

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
    }
}

public interface IFinalSummaryDialogViewModel : IDialogViewModel
{
    DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    ICollection<AttendanceEntry> AttendanceCalls { get; }

    ICollection<DkpEntry> DkpSpentCalls { get; }
}
