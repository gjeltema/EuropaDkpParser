// -----------------------------------------------------------------------
// DialogFactory.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;

internal sealed class DialogFactory : IDialogFactory
{
    private readonly IDialogViewFactory _viewFactory;

    internal DialogFactory(IDialogViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public ICompletedDialogViewModel CreateCompletedDialog()
        => new CompletedDialogViewModel(_viewFactory);

    public IErrorDisplayDialogViewModel CreateErrorDisplayDialog()
        => new ErrorDisplayDialogViewModel(_viewFactory);

    public IFinalSummaryDialogViewModel CreateFinalSummaryDialog()
        => new FinalSummaryDialogViewModel(_viewFactory);

    public ILogSelectionViewModel CreateSettingsViewDialog(IDkpParserSettings settings)
        => new LogSelectionViewModel(_viewFactory, settings);
}

public interface IDialogFactory
{
    ICompletedDialogViewModel CreateCompletedDialog();

    IErrorDisplayDialogViewModel CreateErrorDisplayDialog();

    IFinalSummaryDialogViewModel CreateFinalSummaryDialog();

    ILogSelectionViewModel CreateSettingsViewDialog(IDkpParserSettings settings);
}
