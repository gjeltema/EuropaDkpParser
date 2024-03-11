// -----------------------------------------------------------------------
// DialogFactory.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;

internal sealed class DialogFactory : IDialogFactory
{
    private readonly IDialogViewFactory ViewFactory;

    internal DialogFactory(IDialogViewFactory viewFactory)
    {
        ViewFactory = viewFactory;
    }

    public ILogSelectionViewModel CreateSettingsViewDialog(IDkpParserSettings settings)
        => new LogSelectionViewModel(ViewFactory, settings);
}

public interface IDialogFactory
{
    ILogSelectionViewModel CreateSettingsViewDialog(IDkpParserSettings settings);
}
