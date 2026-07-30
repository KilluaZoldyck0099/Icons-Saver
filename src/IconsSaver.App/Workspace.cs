using IconsSaver.Core;
using IconsSaver.Core.Diagnostics;
using IconsSaver.Core.Models;
using IconsSaver.Core.Services;
using IconsSaver.Core.Storage;

namespace IconsSaver.App;

/// <summary>
/// The application's shared state and the operations both the window and the tray menu invoke.
/// </summary>
/// <remarks>
/// Without this the tray menu and the main window would each grow their own copy of "save a layout,
/// then refresh whatever is showing it", and the two would drift apart. Everything that changes
/// state goes through here and announces itself with <see cref="SnapshotsChanged"/>.
/// </remarks>
internal sealed class Workspace
{
    private readonly SettingsStore _settingsStore;

    public Workspace()
    {
        _settingsStore = new SettingsStore();
        Settings = _settingsStore.Load();
        AppLog.Enabled = Settings.LoggingEnabled;

        Layouts = new LayoutService(new SnapshotStore(AppPaths.SnapshotsFolder));
        AutoSnapshots = new SnapshotStore(AppPaths.AutoSnapshotsFolder);
    }

    /// <summary>Capture and restore, over the manual layout folder.</summary>
    public LayoutService Layouts { get; }

    /// <summary>The rolling automatic snapshot folder.</summary>
    public SnapshotStore AutoSnapshots { get; }

    /// <summary>The current settings. Replaced wholesale by <see cref="ApplySettings"/>.</summary>
    public AppSettings Settings { get; private set; }

    /// <summary>Raised whenever the set of saved layouts changed.</summary>
    public event EventHandler? SnapshotsChanged;

    /// <summary>Raised with a short line of text worth showing the user.</summary>
    public event EventHandler<string>? Status;

    /// <summary>Every layout from both folders, newest first.</summary>
    public IReadOnlyList<SnapshotListItem> AllSnapshots()
    {
        var items = new List<SnapshotListItem>();

        items.AddRange(Layouts.Snapshots.LoadAll().Select(s => new SnapshotListItem(s, Layouts.Snapshots, false)));
        items.AddRange(AutoSnapshots.LoadAll().Select(s => new SnapshotListItem(s, AutoSnapshots, true)));

        return items
            .OrderByDescending(i => i.Snapshot.CapturedAt)
            .ToList();
    }

    /// <summary>Captures the desktop and saves it under <paramref name="name"/>.</summary>
    public LayoutSnapshot? SaveNow(string name)
    {
        try
        {
            LayoutSnapshot snapshot = Layouts.CaptureAndSave(name);
            Report($"Saved '{snapshot.DisplayName}' with {snapshot.IconCount} icons.");
            SnapshotsChanged?.Invoke(this, EventArgs.Empty);
            return snapshot;
        }
        catch (DesktopInteropException exception)
        {
            Report($"Could not read the desktop: {exception.Message}");
            return null;
        }
    }

    /// <summary>Restores a specific layout.</summary>
    public RestoreReport? Restore(LayoutSnapshot snapshot, RestoreOptions options)
    {
        try
        {
            RestoreReport report = Layouts.Restore(snapshot, options);
            Report(report.Outcome == RestoreOutcome.SkippedDisplayMismatch
                ? $"'{snapshot.DisplayName}' belongs to a different display setup."
                : report.Summarize());

            return report;
        }
        catch (DesktopInteropException exception)
        {
            Report($"Could not reach the desktop: {exception.Message}");
            return null;
        }
    }

    /// <summary>
    /// Restores the newest layout that belongs to the display setup currently in use.
    /// </summary>
    public RestoreReport? RestoreBestMatch(bool automatic)
    {
        LayoutSnapshot? snapshot = Layouts.FindBestForCurrentDisplay(
            AllSnapshots().Select(i => i.Snapshot));

        if (snapshot is null)
        {
            Report("No saved layout matches the current display setup.");
            return null;
        }

        return Restore(snapshot, automatic ? RestoreOptions.Automatic : RestoreOptions.Manual);
    }

    /// <summary>Replaces the settings and writes them to disk.</summary>
    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings = settings;
        AppLog.Enabled = settings.LoggingEnabled;
        _settingsStore.Save(settings);
    }

    /// <summary>Announces that layouts changed, for callers that modified a store directly.</summary>
    public void NotifySnapshotsChanged() => SnapshotsChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>Announces a status line.</summary>
    public void Report(string message) => Status?.Invoke(this, message);
}
