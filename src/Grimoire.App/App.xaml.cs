using System.IO;
using Microsoft.UI.Xaml;

namespace Grimoire.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        this.InitializeComponent();

        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            LogCrash("OnLaunched", ex);
            throw;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogCrash("UnhandledException", e.Exception);
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogCrash("AppDomainUnhandledException", ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogCrash("UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static void LogCrash(string source, Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GrimoireAstralArchitect");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            var entry = $"[{DateTimeOffset.UtcNow:O}] {source}\n{ex}\n\n";
            File.AppendAllText(path, entry);
        }
        catch
        {
            // Swallow logging failures
        }
    }
}
