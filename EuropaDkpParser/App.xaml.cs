namespace EuropaDkpParser;

using System.Configuration;
using System.Windows;
using System.Windows.Threading;
using EuropaDkpParser.ViewModels;
using EuropaDkpParser.Views;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += DispatcherUnhandledExceptionHandler;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledExceptionHandler;

        var shellVM = new ShellViewModel();
        var shellView = new ShellView(shellVM);
        MainWindow = shellView;
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

    private static string GetAppConfigSetting(string key)
    {
        try
        {
            return ConfigurationManager.AppSettings[key] ?? string.Empty;
        }
        catch (ConfigurationErrorsException)
        {
            return string.Empty;
        }
    }

    private static void HandleFatalException(Exception ex)
    {
        //Log.Critical($"Critical error, application ending: {ex?.ToLogMessage()}");
        MessageBox.Show(ex.Message);
        Current?.Shutdown(1);
    }
}
