// -----------------------------------------------------------------------
// DialogFactory.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal sealed class DialogFactory : IDialogFactory
{
    private readonly IDialogViewFactory ViewFactory;

    internal DialogFactory(IDialogViewFactory viewFactory)
    {
        ViewFactory = viewFactory;
    }
}

public interface IDialogFactory
{
}
