using IconsSaver.Core;
using IconsSaver.Core.Displays;
using IconsSaver.Core.Models;

namespace IconsSaver.App;

/// <summary>
/// The notification area icon and its menu.
/// </summary>
/// <remarks>
/// This is the interface most of the time: the window is for browsing and managing layouts, but
/// "something just scrambled my icons, put them back" wants to be two clicks from anywhere.
/// </remarks>
internal sealed class TrayController : IDisposable
{
    /// <summary>How many recent layouts to offer directly in the menu.</summary>
    private const int RecentLayoutCount = 8;

    private readonly Workspace _workspace;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu = new();
    private bool _disposed;

    public TrayController(Workspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

        _notifyIcon = new NotifyIcon
        {
            Icon = AppIcons.Tray,
            Text = AppInfo.Name,
            Visible = true,
            ContextMenuStrip = _menu,
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);

        // Rebuilt on every open, so the recent list and the display warning are never stale.
        _menu.Opening += (_, _) => RebuildMenu();
        RebuildMenu();
    }

    /// <summary>The user asked to open the main window.</summary>
    public event EventHandler? OpenRequested;

    /// <summary>The user asked to capture the desktop.</summary>
    public event EventHandler? SaveRequested;

    /// <summary>The user asked to restore a specific layout.</summary>
    public event EventHandler<LayoutSnapshot>? RestoreRequested;

    /// <summary>The user asked to restore whichever layout fits the current displays.</summary>
    public event EventHandler? RestoreBestRequested;

    /// <summary>The user asked to open the settings dialog.</summary>
    public event EventHandler? SettingsRequested;

    /// <summary>The user asked to quit.</summary>
    public event EventHandler? ExitRequested;

    /// <summary>Shows a notification balloon, if the user has them switched on.</summary>
    public void Notify(string title, string message)
    {
        if (!_workspace.Settings.ShowNotifications || _disposed)
        {
            return;
        }

        _notifyIcon.ShowBalloonTip(4000, title, message, ToolTipIcon.Info);
    }

    /// <summary>Updates the tooltip text shown when hovering the tray icon.</summary>
    public void SetTooltip(string text)
    {
        // NotifyIcon.Text throws above 63 characters, and the shell truncates anyway.
        _notifyIcon.Text = text.Length <= 63 ? text : text[..60] + "...";
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        var open = new ToolStripMenuItem($"Open {AppInfo.Name}", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty))
        {
            Font = new Font(_menu.Font, FontStyle.Bold),
        };

        _menu.Items.Add(open);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Save layout now", null, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(new ToolStripMenuItem("Restore best match", null, (_, _) => RestoreBestRequested?.Invoke(this, EventArgs.Empty)));

        _menu.Items.Add(BuildRecentMenu());

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Settings...", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty)));
    }

    private ToolStripMenuItem BuildRecentMenu()
    {
        var recent = new ToolStripMenuItem("Restore layout");
        IReadOnlyList<SnapshotListItem> items = _workspace.AllSnapshots();

        if (items.Count == 0)
        {
            recent.DropDownItems.Add(new ToolStripMenuItem("Nothing saved yet") { Enabled = false });
            return recent;
        }

        DisplayFingerprint current = DisplayEnumerator.Capture();

        foreach (SnapshotListItem item in items.Take(RecentLayoutCount))
        {
            LayoutSnapshot snapshot = item.Snapshot;
            bool matches = current.Matches(snapshot.Display);

            var entry = new ToolStripMenuItem(
                $"{snapshot.DisplayName}  ({snapshot.IconCount} icons)",
                null,
                (_, _) => RestoreRequested?.Invoke(this, snapshot));

            if (!matches)
            {
                entry.ToolTipText = $"Saved on a different display setup: {snapshot.Display.Describe()}";
                entry.ForeColor = SystemColors.GrayText;
            }

            recent.DropDownItems.Add(entry);
        }

        return recent;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Hiding first stops a ghost icon being left behind in the notification area.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
