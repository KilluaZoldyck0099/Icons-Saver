using System.Runtime.InteropServices;

namespace IconsSaver.App;

/// <summary>
/// The handful of Win32 calls the shell of the application needs.
/// </summary>
/// <remarks>
/// Everything to do with reading and writing icon positions lives in IconsSaver.Core; this is only
/// what a tray application needs for global hotkeys and for talking to an instance of itself that
/// is already running.
/// </remarks>
internal static class NativeMethods
{
    /// <summary>Pseudo-handle that delivers a message to every top level window.</summary>
    public static readonly IntPtr HWND_BROADCAST = new(0xFFFF);

    public const int WM_HOTKEY = 0x0312;
    public const int WM_DISPLAYCHANGE = 0x007E;
    public const int WM_SETTINGCHANGE = 0x001A;
    public const int WM_DPICHANGED = 0x02E0;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    /// <summary>Stops a held-down hotkey from firing repeatedly.</summary>
    public const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr window, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "RegisterWindowMessageW")]
    public static extern uint RegisterWindowMessage(string name);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    // ------------------------------------------------------------------ theming

    /// <summary>Paints the title bar and window frame dark. Windows 10 2004 and later.</summary>
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    /// <summary>The attribute number used by Windows 10 builds before 19041.</summary>
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    /// <summary>
    /// Repoints a control at a different visual style class.
    /// </summary>
    /// <remarks>
    /// Passing <c>DarkMode_Explorer</c> is what gives a list view dark scroll bars and dark hover
    /// highlights. There is no managed equivalent: those parts are drawn by the theme engine, not
    /// by the control's own colour properties.
    /// </remarks>
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    public static extern int SetWindowTheme(IntPtr window, string? subAppName, string? subIdList);
}
