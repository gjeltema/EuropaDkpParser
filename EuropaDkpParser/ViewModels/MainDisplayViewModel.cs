// -----------------------------------------------------------------------
// MainDisplayViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using Prism.Commands;

internal sealed class MainDisplayViewModel : EuropaViewModelBase, IMainDisplayViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;

    internal MainDisplayViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
        OpenSettingsDialogCommand = new DelegateCommand(OpenSettingsDialog);
    }

    public string EndTimeText { get; set; }

    public DelegateCommand OpenSettingsDialogCommand { get; }

    public string OutputFile { get; set; }

    public DelegateCommand StartLogParseCommand { get; }

    public string StartTimeText { get; set; }

    private void OpenSettingsDialog()
    {
        ILogSelectionViewModel settingsDialog = _dialogFactory.CreateSettingsViewDialog(_settings);
        if (settingsDialog.ShowDialog() != true)
            return;

        _settings.EqDirectory = settingsDialog.EqDirectory;
        _settings.SelectedLogFiles = settingsDialog.SelectedCharacterLogFiles;
        _settings.SaveSettings();
    }
}

public interface IMainDisplayViewModel : IEuropaViewModel
{
    string EndTimeText { get; set; }

    DelegateCommand OpenSettingsDialogCommand { get; }

    string OutputFile { get; set; }

    DelegateCommand StartLogParseCommand { get; }

    string StartTimeText { get; set; }
}
