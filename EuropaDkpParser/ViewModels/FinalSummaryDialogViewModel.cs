// -----------------------------------------------------------------------
// FinalSummaryDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using Prism.Commands;

internal sealed class FinalSummaryDialogViewModel : DialogViewModelBase, IFinalSummaryDialogViewModel
{
    internal FinalSummaryDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
    }

    public ICollection<string> AttendanceCalls { get; }
    public ICollection<string> DkpSpentCalls { get; }
    public DelegateCommand CreateLogFileCommand { get; }
}

public interface IFinalSummaryDialogViewModel : IDialogViewModel
{
    ICollection<string> AttendanceCalls { get; }
    ICollection<string> DkpSpentCalls { get; }
    DelegateCommand CreateLogFileCommand { get; }
}
