using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SiamNetworkMonitor;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        base.OnStartup(e);
    }

    private static string ErrorPath => Path.Combine(AppContext.BaseDirectory, "app-error.txt");

    private static void WriteError(string source, Exception? ex)
    {
        try { File.AppendAllText(ErrorPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}"); }
        catch { }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteError("WPF Dispatcher", e.Exception);
        System.Windows.MessageBox.Show($"Siam Network Monitor could not continue.\n\n{e.Exception.Message}\n\nDetails were saved to:\n{ErrorPath}", "Siam Network Monitor", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        e.Handled = true;
        Shutdown(-1);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => WriteError("AppDomain", e.ExceptionObject as Exception);
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) { WriteError("Task", e.Exception); e.SetObserved(); }
}
