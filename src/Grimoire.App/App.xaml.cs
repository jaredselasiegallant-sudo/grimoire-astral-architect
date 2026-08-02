using System.IO;
using System.Windows;

namespace Grimoire.App;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _window = new MainWindow();
            _window.Show();
        }
        catch (Exception ex)
        {
            LogCrash("OnStartup", ex);
            throw;
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash("DispatcherUnhandledException", e.Exception);
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
