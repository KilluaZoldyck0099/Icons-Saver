namespace IconsSaver.Core.Storage;

/// <summary>
/// Where Icons Saver keeps its data.
/// </summary>
/// <remarks>
/// Everything lives under <c>%APPDATA%\IconsSaver</c> so the app stays portable-ish and never needs
/// administrator rights. Layouts are user data, not machine data, which is exactly what roaming
/// AppData is for.
/// </remarks>
public static class AppPaths
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>Root data folder.</summary>
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppInfo.FolderName);

    /// <summary>Folder holding one JSON document per saved layout.</summary>
    public static string SnapshotsFolder => Path.Combine(Root, "layouts");

    /// <summary>Folder holding automatic snapshots, kept apart so pruning is simple.</summary>
    public static string AutoSnapshotsFolder => Path.Combine(Root, "auto");

    /// <summary>Application settings document.</summary>
    public static string SettingsFile => Path.Combine(Root, "settings.json");

    /// <summary>Rolling diagnostic log.</summary>
    public static string LogFile => Path.Combine(Root, "logs", "iconssaver.log");

    /// <summary>Creates every folder the app writes to.</summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(SnapshotsFolder);
        Directory.CreateDirectory(AutoSnapshotsFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
    }

    /// <summary>
    /// Turns a user supplied layout name into something safe to put on disk.
    /// </summary>
    /// <remarks>
    /// Layout files are named by id rather than by title, so this is only used for exports, where
    /// the user does expect to recognise the file they just wrote.
    /// </remarks>
    public static string SanitizeFileName(string name, string fallback = "layout")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallback;
        }

        var builder = new System.Text.StringBuilder(name.Length);
        foreach (char character in name.Trim())
        {
            builder.Append(Array.IndexOf(InvalidFileNameChars, character) >= 0 ? '_' : character);
        }

        string sanitized = builder.ToString().Trim('.', ' ');
        return sanitized.Length == 0 ? fallback : sanitized;
    }
}
