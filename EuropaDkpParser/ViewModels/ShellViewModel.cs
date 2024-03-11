// -----------------------------------------------------------------------
// ShellViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;

internal sealed class ShellViewModel : EuropaViewModelBase, IShellViewModel
{
    internal ShellViewModel(IDkpParserSettings settings)
    {
        MainDisplayVM = new MainDisplayViewModel(settings);
        WindowLocationX = settings.MainWindowX;
        WindowLocationY = settings.MainWindowY;
    }

    public IMainDisplayViewModel MainDisplayVM { get; }

    public int WindowLocationX { get; }

    public int WindowLocationY { get; }
}

public interface IShellViewModel : IEuropaViewModel
{
    IMainDisplayViewModel MainDisplayVM { get; }

    int WindowLocationX { get; }

    int WindowLocationY { get; }
}
