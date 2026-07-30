using IconsSaver.Core.Diagnostics;
using Microsoft.Win32;

namespace IconsSaver.Core.Services;

/// <summary>
/// Registers the application to start with Windows.
/// </summary>
/// <remarks>
/// The per-user <c>Run</c> key is used rather than a scheduled task or the machine-wide key, because
/// desktop icon layouts are per-user data and none of this should ever need administrator rights.
/// </remarks>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>Whether the application is currently registered to start with Windows.</summary>
    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(AppInfo.StartupRegistryValueName) is not null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            AppLog.Error("Could not read the startup registry key.", exception);
            return false;
        }
    }

    /// <summary>Adds or removes the startup registration.</summary>
    /// <param name="enabled">Whether the app should start with Windows.</param>
    /// <param name="executablePath">Executable to register; defaults to the running one.</param>
    /// <returns><see langword="true"/> when the registry was updated successfully.</returns>
    public static bool SetEnabled(bool enabled, string? executablePath = null)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("The Run key could not be opened for writing.");

            if (enabled)
            {
                string path = executablePath ?? ResolveExecutablePath();

                // The path is quoted so a space in it cannot be mistaken for an argument separator.
                key.SetValue(AppInfo.StartupRegistryValueName, $"\"{path}\" --tray", RegistryValueKind.String);
                AppLog.Info($"Enabled start with Windows: {path}");
            }
            else
            {
                key.DeleteValue(AppInfo.StartupRegistryValueName, throwOnMissingValue: false);
                AppLog.Info("Disabled start with Windows.");
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            AppLog.Error("Could not update the startup registry key.", exception);
            return false;
        }
    }

    /// <summary>Path of the currently running executable.</summary>
    public static string ResolveExecutablePath() =>
        Environment.ProcessPath
        ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
        ?? throw new InvalidOperationException("The path of the running executable could not be determined.");
}
