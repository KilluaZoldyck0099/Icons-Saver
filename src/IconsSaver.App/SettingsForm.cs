using IconsSaver.Core;
using IconsSaver.Core.Services;
using IconsSaver.Core.Storage;

namespace IconsSaver.App;

/// <summary>
/// The settings dialog.
/// </summary>
/// <remarks>
/// Edits a copy of the settings, so pressing Cancel really does leave everything as it was.
/// </remarks>
internal sealed class SettingsForm : Form
{
    private readonly AppSettings _draft;

    private readonly CheckBox _startWithWindows = new() { Text = "Start with Windows", AutoSize = true };
    private readonly CheckBox _startMinimized = new() { Text = "Start hidden in the notification area", AutoSize = true };
    private readonly CheckBox _minimizeToTray = new() { Text = "Closing the window hides it instead of quitting", AutoSize = true };

    private readonly CheckBox _autoSaveEnabled = new() { Text = "Take automatic snapshots", AutoSize = true };
    private readonly NumericUpDown _autoSaveInterval = new() { Minimum = 1, Maximum = 1440, Width = 80 };
    private readonly NumericUpDown _autoSaveHistory = new() { Minimum = 1, Maximum = 200, Width = 80 };
    private readonly CheckBox _autoSaveOnlyWhenChanged = new() { Text = "Skip a snapshot when nothing has moved", AutoSize = true };

    private readonly CheckBox _autoRestore = new() { Text = "Restore automatically after the displays change", AutoSize = true };
    private readonly NumericUpDown _autoRestoreDelay = new() { Minimum = 1, Maximum = 120, Width = 80 };
    private readonly CheckBox _requireMatchingDisplays = new() { Text = "Only onto a matching display setup", AutoSize = true };

    private readonly CheckBox _hotkeysEnabled = new() { Text = "Global hotkeys", AutoSize = true };
    private readonly TextBox _saveHotkey = new() { Width = 160 };
    private readonly TextBox _restoreHotkey = new() { Width = 160 };

    private readonly CheckBox _showNotifications = new() { Text = "Show a notification after automatic actions", AutoSize = true };
    private readonly CheckBox _loggingEnabled = new() { Text = "Write a diagnostic log", AutoSize = true };

    public SettingsForm(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _draft = settings.Clone();

        Text = $"{AppInfo.Name} settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 560);
        Icon = AppIcons.Application;

        BuildLayout();
        LoadFrom(_draft);
    }

    /// <summary>The edited settings. Only meaningful after the dialog returned OK.</summary>
    public AppSettings Result => _draft;

    private void BuildLayout()
    {
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16, 16, 16, 8),
        };

        layout.Controls.Add(Header("Startup"));
        layout.Controls.Add(_startWithWindows);
        layout.Controls.Add(_startMinimized);
        layout.Controls.Add(_minimizeToTray);

        layout.Controls.Add(Header("Automatic snapshots"));
        layout.Controls.Add(_autoSaveEnabled);
        layout.Controls.Add(Row("Every", _autoSaveInterval, "minutes"));
        layout.Controls.Add(Row("Keep the newest", _autoSaveHistory, "snapshots"));
        layout.Controls.Add(_autoSaveOnlyWhenChanged);

        layout.Controls.Add(Header("Automatic restore"));
        layout.Controls.Add(_autoRestore);
        layout.Controls.Add(Row("Wait", _autoRestoreDelay, "seconds for the shell to settle first"));
        layout.Controls.Add(_requireMatchingDisplays);

        layout.Controls.Add(Header("Hotkeys"));
        layout.Controls.Add(_hotkeysEnabled);
        layout.Controls.Add(Row("Save layout", _saveHotkey, string.Empty));
        layout.Controls.Add(Row("Restore layout", _restoreHotkey, string.Empty));

        layout.Controls.Add(Header("Other"));
        layout.Controls.Add(_showNotifications);
        layout.Controls.Add(_loggingEnabled);

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(96, 30) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(96, 30) };
        ok.Click += (_, _) => SaveTo(_draft);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(16, 8, 16, 8),
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(layout);
        Controls.Add(buttons);

        AcceptButton = ok;
        CancelButton = cancel;

        _autoSaveEnabled.CheckedChanged += (_, _) => UpdateEnabledState();
        _autoRestore.CheckedChanged += (_, _) => UpdateEnabledState();
        _hotkeysEnabled.CheckedChanged += (_, _) => UpdateEnabledState();
    }

    private void LoadFrom(AppSettings settings)
    {
        _startWithWindows.Checked = StartupManager.IsEnabled();
        _startMinimized.Checked = settings.StartMinimized;
        _minimizeToTray.Checked = settings.MinimizeToTray;

        _autoSaveEnabled.Checked = settings.AutoSaveEnabled;
        _autoSaveInterval.Value = settings.AutoSaveIntervalMinutes;
        _autoSaveHistory.Value = settings.AutoSaveHistoryCount;
        _autoSaveOnlyWhenChanged.Checked = settings.AutoSaveOnlyWhenChanged;

        _autoRestore.Checked = settings.AutoRestoreOnDisplayChange;
        _autoRestoreDelay.Value = settings.AutoRestoreDelaySeconds;
        _requireMatchingDisplays.Checked = settings.RestoreOnlyOnMatchingDisplays;

        _hotkeysEnabled.Checked = settings.HotkeysEnabled;
        _saveHotkey.Text = settings.SaveHotkey;
        _restoreHotkey.Text = settings.RestoreHotkey;

        _showNotifications.Checked = settings.ShowNotifications;
        _loggingEnabled.Checked = settings.LoggingEnabled;

        UpdateEnabledState();
    }

    private void SaveTo(AppSettings settings)
    {
        settings.StartMinimized = _startMinimized.Checked;
        settings.MinimizeToTray = _minimizeToTray.Checked;

        settings.AutoSaveEnabled = _autoSaveEnabled.Checked;
        settings.AutoSaveIntervalMinutes = (int)_autoSaveInterval.Value;
        settings.AutoSaveHistoryCount = (int)_autoSaveHistory.Value;
        settings.AutoSaveOnlyWhenChanged = _autoSaveOnlyWhenChanged.Checked;

        settings.AutoRestoreOnDisplayChange = _autoRestore.Checked;
        settings.AutoRestoreDelaySeconds = (int)_autoRestoreDelay.Value;
        settings.RestoreOnlyOnMatchingDisplays = _requireMatchingDisplays.Checked;

        settings.HotkeysEnabled = _hotkeysEnabled.Checked;
        settings.SaveHotkey = _saveHotkey.Text.Trim();
        settings.RestoreHotkey = _restoreHotkey.Text.Trim();

        settings.ShowNotifications = _showNotifications.Checked;
        settings.LoggingEnabled = _loggingEnabled.Checked;
        settings.StartWithWindows = _startWithWindows.Checked;

        // Applied here rather than by the caller, because the registry is the source of truth for
        // this one and the checkbox has to agree with it next time the dialog opens.
        StartupManager.SetEnabled(_startWithWindows.Checked);
    }

    private void UpdateEnabledState()
    {
        _autoSaveInterval.Enabled = _autoSaveEnabled.Checked;
        _autoSaveHistory.Enabled = _autoSaveEnabled.Checked;
        _autoSaveOnlyWhenChanged.Enabled = _autoSaveEnabled.Checked;

        _autoRestoreDelay.Enabled = _autoRestore.Checked;
        _requireMatchingDisplays.Enabled = _autoRestore.Checked;

        _saveHotkey.Enabled = _hotkeysEnabled.Checked;
        _restoreHotkey.Enabled = _hotkeysEnabled.Checked;
    }

    private static Label Header(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font(DefaultFont.FontFamily, DefaultFont.Size + 1f, FontStyle.Bold),
        Margin = new Padding(0, 14, 0, 6),
    };

    private static Control Row(string label, Control field, string suffix)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(20, 2, 0, 2),
            WrapContents = false,
        };

        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        panel.Controls.Add(field);

        if (!string.IsNullOrEmpty(suffix))
        {
            panel.Controls.Add(new Label { Text = suffix, AutoSize = true, Margin = new Padding(6, 6, 0, 0) });
        }

        return panel;
    }
}
