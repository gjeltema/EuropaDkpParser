// -----------------------------------------------------------------------
// SimpleStartDisplayViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using Prism.Commands;

internal sealed class SimpleStartDisplayViewModel : EuropaViewModelBase, ISimpleStartDisplayViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;

    internal SimpleStartDisplayViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;

        OpenSettingsCommand = new DelegateCommand(OpenSettingsDialog);
        OpenArchiveFilesCommand = new DelegateCommand(OpenArchiveFilesDialog);
        OpenDkpParserCommand = new DelegateCommand(OpenDkpParserDialog);
        OpenOtherParserCommand = new DelegateCommand(OpenOtherParserDialog);
    }

    public DelegateCommand OpenArchiveFilesCommand { get; }

    public DelegateCommand OpenDkpParserCommand { get; }

    public DelegateCommand OpenOtherParserCommand { get; }

    public DelegateCommand OpenSettingsCommand { get; }

    private void OpenArchiveFilesDialog()
    {
        IFileArchiveDialogViewModel fileArchiveDialog = _dialogFactory.CreateFileArchiveDialogViewModel(_settings);
        if (fileArchiveDialog.ShowDialog() != true)
            return;

        fileArchiveDialog.UpdateSettings(_settings);
    }

    private void OpenDkpParserDialog()
    {
        IDkpParseDialogViewModel dkpDialog = _dialogFactory.CreateDkpParseDialogViewModel(_settings, _dialogFactory);
        dkpDialog.ShowDialog();
    }

    private void OpenOtherParserDialog()
    {
        IParserDialogViewModel parserDialog = _dialogFactory.CreateParserDialogViewModel(_settings, _dialogFactory);
        parserDialog.ShowDialog();
    }

    private void OpenSettingsDialog()
    {
        ILogSelectionViewModel settingsDialog = _dialogFactory.CreateSettingsViewDialogViewModel(_settings);
        if (settingsDialog.ShowDialog() != true)
            return;

        settingsDialog.UpdateSettings(_settings);
    }
}

public interface ISimpleStartDisplayViewModel : IEuropaViewModel
{
    DelegateCommand OpenArchiveFilesCommand { get; }

    DelegateCommand OpenDkpParserCommand { get; }

    DelegateCommand OpenOtherParserCommand { get; }

    DelegateCommand OpenSettingsCommand { get; }
}
