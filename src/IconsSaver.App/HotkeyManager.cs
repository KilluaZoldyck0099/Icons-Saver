using IconsSaver.Core.Diagnostics;

namespace IconsSaver.App;

/// <summary>
/// Registers the system-wide hotkeys.
/// </summary>
/// <remarks>
/// The whole point of "restore my icons" is that it works when something has just gone wrong,
/// which is exactly when hunting for a tray icon is most annoying. Registration is best-effort:
/// another application may already own a combination, and losing a hotkey is not worth failing
/// startup over.
/// </remarks>
internal sealed class HotkeyManager : IDisposable
{
    /// <summary>Hotkey id for "save the current layout".</summary>
    public const int SaveHotkeyId = 1;

    /// <summary>Hotkey id for "restore the best matching layout".</summary>
    public const int RestoreHotkeyId = 2;

    private readonly IntPtr _window;
    private readonly List<int> _registered = new();
    private bool _disposed;

    public HotkeyManager(IntPtr window)
    {
        _window = window;
    }

    /// <summary>Registers one hotkey from a gesture such as <c>Ctrl+Alt+S</c>.</summary>
    /// <returns><see langword="true"/> when Windows accepted the registration.</returns>
    public bool Register(int id, string? gesture)
    {
        if (!TryParse(gesture, out uint modifiers, out uint virtualKey))
        {
            return false;
        }

        if (!NativeMethods.RegisterHotKey(_window, id, modifiers | NativeMethods.MOD_NOREPEAT, virtualKey))
        {
            AppLog.Warn($"Hotkey '{gesture}' is already taken by another application.");
            return false;
        }

        _registered.Add(id);
        return true;
    }

    /// <summary>Releases every hotkey this manager holds.</summary>
    public void UnregisterAll()
    {
        foreach (int id in _registered)
        {
            NativeMethods.UnregisterHotKey(_window, id);
        }

        _registered.Clear();
    }

    /// <summary>
    /// Parses a gesture such as <c>Ctrl+Shift+F5</c> into modifier flags and a virtual key code.
    /// </summary>
    public static bool TryParse(string? gesture, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(gesture))
        {
            return false;
        }

        foreach (string token in gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (token.ToLowerInvariant())
            {
                case "ctrl" or "control":
                    modifiers |= NativeMethods.MOD_CONTROL;
                    continue;
                case "alt":
                    modifiers |= NativeMethods.MOD_ALT;
                    continue;
                case "shift":
                    modifiers |= NativeMethods.MOD_SHIFT;
                    continue;
                case "win" or "windows":
                    modifiers |= NativeMethods.MOD_WIN;
                    continue;
            }

            if (!Enum.TryParse(token, ignoreCase: true, out Keys key) || key == Keys.None)
            {
                return false;
            }

            virtualKey = (uint)key;
        }

        // A bare key with no modifier would swallow that key for the whole system.
        return virtualKey != 0 && modifiers != 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        UnregisterAll();
    }
}
