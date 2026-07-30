using System.Reflection;

namespace IconsSaver.Core;

/// <summary>
/// Identity of the application, shared by the tray app, the CLI and the on-disk format.
/// </summary>
public static class AppInfo
{
    /// <summary>Display name, as shown in the UI.</summary>
    public const string Name = "Icons Saver";

    /// <summary>Folder name used under <c>%APPDATA%</c>. Deliberately space-free.</summary>
    public const string FolderName = "IconsSaver";

    /// <summary>Value written to the <c>Run</c> registry key when "start with Windows" is on.</summary>
    public const string StartupRegistryValueName = "IconsSaver";

    /// <summary>Project home, shown in the about box.</summary>
    public const string ProjectUrl = "https://github.com/KilluaZoldyck0099/Icons-Saver";

    /// <summary>Copyright line, shown in the about box.</summary>
    public const string Copyright = "Copyright (c) 2026 KilluaZoldyck";

    /// <summary>The running assembly version, formatted as <c>major.minor.patch</c>.</summary>
    public static string Version =>
        typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>The informational version, which carries any build suffix.</summary>
    public static string FullVersion =>
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Version;
}
