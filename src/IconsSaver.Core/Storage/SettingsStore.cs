using System.Text.Json;
using IconsSaver.Core.Diagnostics;

namespace IconsSaver.Core.Storage;

/// <summary>
/// Loads and saves <see cref="AppSettings"/>.
/// </summary>
public sealed class SettingsStore
{
    private readonly string _path;

    public SettingsStore()
        : this(AppPaths.SettingsFile)
    {
    }

    public SettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    /// <summary>Full path of the settings document.</summary>
    public string Path => _path;

    /// <summary>
    /// Reads the settings, falling back to defaults when the file is absent or damaged.
    /// </summary>
    /// <remarks>
    /// A broken settings file must never block startup: the worst outcome of ignoring it is that
    /// the user re-picks their preferences, which beats an app that refuses to launch.
    /// </remarks>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            AppSettings settings =
                JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), JsonSettings.Default)
                ?? new AppSettings();

            settings.Normalize();
            return settings;
        }
        catch (JsonException exception)
        {
            AppLog.Error("Settings file is not valid JSON; falling back to defaults.", exception);
            return new AppSettings();
        }
        catch (IOException exception)
        {
            AppLog.Error("Settings file could not be read; falling back to defaults.", exception);
            return new AppSettings();
        }
    }

    /// <summary>Writes the settings.</summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Normalize();
        AtomicFile.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonSettings.Default));
    }
}
