// -----------------------------------------------------------------------
// ShellViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

internal sealed class ShellViewModel : EuropaViewModelBase, IShellViewModel
{
    internal ShellViewModel()
    {
        MainDisplayVM = new MainDisplayViewModel();
    }

    public IMainDisplayViewModel MainDisplayVM { get; }
}

public interface IShellViewModel : IEuropaViewModel
{
    IMainDisplayViewModel MainDisplayVM { get; }
}
