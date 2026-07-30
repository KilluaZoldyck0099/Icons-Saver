using System.Text.Json;
using IconsSaver.Core.Diagnostics;
using IconsSaver.Core.Models;

namespace IconsSaver.Core.Storage;

/// <summary>
/// Reads and writes saved layouts as one JSON document per layout.
/// </summary>
/// <remarks>
/// A folder of independent files is used rather than a single database so that a corrupt or
/// hand-edited layout can only ever cost the user that one layout, and so exporting is a copy.
/// </remarks>
public sealed class SnapshotStore
{
    private readonly string _folder;

    /// <summary>Creates a store over the default manual-layout folder.</summary>
    public SnapshotStore()
        : this(AppPaths.SnapshotsFolder)
    {
    }

    /// <summary>Creates a store over a specific folder.</summary>
    public SnapshotStore(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        _folder = folder;
    }

    /// <summary>The folder this store reads and writes.</summary>
    public string Folder => _folder;

    /// <summary>Full path of the document backing a given layout id.</summary>
    public string PathFor(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return Path.Combine(_folder, $"{id}.json");
    }

    /// <summary>Writes a layout, replacing any previous version of it.</summary>
    public void Save(LayoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Directory.CreateDirectory(_folder);
        string json = JsonSerializer.Serialize(snapshot, JsonSettings.Default);
        AtomicFile.WriteAllText(PathFor(snapshot.Id), json);

        AppLog.Info($"Saved layout '{snapshot.DisplayName}' ({snapshot.IconCount} icons) as {snapshot.Id}.");
    }

    /// <summary>Loads one layout, or <see langword="null"/> when it is missing or unreadable.</summary>
    public LayoutSnapshot? Load(string id)
    {
        string path = PathFor(id);
        return File.Exists(path) ? ReadFile(path) : null;
    }

    /// <summary>
    /// Loads every readable layout, newest first.
    /// </summary>
    /// <remarks>
    /// Unreadable files are skipped rather than thrown on: one bad document must not stop the app
    /// from opening.
    /// </remarks>
    public IReadOnlyList<LayoutSnapshot> LoadAll()
    {
        if (!Directory.Exists(_folder))
        {
            return Array.Empty<LayoutSnapshot>();
        }

        var snapshots = new List<LayoutSnapshot>();
        foreach (string path in Directory.EnumerateFiles(_folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            LayoutSnapshot? snapshot = ReadFile(path);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots
            .OrderByDescending(s => s.CapturedAt)
            .ToList();
    }

    /// <summary>Deletes a layout. Returns <see langword="false"/> when there was nothing to delete.</summary>
    public bool Delete(string id)
    {
        string path = PathFor(id);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        AppLog.Info($"Deleted layout {id}.");
        return true;
    }

    /// <summary>Writes a copy of a layout to an arbitrary path, for sharing or backup.</summary>
    public static void Export(LayoutSnapshot snapshot, string path)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonSettings.Default));
    }

    /// <summary>
    /// Reads a layout from an arbitrary path and gives it a fresh id so importing the same file
    /// twice does not overwrite the first import.
    /// </summary>
    /// <exception cref="InvalidDataException">The file is not a layout this build understands.</exception>
    public static LayoutSnapshot Import(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        LayoutSnapshot snapshot = ReadFile(path)
            ?? throw new InvalidDataException($"'{path}' is not a readable Icons Saver layout.");

        return new LayoutSnapshot
        {
            SchemaVersion = LayoutSnapshot.CurrentSchemaVersion,
            Id = Guid.NewGuid().ToString("n"),
            Name = snapshot.Name,
            Note = snapshot.Note,
            CapturedAt = snapshot.CapturedAt,
            IsAutomatic = false,
            IsPinned = snapshot.IsPinned,
            ViewOptions = snapshot.ViewOptions,
            Display = snapshot.Display,
            Icons = snapshot.Icons,
            AppVersion = snapshot.AppVersion,
        };
    }

    private static LayoutSnapshot? ReadFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            LayoutSnapshot? snapshot = JsonSerializer.Deserialize<LayoutSnapshot>(json, JsonSettings.Default);

            if (snapshot is null)
            {
                return null;
            }

            if (snapshot.SchemaVersion > LayoutSnapshot.CurrentSchemaVersion)
            {
                AppLog.Warn($"Skipping '{path}': written by a newer version (schema {snapshot.SchemaVersion}).");
                return null;
            }

            return snapshot;
        }
        catch (JsonException exception)
        {
            AppLog.Error($"Could not parse layout '{path}'.", exception);
            return null;
        }
        catch (IOException exception)
        {
            AppLog.Error($"Could not read layout '{path}'.", exception);
            return null;
        }
    }
}
