// -----------------------------------------------------------------------
// SimpleStartDisplayViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.IO;
using System.Windows;
using DkpParser;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal sealed class SimpleStartDisplayViewModel : EuropaViewModelBase, ISimpleStartDisplayViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly DkpLogGenerator _logGenerator;
    private readonly IOverlayFactory _overlayFactory;
    private readonly IDkpParserSettings _settings;
    private readonly IWindowFactory _windowFactory;
    private bool _ableToUpload;
    private ILiveLogTrackingViewModel _biddingDialogVM;

    internal SimpleStartDisplayViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory, IOverlayFactory overlayFactory, IWindowFactory windowFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
        _overlayFactory = overlayFactory;
        _windowFactory = windowFactory;

        OpenSettingsCommand = new DelegateCommand(OpenSettingsDialog);
        OpenArchiveFilesCommand = new DelegateCommand(OpenArchiveFilesDialog);
        OpenDkpParserCommand = new DelegateCommand(OpenDkpParserDialog);
        UploadGeneratedLogCommand = new DelegateCommand(UploadGeneratedLog);
        OpenOtherParserCommand = new DelegateCommand(OpenOtherParserDialog);
        OpenBiddingTrackerDialogCommand = new DelegateCommand(OpenBiddingTrackerDialog);

        _logGenerator = new DkpLogGenerator(settings, dialogFactory);
        AbleToUpload = _settings.IsApiConfigured;
    }

    public bool AbleToUpload
    {
        get => _ableToUpload;
        set => SetProperty(ref _ableToUpload, value);
    }

    public DelegateCommand OpenArchiveFilesCommand { get; }

    public DelegateCommand OpenBiddingTrackerDialogCommand { get; }

    public DelegateCommand OpenDkpParserCommand { get; }

    public DelegateCommand OpenOtherParserCommand { get; }

    public DelegateCommand OpenSettingsCommand { get; }

    public DelegateCommand UploadGeneratedLogCommand { get; }

    private void OpenArchiveFilesDialog()
    {
        IFileArchiveDialogViewModel fileArchiveDialog = _dialogFactory.CreateFileArchiveDialogViewModel(_settings);
        if (fileArchiveDialog.ShowDialog() != true)
            return;

        fileArchiveDialog.UpdateSettings(_settings);
    }

    private void OpenBiddingTrackerDialog()
    {
        _biddingDialogVM = _windowFactory.CreateLiveLogTrackingViewModel(_settings, _dialogFactory, _overlayFactory, _windowFactory);
        _biddingDialogVM.Show();
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

        AbleToUpload = _settings.IsApiConfigured;
    }

    private async void UploadGeneratedLog()
        => await UploadGeneratedLogAsync();

    private async Task UploadGeneratedLogAsync()
    {
        var fileDialog = new Microsoft.Win32.OpenFileDialog()
        {
            Title = "Select Generated Log File"
        };

        if (fileDialog.ShowDialog() != true)
            return;

        string generatedLogFile = fileDialog.FileName;
        if (!File.Exists(generatedLogFile))
        {
            MessageBox.Show($"{generatedLogFile} does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await _logGenerator.UploadGeneratedLogFile(generatedLogFile);
    }
}

public interface ISimpleStartDisplayViewModel : IEuropaViewModel
{
    bool AbleToUpload { get; set; }

    DelegateCommand OpenArchiveFilesCommand { get; }

    DelegateCommand OpenBiddingTrackerDialogCommand { get; }

    DelegateCommand OpenDkpParserCommand { get; }

    DelegateCommand OpenOtherParserCommand { get; }

    DelegateCommand OpenSettingsCommand { get; }

    DelegateCommand UploadGeneratedLogCommand { get; }
}
