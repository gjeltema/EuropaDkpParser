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

        UseAdvancedDialog = settings.UseAdvancedDialog;
        if (UseAdvancedDialog)
            MainDisplayVM = new MainDisplayViewModel(settings, dialogFactory);
        else
            SimpleStartDisplayVM = new SimpleStartDisplayViewModel(settings, dialogFactory);

        WindowLocationX = settings.MainWindowX;
        WindowLocationY = settings.MainWindowY;
    }

    public IMainDisplayViewModel MainDisplayVM { get; }

    public ISimpleStartDisplayViewModel SimpleStartDisplayVM { get; }

    public string TitleText { get; }

    public bool UseAdvancedDialog { get; private set; }

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
    IMainDisplayViewModel MainDisplayVM { get; }

    ISimpleStartDisplayViewModel SimpleStartDisplayVM { get; }

    string TitleText { get; }

    bool UseAdvancedDialog { get; }

    int WindowLocationX { get; set; }

    int WindowLocationY { get; set; }
}
