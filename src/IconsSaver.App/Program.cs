using IconsSaver.Core;
using IconsSaver.Core.Diagnostics;
using IconsSaver.Core.Storage;

namespace IconsSaver.App;

/// <summary>Entry point.</summary>
internal static class Program
{
    /// <summary>Passed by the startup registry entry so a login does not pop a window open.</summary>
    private const string TrayArgument = "--tray";

    [STAThread]
    private static void Main(string[] arguments)
    {
        try
        {
            EnvironmentGuard.EnsureSupportedProcess();
        }
        catch (PlatformNotSupportedException exception)
        {
            MessageBox.Show(exception.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var instance = new SingleInstance($"{AppInfo.FolderName}.SingleInstance");

        if (!instance.IsFirstInstance)
        {
            // Hand over to the copy that is already running instead of starting a second tray icon.
            SingleInstance.SignalExisting();
            return;
        }

        AppPaths.EnsureCreated();
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, e) => ReportCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ReportCrash(e.ExceptionObject as Exception);

        bool startHidden = arguments.Any(a => a.Equals(TrayArgument, StringComparison.OrdinalIgnoreCase));

        using var host = new AppHost(startHidden);
        Application.Run(host);
    }

    private static void ReportCrash(Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        AppLog.Error("Unhandled exception.", exception);

        MessageBox.Show(
            $"{AppInfo.Name} hit an unexpected problem.\n\n{exception.Message}\n\n" +
            $"Details were written to:\n{AppPaths.LogFile}",
            AppInfo.Name,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
