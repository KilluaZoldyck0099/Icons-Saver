namespace IconsSaver.Core.Storage;

/// <summary>
/// Everything the user can configure.
/// </summary>
/// <remarks>
/// Defaults are chosen so that a fresh install is useful without a trip to the settings dialog:
/// automatic snapshots on, automatic restore on, and a delay long enough for the shell to finish
/// rearranging before anything is put back.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>Register the app in the per-user <c>Run</c> key.</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Start straight to the tray instead of showing the window.</summary>
    public bool StartMinimized { get; set; } = true;

    /// <summary>Closing the window hides it instead of exiting.</summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>Take an automatic snapshot on a timer.</summary>
    public bool AutoSaveEnabled { get; set; } = true;

    /// <summary>Minutes between automatic snapshots.</summary>
    public int AutoSaveIntervalMinutes { get; set; } = 30;

    /// <summary>How many automatic snapshots to keep before the oldest is pruned.</summary>
    public int AutoSaveHistoryCount { get; set; } = 12;

    /// <summary>Skip an automatic snapshot when nothing has moved since the last one.</summary>
    public bool AutoSaveOnlyWhenChanged { get; set; } = true;

    /// <summary>Put icons back automatically after the display configuration changes.</summary>
    public bool AutoRestoreOnDisplayChange { get; set; } = true;

    /// <summary>
    /// Seconds to wait after a display change before restoring.
    /// </summary>
    /// <remarks>
    /// Windows raises the change notification before the shell has finished repacking icons onto
    /// the new arrangement. Restoring immediately means explorer overwrites the restore a moment
    /// later, so a pause is required rather than merely polite.
    /// </remarks>
    public int AutoRestoreDelaySeconds { get; set; } = 6;

    /// <summary>Only restore automatically onto a display setup the layout was captured on.</summary>
    public bool RestoreOnlyOnMatchingDisplays { get; set; } = true;

    /// <summary>Show a tray balloon after automatic actions.</summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>Write diagnostics to the log file.</summary>
    public bool LoggingEnabled { get; set; } = true;

    /// <summary>Layout used for automatic restores; empty means "the newest matching one".</summary>
    public string? PreferredSnapshotId { get; set; }

    /// <summary>Global hotkey for "save layout now", in the form <c>Ctrl+Shift+S</c>.</summary>
    public string SaveHotkey { get; set; } = "Ctrl+Alt+S";

    /// <summary>Global hotkey for "restore last layout".</summary>
    public string RestoreHotkey { get; set; } = "Ctrl+Alt+R";

    /// <summary>Whether the global hotkeys are registered at all.</summary>
    public bool HotkeysEnabled { get; set; } = true;

    /// <summary>Returns a copy, so a settings dialog can be cancelled without side effects.</summary>
    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    /// <summary>Clamps every numeric setting into a range the app can actually honour.</summary>
    public void Normalize()
    {
        AutoSaveIntervalMinutes = Math.Clamp(AutoSaveIntervalMinutes, 1, 24 * 60);
        AutoSaveHistoryCount = Math.Clamp(AutoSaveHistoryCount, 1, 200);
        AutoRestoreDelaySeconds = Math.Clamp(AutoRestoreDelaySeconds, 1, 120);
    }
}
