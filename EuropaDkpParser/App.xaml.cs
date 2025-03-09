// -----------------------------------------------------------------------
// App.xaml.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser;

using System.IO;
using System.Windows;
using System.Windows.Threading;
using DkpParser;
using EuropaDkpParser.Utility;
using EuropaDkpParser.ViewModels;
using EuropaDkpParser.Views;
using Gjeltema.Logging;

public partial class App : Application
{
    private const string DkpCharactersFilePath = "DkpServerCharacters.txt";
    private const string ItemLinkIdsFilePath = "ItemLinkIDs.txt";
    private const string RaidValuesFilePath = "RaidValues.txt";
    private const string SettingsFilePath = "Settings.txt";
    private const string ZoneIdMappingFilePath = "Zones.txt";
    private static readonly LogFormatter LogFormatter =
         (logLevel, message) => $"{DateTime.Now:HH:mm:ss} {logLevel.ToString().ToUpper()} {message}";
    private IDkpParserSettings _settings;
    private ShellView _shellView;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += DispatcherUnhandledExceptionHandler;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledExceptionHandler;

        DialogFactory dialogFactory = new(new DialogViewFactory());
        MessageDialog.Initialize(dialogFactory);

        _settings = new DkpParserSettings(SettingsFilePath, RaidValuesFilePath, ItemLinkIdsFilePath, DkpCharactersFilePath, ZoneIdMappingFilePath);

        _settings.LoadBaseSettings();

        InitializeLogging();

        try
        {
            _settings.LoadOtherFileSettings();
        }
        catch (FileNotFoundException fnf)
        {
            MessageBox.Show(
               $"{fnf.FileName} was unable to be loaded. Obtain a correct version of this file and place it in the same folder as this executable.",
               "Configuration File Not Loaded",
               MessageBoxButton.OK,
               MessageBoxImage.Error);

            Current?.Shutdown(1);
            return;
        }

        if (_settings.UseLightMode)
        {
            Uri lightMode = new("Resources/GenericLight.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = lightMode });
            Log.Debug($"Light Mode enabled");
        }
        else
        {
            Uri darkMode = new("Resources/Generic.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = darkMode });
            Log.Debug($"Dark Mode enabled");
        }

        OverlayFactory overlayFactory = new(new OverlayViewFactory());
        WindowFactory windowFactory = new(new WindowViewFactory());

        var shellViewModel = new ShellViewModel(_settings, dialogFactory, overlayFactory, windowFactory);
        _shellView = new ShellView(shellViewModel);
        MainWindow = _shellView;
        MainWindow.Show();

        base.OnStartup(e);
    }

    private static void CurrentDomainUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        => HandleFatalException(e.ExceptionObject as Exception);

    private static void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleFatalException(e.Exception);
        e.Handled = true;
    }

    private static void HandleFatalException(Exception ex)
    {
        Log.Critical($"Fatal exception: {ex?.ToLogMessage()}");
        MessageBox.Show(ex.Message);
        Current?.Shutdown(1);
    }

    private static void HandleLoggingError(object sender, BackgroundLogErrorEventArgs e)
        => MessageDialog.ShowDialog($"Logger error: {e.ErrorException}", "Logger Error");

    private void InitializeLogging()
    {
        string logFileName = $"{DateTime.Now:MMdd}_ParserLog.txt";
        string currentDirectory = AppContext.BaseDirectory;
        string logDirectory = Path.Combine(currentDirectory, "Logs");
        if (!Directory.Exists(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
            }
            catch
            {
                return;
            }
        }

        string logFilePath = Path.Combine(logDirectory, logFileName);

        ILogTargetFactory logFactory = new LogTargetFactory();
        ILogTarget simpleAsyncLogTarget = logFactory.CreateAsyncSimpleLogTarget(logFilePath, LogFormatter);
        BackgroundLogTarget backgroundLogTarget = (BackgroundLogTarget)simpleAsyncLogTarget;
        backgroundLogTarget.ErrorLoggingMessage += HandleLoggingError;

        simpleAsyncLogTarget.LoggingLevel = _settings.LoggingLevel;

        Log.Logger = new SingleLogger();
        Log.Logger.Default = simpleAsyncLogTarget;

        Log.Info($"Logging started. LogLevel set to: {Log.Logger.Default.LoggingLevel}");
    }
}
