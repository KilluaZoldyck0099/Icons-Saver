using System.Globalization;
using IconsSaver.Core;
using IconsSaver.Core.Displays;
using IconsSaver.Core.Models;
using IconsSaver.Core.Storage;

namespace IconsSaver.App;

/// <summary>
/// The main window: the list of saved layouts, a preview of the selected one, and the actions.
/// </summary>
internal sealed class MainForm : Form
{
    /// <summary>Width reserved for the preview and details column.</summary>
    private const int DetailsPanelWidth = 400;

    private readonly Workspace _workspace;

    private readonly ListView _list = new();
    private readonly DesktopPreview _preview = new();
    private readonly Label _details = new();
    private readonly ToolStripStatusLabel _status = new();
    private readonly SplitContainer _split = new();

    private readonly ToolStripButton _restoreButton = new();
    private readonly ToolStripButton _previewButton = new();
    private readonly ToolStripButton _renameButton = new();
    private readonly ToolStripButton _deleteButton = new();
    private readonly ToolStripButton _exportButton = new();

    private List<SnapshotListItem> _items = new();

    public MainForm(Workspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

        Text = AppInfo.Name;
        MinimumSize = new Size(880, 520);
        ClientSize = new Size(1040, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = AppIcons.Application;

        BuildLayout();
        WireEvents();
        Reload();
    }

    /// <summary>Raised when the user asks to open the settings dialog.</summary>
    public event EventHandler? SettingsRequested;

    /// <summary>Raised when the user asks to quit the whole application.</summary>
    public event EventHandler? ExitRequested;

    private void BuildLayout()
    {
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.Dock = DockStyle.Fill;
        _list.Columns.Add("Name", 200);
        _list.Columns.Add("Icons", 56, HorizontalAlignment.Right);
        _list.Columns.Add("Captured", 130);
        _list.Columns.Add("Display", 150);
        _list.Columns.Add("Source", 64);

        _preview.Dock = DockStyle.Fill;

        _details.Dock = DockStyle.Bottom;
        _details.Height = 190;
        _details.Padding = new Padding(12, 10, 12, 10);
        _details.Text = string.Empty;

        var rightPanel = new Panel { Dock = DockStyle.Fill };
        rightPanel.Controls.Add(_preview);
        rightPanel.Controls.Add(_details);

        // Sized and positioned before the minimums are applied. A fresh SplitContainer is 150px
        // wide, and its Panel1MinSize setter rejects any value that cannot fit inside that.
        _split.Size = ClientSize;
        _split.SplitterDistance = Math.Max(100, ClientSize.Width - DetailsPanelWidth);
        _split.Dock = DockStyle.Fill;
        _split.Panel1MinSize = 380;
        _split.Panel2MinSize = 340;

        // The details column has a fixed amount to say, so it keeps its width and the list of
        // layouts takes everything the window gains.
        _split.FixedPanel = FixedPanel.Panel2;
        _split.Panel1.Controls.Add(_list);
        _split.Panel2.Controls.Add(rightPanel);

        var toolStrip = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(6, 4, 6, 4),
            RenderMode = ToolStripRenderMode.System,
        };

        toolStrip.Items.Add(Button("Save now", OnSaveNow));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(Configure(_restoreButton, "Restore", OnRestore));
        toolStrip.Items.Add(Configure(_previewButton, "What would change?", OnPreviewChanges));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(Configure(_renameButton, "Rename", OnRename));
        toolStrip.Items.Add(Configure(_deleteButton, "Delete", OnDelete));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(Button("Import", OnImport));
        toolStrip.Items.Add(Configure(_exportButton, "Export", OnExport));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(Button("Settings", (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty)));
        toolStrip.Items.Add(Button("About", OnAbout));

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);

        Controls.Add(_split);
        Controls.Add(toolStrip);
        Controls.Add(statusStrip);
    }

    /// <summary>
    /// Places the splitter once the form has been laid out at its real size.
    /// </summary>
    /// <remarks>
    /// This has to wait for <see cref="Form.OnShown"/>. Earlier than that the split container still
    /// has its default width, and <see cref="SplitContainer.SplitterDistance"/> throws outright when
    /// the value it is given cannot satisfy both panel minimums at the current width.
    /// </remarks>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        int lowest = _split.Panel1MinSize;
        int highest = _split.Width - _split.Panel2MinSize;

        if (highest >= lowest)
        {
            _split.SplitterDistance = Math.Clamp(_split.Width - DetailsPanelWidth, lowest, highest);
        }
    }

    private void WireEvents()
    {
        _list.SelectedIndexChanged += (_, _) => OnSelectionChanged();
        _list.DoubleClick += OnRestore;

        _workspace.SnapshotsChanged += (_, _) => BeginInvoke(Reload);
        _workspace.Status += (_, message) => BeginInvoke(() => _status.Text = message);

        FormClosing += OnFormClosing;
    }

    /// <summary>Rereads both layout folders and rebuilds the list.</summary>
    public void Reload()
    {
        string? selectedId = SelectedItem()?.Snapshot.Id;

        _items = _workspace.AllSnapshots().ToList();
        DisplayFingerprint current = DisplayEnumerator.Capture();

        _list.BeginUpdate();
        _list.Items.Clear();

        foreach (SnapshotListItem item in _items)
        {
            LayoutSnapshot snapshot = item.Snapshot;
            bool matches = current.Matches(snapshot.Display);

            var row = new ListViewItem(new[]
            {
                snapshot.DisplayName,
                snapshot.IconCount.ToString(CultureInfo.CurrentCulture),
                snapshot.CapturedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture),
                matches ? snapshot.Display.Describe() : $"{snapshot.Display.Describe()} *",
                item.SourceLabel,
            })
            {
                Tag = item,
                ToolTipText = matches ? null : "Saved on a different display setup.",
            };

            if (!matches)
            {
                row.ForeColor = SystemColors.GrayText;
            }

            _list.Items.Add(row);
        }

        _list.EndUpdate();

        if (selectedId is not null)
        {
            SelectById(selectedId);
        }
        else if (_list.Items.Count > 0)
        {
            _list.Items[0].Selected = true;
        }

        OnSelectionChanged();
        _status.Text = _items.Count == 0
            ? "No layouts saved yet. Press 'Save now' to capture the desktop as it is."
            : $"{_items.Count} saved layouts.";
    }

    private void OnSelectionChanged()
    {
        SnapshotListItem? item = SelectedItem();

        _restoreButton.Enabled = item is not null;
        _previewButton.Enabled = item is not null;
        _renameButton.Enabled = item is not null;
        _deleteButton.Enabled = item is not null;
        _exportButton.Enabled = item is not null;

        _preview.Snapshot = item?.Snapshot;

        if (item is null)
        {
            _details.Text = string.Empty;
            return;
        }

        LayoutSnapshot snapshot = item.Snapshot;
        var lines = new List<string>
        {
            snapshot.DisplayName,
            string.Empty,
            $"Captured   {snapshot.CapturedAt.LocalDateTime:F}",
            $"Icons      {snapshot.IconCount}",
            $"Display    {snapshot.Display.Describe()}  [{snapshot.Display.Signature}]",
            $"View       {snapshot.ViewOptions}",
            $"Source     {item.SourceLabel}, Icons Saver {snapshot.AppVersion}",
        };

        if (!string.IsNullOrWhiteSpace(snapshot.Note))
        {
            lines.Add($"Note       {snapshot.Note}");
        }

        _details.Text = string.Join(Environment.NewLine, lines);
    }

    private void OnSaveNow(object? sender, EventArgs e)
    {
        string suggestion = DateTimeOffset.Now.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
        string? name = InputDialog.Ask(this, "Save layout", "Name for this layout:", suggestion);

        if (name is not null)
        {
            _workspace.SaveNow(name);
            Reload();
        }
    }

    private void OnRestore(object? sender, EventArgs e)
    {
        SnapshotListItem? item = SelectedItem();
        if (item is null)
        {
            return;
        }

        LayoutSnapshot snapshot = item.Snapshot;
        DisplayFingerprint current = DisplayEnumerator.Capture();

        if (!current.Matches(snapshot.Display))
        {
            DialogResult answer = MessageBox.Show(
                this,
                $"'{snapshot.DisplayName}' was saved on a different display setup " +
                $"({snapshot.Display.Describe()}). Some icons may land off screen.\n\nRestore it anyway?",
                AppInfo.Name,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                return;
            }
        }

        RestoreReport? report = _workspace.Restore(snapshot, RestoreOptions.Manual);
        if (report is null)
        {
            return;
        }

        if (report.AutoArrangeSuspended)
        {
            MessageBox.Show(
                this,
                report.Summarize() +
                "\n\n'Auto arrange icons' has been turned off, because with it on Windows would " +
                "immediately pack the icons back into a grid.",
                AppInfo.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void OnPreviewChanges(object? sender, EventArgs e)
    {
        SnapshotListItem? item = SelectedItem();
        if (item is null)
        {
            return;
        }

        RestoreReport? report = _workspace.Restore(item.Snapshot, RestoreOptions.Preview);
        if (report is null)
        {
            return;
        }

        var text = new List<string>
        {
            report.Summarize(),
            string.Empty,
            $"On the desktop now : {report.DesktopIconCount}",
            $"In this layout     : {report.SnapshotIconCount}",
            $"Would move         : {report.Matched - report.AlreadyInPlace}",
            $"Already in place   : {report.AlreadyInPlace}",
        };

        if (report.Missing.Count > 0)
        {
            text.Add(string.Empty);
            text.Add("No longer on the desktop:");
            text.AddRange(report.Missing.Take(12).Select(n => "  " + n));

            if (report.Missing.Count > 12)
            {
                text.Add($"  ... and {report.Missing.Count - 12} more");
            }
        }

        if (report.Unknown.Count > 0)
        {
            text.Add(string.Empty);
            text.Add("New since this layout was saved (these are left alone):");
            text.AddRange(report.Unknown.Take(12).Select(n => "  " + n));

            if (report.Unknown.Count > 12)
            {
                text.Add($"  ... and {report.Unknown.Count - 12} more");
            }
        }

        MessageBox.Show(this, string.Join(Environment.NewLine, text), "What would change?", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnRename(object? sender, EventArgs e)
    {
        SnapshotListItem? item = SelectedItem();
        if (item is null)
        {
            return;
        }

        string? name = InputDialog.Ask(this, "Rename layout", "New name:", item.Snapshot.Name);
        if (name is null)
        {
            return;
        }

        item.Snapshot.Name = name;
        item.Store.Save(item.Snapshot);
        Reload();
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        SnapshotListItem? item = SelectedItem();
        if (item is null)
        {
            return;
        }

        DialogResult answer = MessageBox.Show(
            this,
            $"Delete '{item.Snapshot.DisplayName}'?",
            AppInfo.Name,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes && item.Store.Delete(item.Snapshot.Id))
        {
            Reload();
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import layout",
            Filter = "Icons Saver layout (*.json)|*.json|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            LayoutSnapshot snapshot = SnapshotStore.Import(dialog.FileName);
            _workspace.Layouts.Snapshots.Save(snapshot);
            Reload();
            _status.Text = $"Imported '{snapshot.DisplayName}' with {snapshot.IconCount} icons.";
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            MessageBox.Show(this, exception.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnExport(object? sender, EventArgs e)
    {
        SnapshotListItem? item = SelectedItem();
        if (item is null)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export layout",
            Filter = "Icons Saver layout (*.json)|*.json",
            FileName = AppPaths.SanitizeFileName(item.Snapshot.DisplayName) + ".json",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SnapshotStore.Export(item.Snapshot, dialog.FileName);
            _status.Text = $"Exported to {dialog.FileName}.";
        }
        catch (IOException exception)
        {
            MessageBox.Show(this, exception.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        using var about = new AboutForm();
        about.ShowDialog(this);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // Closing the window of a tray utility should put it away, not end the session that is
        // watching for display changes. Quitting is an explicit choice from the tray menu.
        if (e.CloseReason == CloseReason.UserClosing && _workspace.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        if (e.CloseReason == CloseReason.UserClosing)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private SnapshotListItem? SelectedItem() =>
        _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as SnapshotListItem : null;

    private void SelectById(string id)
    {
        foreach (ListViewItem row in _list.Items)
        {
            if (row.Tag is SnapshotListItem item && item.Snapshot.Id == id)
            {
                row.Selected = true;
                row.EnsureVisible();
                return;
            }
        }
    }

    private static ToolStripButton Button(string text, EventHandler onClick)
    {
        var button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        button.Click += onClick;
        return button;
    }

    private static ToolStripButton Configure(ToolStripButton button, string text, EventHandler onClick)
    {
        button.Text = text;
        button.DisplayStyle = ToolStripItemDisplayStyle.Text;
        button.Enabled = false;
        button.Click += onClick;
        return button;
    }
}
