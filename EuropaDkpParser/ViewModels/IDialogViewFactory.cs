// -----------------------------------------------------------------------
// IDialogViewFactory.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

public interface IDialogViewFactory
{
    IDialogView CreateDialogView(IDialogViewModel dialogViewModel);
}
