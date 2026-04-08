// -----------------------------------------------------------------------
// SimpleStartDisplayViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System;
using System.IO;
using System.Windows;
using DkpParser;
using DkpParser.LiveTracking;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal sealed class SimpleStartDisplayViewModel : EuropaViewModelBase, ISimpleStartDisplayViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly DkpLogGenerator _logGenerator;
    private readonly IOverlayFactory _overlayFactory;
    private readonly IDkpParserSettings _settings;
    private readonly IWindowFactory _windowFactory;
    private static bool _raCalculatorInitialized = false;
    private bool _ableToUpload;
    private ILiveLogTrackingViewModel _adminBiddingDialogVM;
    private ISimpleBidTrackerViewModel _simpleBidTrackerVM;

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
        OpenAdminBiddingTrackerDialogCommand = new DelegateCommand(OpenBiddingTrackerDialog, () => _adminBiddingDialogVM == null && _simpleBidTrackerVM == null);
        OpenSimpleBiddingTrackerDialogCommand = new DelegateCommand(OpenSimpleBidTrackerDialog, () => _adminBiddingDialogVM == null && _simpleBidTrackerVM == null);

        _logGenerator = new DkpLogGenerator(settings, dialogFactory);
        AbleToUpload = _settings.IsApiConfigured;
    }

    public bool AbleToUpload
    {
        get => _ableToUpload;
        set => SetProperty(ref _ableToUpload, value);
    }

    public DelegateCommand OpenAdminBiddingTrackerDialogCommand { get; }

    public DelegateCommand OpenArchiveFilesCommand { get; }

    public DelegateCommand OpenDkpParserCommand { get; }

    public DelegateCommand OpenOtherParserCommand { get; }

    public DelegateCommand OpenSettingsCommand { get; }

    public DelegateCommand OpenSimpleBiddingTrackerDialogCommand { get; }

    public DelegateCommand UploadGeneratedLogCommand { get; }

    private void HandleAdminBiddingWindowClosed(object sender, EventArgs e)
    {
        _adminBiddingDialogVM = null;
        RaiseBiddingDialogCommandsCanExecuteChanged();
    }

    private void HandleSimpleBiddingWindowClosed(object sender, EventArgs e)
    {
        _simpleBidTrackerVM = null;
        RaiseBiddingDialogCommandsCanExecuteChanged();
    }

    private async Task InitializeRaidAttendanceCalculatorAsync()
    {
        if (_raCalculatorInitialized)
            return;

        DkpServer dkpServer = new(_settings);
        await RaidAttendanceCalculator.InitializeAsync(dkpServer, _settings);
        _raCalculatorInitialized = true;
    }

    private void OpenArchiveFilesDialog()
    {
        IFileArchiveDialogViewModel fileArchiveDialog = _dialogFactory.CreateFileArchiveDialogViewModel(_settings);
        if (fileArchiveDialog.ShowDialog() != true)
            return;

        fileArchiveDialog.UpdateSettings(_settings);
    }

    private async void OpenBiddingTrackerDialog()
        => await OpenBiddingTrackerDialogAsync();

    private async Task OpenBiddingTrackerDialogAsync()
    {
        _adminBiddingDialogVM = _windowFactory.CreateLiveLogTrackingViewModel(_settings, EqLogTailFile.Instance, RaidAttendanceCalculator.Instance, _dialogFactory, _overlayFactory, _windowFactory);
        _adminBiddingDialogVM.WindowClosing += HandleAdminBiddingWindowClosed;
        _adminBiddingDialogVM.Show();

        RaiseBiddingDialogCommandsCanExecuteChanged();
        await InitializeRaidAttendanceCalculatorAsync();
    }

    private void OpenDkpParserDialog()
    {
        Task.Run(() => InitializeRaidAttendanceCalculatorAsync());
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

    private async void OpenSimpleBidTrackerDialog()
        => await OpenSimpleBidTrackerDialogAsync();

    private async Task OpenSimpleBidTrackerDialogAsync()
    {
        _simpleBidTrackerVM = _windowFactory.CreateSimpleBidTrackerViewModel(_settings, EqLogTailFile.Instance, RaidAttendanceCalculator.Instance);
        _simpleBidTrackerVM.WindowClosing += HandleSimpleBiddingWindowClosed;
        _simpleBidTrackerVM.Show();

        RaiseBiddingDialogCommandsCanExecuteChanged();
        await InitializeRaidAttendanceCalculatorAsync();
    }

    private void RaiseBiddingDialogCommandsCanExecuteChanged()
    {
        OpenAdminBiddingTrackerDialogCommand.RaiseCanExecuteChanged();
        OpenSimpleBiddingTrackerDialogCommand.RaiseCanExecuteChanged();
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

        await InitializeRaidAttendanceCalculatorAsync();
        await _logGenerator.UploadGeneratedLogFileAsync(generatedLogFile);
    }
}

public interface ISimpleStartDisplayViewModel : IEuropaViewModel
{
    bool AbleToUpload { get; set; }

    DelegateCommand OpenAdminBiddingTrackerDialogCommand { get; }

    DelegateCommand OpenArchiveFilesCommand { get; }

    DelegateCommand OpenDkpParserCommand { get; }

    DelegateCommand OpenOtherParserCommand { get; }

    DelegateCommand OpenSettingsCommand { get; }

    DelegateCommand OpenSimpleBiddingTrackerDialogCommand { get; }

    DelegateCommand UploadGeneratedLogCommand { get; }
}
