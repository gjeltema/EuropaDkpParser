// -----------------------------------------------------------------------
// DialogViewFactory.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views
{
    using EuropaDkpParser.ViewModels;

    public sealed class DialogViewFactory : IDialogViewFactory
    {
        public IDialogView CreateDialogView(IDialogViewModel dialogViewModel)
            => new DialogView(dialogViewModel);
    }
}
