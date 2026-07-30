using System.Runtime.InteropServices;
using System.Text;

namespace IconsSaver.Core.Native;

/// <summary>
/// P/Invoke surface for <c>user32.dll</c>.
/// </summary>
internal static class User32
{
    private const string Dll = "user32.dll";

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "FindWindowW")]
    public static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "FindWindowExW")]
    public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetClassNameW")]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder buffer, int maxCount);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport(Dll, SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetWindowLongW")]
    public static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport(Dll, CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Preferred way to talk to explorer: a wedged shell can otherwise block the caller forever.
    /// </summary>
    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SendMessageTimeoutW")]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        uint flags,
        uint timeoutMilliseconds,
        out IntPtr result);

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport(Dll)]
    public static extern int GetSystemMetrics(int index);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport(Dll)]
    public static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport(Dll)]
    public static extern uint GetDpiForSystem();

    // ------------------------------------------------------------------ monitors

    public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref RECT clip, IntPtr data);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFOEX info);

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "EnumDisplayDevicesW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayDevices(string? device, uint deviceIndex, ref DISPLAY_DEVICE displayDevice, uint flags);
}
