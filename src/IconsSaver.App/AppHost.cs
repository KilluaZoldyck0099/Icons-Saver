using System.Globalization;
using IconsSaver.Core;
using IconsSaver.Core.Diagnostics;
using IconsSaver.Core.Displays;
using IconsSaver.Core.Models;
using IconsSaver.Core.Services;
using IconsSaver.Core.Storage;

namespace IconsSaver.App;

/// <summary>
/// Owns the application's lifetime and wires the pieces together.
/// </summary>
/// <remarks>
/// The application outlives its window: closing the window leaves the tray icon, the hotkeys and
/// the display watcher running. That is why the lifetime hangs off an
/// <see cref="ApplicationContext"/> and a hidden message window rather than off a form.
/// </remarks>
internal sealed class AppHost : ApplicationContext
{
    private readonly Workspace _workspace = new();
    private readonly MessageWindow _messages = new();
    private readonly DisplayChangeMonitor _displays = new();
    private readonly HotkeyManager _hotkeys;
    private readonly AutoSaveService _autoSave;
    private readonly TrayController _tray;

    private MainForm? _mainForm;
    private bool _shuttingDown;

    public AppHost(bool startHidden)
    {
        _hotkeys = new HotkeyManager(_messages.Handle);
        _autoSave = new AutoSaveService(_workspace.Layouts, _workspace.AutoSnapshots);
        _tray = new TrayController(_workspace);

        WireMessages();
        WireTray();
        WireAutoSave();
        ApplySettings(_workspace.Settings);

        // A 15 second poll is a cheap safety net for display changes that never broadcast, such as
        // a scaling change applied from Settings. The debounce means it costs one monitor
        // enumeration and nothing else when nothing has happened.
        _displays.StartPolling(TimeSpan.FromSeconds(15));

        AppLog.Info($"{AppInfo.Name} {AppInfo.Version} started.");
        UpdateTooltip();

        if (!startHidden)
        {
            ShowMainWindow();
        }
    }

    private void WireMessages()
    {
        _messages.DisplayEvent += (_, _) => _displays.Notify();
        _messages.HotkeyPressed += (_, id) => OnHotkey(id);
        _messages.ShowRequested += (_, _) => ShowMainWindow();
        _displays.Changed += OnDisplayChanged;
    }

    private void WireTray()
    {
        _tray.OpenRequested += (_, _) => ShowMainWindow();
        _tray.SaveRequested += (_, _) => SaveWithGeneratedName();
        _tray.RestoreBestRequested += (_, _) => RestoreBest(automatic: false);
        _tray.RestoreRequested += (_, snapshot) => _workspace.Restore(snapshot, RestoreOptions.Manual);
        _tray.SettingsRequested += (_, _) => ShowSettings();
        _tray.ExitRequested += (_, _) => Shutdown();
    }

    private void WireAutoSave()
    {
        _autoSave.Saved += (_, snapshot) =>
        {
            _workspace.NotifySnapshotsChanged();
            AppLog.Info($"Automatic snapshot taken: {snapshot.IconCount} icons.");
        };

        _autoSave.Failed += (_, exception) =>
            AppLog.Error("Automatic snapshot failed.", exception);
    }

    /// <summary>Pushes the current settings into every component that depends on them.</summary>
    private void ApplySettings(AppSettings settings)
    {
        _displays.SettleDelay = TimeSpan.FromSeconds(settings.AutoRestoreDelaySeconds);
        _autoSave.Reconfigure(settings);

        _hotkeys.UnregisterAll();
        if (settings.HotkeysEnabled)
        {
            _hotkeys.Register(HotkeyManager.SaveHotkeyId, settings.SaveHotkey);
            _hotkeys.Register(HotkeyManager.RestoreHotkeyId, settings.RestoreHotkey);
        }
    }

    private void OnHotkey(int id)
    {
        switch (id)
        {
            case HotkeyManager.SaveHotkeyId:
                LayoutSnapshot? snapshot = SaveWithGeneratedName();
                if (snapshot is not null)
                {
                    _tray.Notify(AppInfo.Name, $"Saved {snapshot.IconCount} icon positions.");
                }

                break;

            case HotkeyManager.RestoreHotkeyId:
                RestoreBest(automatic: false);
                break;
        }
    }

    /// <summary>
    /// Reacts to a settled display change by putting the matching layout back.
    /// </summary>
    /// <remarks>
    /// Raised on a thread pool thread, so everything is marshalled onto the UI thread first: the
    /// tray icon, the balloon and the main window all belong to it.
    /// </remarks>
    private void OnDisplayChanged(object? sender, DisplayChangedEventArgs e)
    {
        if (_shuttingDown)
        {
            return;
        }

        _messages.BeginInvoke(() =>
        {
            UpdateTooltip();

            if (!_workspace.Settings.AutoRestoreOnDisplayChange)
            {
                return;
            }

            RestoreReport? report = RestoreBest(automatic: _workspace.Settings.RestoreOnlyOnMatchingDisplays);

            if (report is { Moved: > 0 })
            {
                _tray.Notify(AppInfo.Name, $"Displays changed. {report.Summarize()}");
            }

            // Whatever the outcome, the new arrangement is now the baseline; otherwise the poll
            // would keep re-reporting the same change.
            _displays.AcceptCurrent();
        });
    }

    private LayoutSnapshot? SaveWithGeneratedName()
    {
        string name = DateTimeOffset.Now.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
        LayoutSnapshot? snapshot = _workspace.SaveNow(name);
        UpdateTooltip();
        return snapshot;
    }

    private RestoreReport? RestoreBest(bool automatic) => _workspace.RestoreBestMatch(automatic);

    private void ShowMainWindow()
    {
        if (_mainForm is null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_workspace);
            _mainForm.SettingsRequested += (_, _) => ShowSettings();
            _mainForm.ExitRequested += (_, _) => Shutdown();
        }

        _mainForm.Show();

        if (_mainForm.WindowState == FormWindowState.Minimized)
        {
            _mainForm.WindowState = FormWindowState.Normal;
        }

        _mainForm.Activate();
        _mainForm.BringToFront();
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsForm(_workspace.Settings);

        if (dialog.ShowDialog(_mainForm) != DialogResult.OK)
        {
            return;
        }

        _workspace.ApplySettings(dialog.Result);
        ApplySettings(dialog.Result);
        _workspace.Report("Settings saved.");
    }

    private void UpdateTooltip()
    {
        DisplayFingerprint display = DisplayEnumerator.Capture();
        _tray.SetTooltip($"{AppInfo.Name} - {display.Describe()}");
    }

    private void Shutdown()
    {
        if (_shuttingDown)
        {
            return;
        }

        _shuttingDown = true;
        AppLog.Info("Shutting down.");
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeys.Dispose();
            _autoSave.Dispose();
            _displays.Dispose();
            _tray.Dispose();
            _mainForm?.Dispose();
            _messages.Dispose();
        }

        base.Dispose(disposing);
    }
}
