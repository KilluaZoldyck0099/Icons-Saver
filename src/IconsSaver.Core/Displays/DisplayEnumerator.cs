using IconsSaver.Core.Models;
using IconsSaver.Core.Native;

namespace IconsSaver.Core.Displays;

/// <summary>
/// Captures the current monitor arrangement.
/// </summary>
public static class DisplayEnumerator
{
    /// <summary>Ask <c>EnumDisplayDevices</c> for the device interface path rather than the adapter name.</summary>
    private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

    /// <summary>Enumerates every attached monitor, ordered so the result is reproducible.</summary>
    public static IReadOnlyList<MonitorDescriptor> EnumerateMonitors()
    {
        var monitors = new List<MonitorDescriptor>();

        // The callback is kept in a local so it stays alive for the duration of the call.
        User32.MonitorEnumProc callback = (IntPtr monitor, IntPtr hdc, ref RECT clip, IntPtr data) =>
        {
            MONITORINFOEX info = MONITORINFOEX.Create();
            if (User32.GetMonitorInfo(monitor, ref info))
            {
                monitors.Add(new MonitorDescriptor(
                    DeviceName: info.szDevice ?? string.Empty,
                    DeviceId: ReadDeviceId(info.szDevice),
                    Left: info.rcMonitor.Left,
                    Top: info.rcMonitor.Top,
                    Width: info.rcMonitor.Width,
                    Height: info.rcMonitor.Height,
                    IsPrimary: (info.dwFlags & MONITORINFOEX.MONITORINFOF_PRIMARY) != 0,
                    Dpi: Shcore.GetEffectiveDpi(monitor)));
            }

            return true;
        };

        User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

        return monitors
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.Left)
            .ThenBy(m => m.Top)
            .ToList();
    }

    /// <summary>Captures the whole arrangement, virtual screen included.</summary>
    public static DisplayFingerprint Capture() => new()
    {
        VirtualLeft = User32.GetSystemMetrics(NativeConstants.SM_XVIRTUALSCREEN),
        VirtualTop = User32.GetSystemMetrics(NativeConstants.SM_YVIRTUALSCREEN),
        VirtualWidth = User32.GetSystemMetrics(NativeConstants.SM_CXVIRTUALSCREEN),
        VirtualHeight = User32.GetSystemMetrics(NativeConstants.SM_CYVIRTUALSCREEN),
        Monitors = EnumerateMonitors(),
    };

    /// <summary>
    /// Resolves the stable device interface path behind a GDI display name.
    /// </summary>
    /// <remarks>
    /// <c>\\.\DISPLAY1</c> is an adapter output slot, not a monitor, and Windows reshuffles those
    /// numbers freely. Asking for the interface name instead yields something like
    /// <c>\\?\DISPLAY#DEL41A8#5&amp;...</c>, which survives reboots and cable swaps.
    /// </remarks>
    private static string ReadDeviceId(string? gdiDeviceName)
    {
        if (string.IsNullOrWhiteSpace(gdiDeviceName))
        {
            return string.Empty;
        }

        DISPLAY_DEVICE device = DISPLAY_DEVICE.Create();
        return User32.EnumDisplayDevices(gdiDeviceName, 0, ref device, EDD_GET_DEVICE_INTERFACE_NAME)
            ? device.DeviceID ?? string.Empty
            : string.Empty;
    }
}
