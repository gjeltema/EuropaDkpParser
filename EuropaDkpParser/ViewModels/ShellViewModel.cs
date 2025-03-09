// -----------------------------------------------------------------------
// ShellViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;

internal sealed class ShellViewModel : EuropaViewModelBase, IShellViewModel
{
    private readonly IDkpParserSettings _settings;
    private int _windowLocationX;
    private int _windowLocationY;

    internal ShellViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory, IOverlayFactory overlayFactory, IWindowFactory windowFactory)
    {
        TitleText = Strings.GetString("MainWindowTitleText") + " " + Strings.GetString("Version");

        SimpleStartDisplayVM = new SimpleStartDisplayViewModel(settings, dialogFactory, overlayFactory, windowFactory);

        WindowLocationX = settings.MainWindowX;
        WindowLocationY = settings.MainWindowY;
        _settings = settings;
    }

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

    public void HandleClosed(int left, int top)
    {
        if (_settings.MainWindowX == left && _settings.MainWindowY == top)
            return;

        _settings.MainWindowX = left;
        _settings.MainWindowY = top;
        _settings.SaveSettings();
    }
}

public interface IShellViewModel : IEuropaViewModel
{
    ISimpleStartDisplayViewModel SimpleStartDisplayVM { get; }

    string TitleText { get; }

    bool UseAdvancedDialog { get; }

    int WindowLocationX { get; set; }

    int WindowLocationY { get; set; }
}
