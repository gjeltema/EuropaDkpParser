// -----------------------------------------------------------------------
// FinalSummaryDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;

internal sealed class FinalSummaryDialogViewModel : DialogViewModelBase, IFinalSummaryDialogViewModel
{
    private readonly RaidEntries _raidEntries;

    internal FinalSummaryDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries)
        : base(viewFactory)
    {
        Title = Strings.GetString("LogParseSummaryDialogTitleText");

        _raidEntries = raidEntries;

        AttendanceCalls = raidEntries.AttendanceEntries;
        DkpSpentCalls = raidEntries.DkpEntries;
    }

    public ICollection<AttendanceEntry> AttendanceCalls { get; }

    public ICollection<DkpEntry> DkpSpentCalls { get; }
}

public interface IFinalSummaryDialogViewModel : IDialogViewModel
{
    ICollection<AttendanceEntry> AttendanceCalls { get; }

    ICollection<DkpEntry> DkpSpentCalls { get; }
}
