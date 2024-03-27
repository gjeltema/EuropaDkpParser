// -----------------------------------------------------------------------
// ShellViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;

internal sealed class ShellViewModel : EuropaViewModelBase, IShellViewModel
{
    private int _windowLocationX;
    private int _windowLocationY;

    internal ShellViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        TitleText = Strings.GetString("MainWindowTitleText") + " " + Strings.GetString("Version");
        MainDisplayVM = new MainDisplayViewModel(settings, dialogFactory);
        WindowLocationX = settings.MainWindowX;
        WindowLocationY = settings.MainWindowY;
    }

    public IMainDisplayViewModel MainDisplayVM { get; }

    public string TitleText { get; }

    public int WindowLocationX
    {
        get => _windowLocationX;
        set => SetProperty(ref _windowLocationX, value);
    }

    public int WindowLocationY
    {
        get => _windowLocationY;
        set => SetProperty(ref _windowLocationY, value);
    }
}

public interface IShellViewModel : IEuropaViewModel
{
    public string TitleText { get; }

    IMainDisplayViewModel MainDisplayVM { get; }

    int WindowLocationX { get; set; }

    int WindowLocationY { get; set; }
}
