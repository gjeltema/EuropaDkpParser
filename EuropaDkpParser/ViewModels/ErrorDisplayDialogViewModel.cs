// -----------------------------------------------------------------------
// ErrorDisplayDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using Prism.Commands;

internal sealed class ErrorDisplayDialogViewModel : DialogViewModelBase, IErrorDisplayDialogViewModel
{
    internal ErrorDisplayDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
    }

    public string ErrorLogEntry { get; }
    public string ErrorMessageText { get; }
    public ICollection<string> ApprovedBossNames { get; }
    public string SelectedBossName { get; set; }
    public DelegateCommand MoveToNextErrorCommand { get; }
    public DelegateCommand FixErrorCommand { get; }
    public DelegateCommand FinishReviewingErrorsCommand { get; }
    public bool MoreErrorsRemaining { get; }
}

public interface IErrorDisplayDialogViewModel : IDialogViewModel
{ 
    string ErrorLogEntry { get; }
    string ErrorMessageText { get; }
    ICollection<string> ApprovedBossNames { get; }
    string SelectedBossName { get; set; }
    DelegateCommand MoveToNextErrorCommand { get; }
    DelegateCommand FixErrorCommand { get; }
    DelegateCommand FinishReviewingErrorsCommand { get; }
    bool MoreErrorsRemaining { get; }
}
