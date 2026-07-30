using System.Runtime.InteropServices;

namespace IconsSaver.Core.Native;

/// <summary>
/// P/Invoke surface for <c>shcore.dll</c>, used for per-monitor DPI queries.
/// </summary>
internal static class Shcore
{
    private const string Dll = "shcore.dll";

    /// <summary>The DPI a per-monitor aware application should render at for this monitor.</summary>
    public const int MDT_EFFECTIVE_DPI = 0;

    [DllImport(Dll)]
    public static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// Reads the effective DPI of a monitor, falling back to the classic 96 when the call fails.
    /// </summary>
    public static uint GetEffectiveDpi(IntPtr monitor)
    {
        try
        {
            if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 && dpiX > 0)
            {
                return dpiX;
            }
        }
        catch (DllNotFoundException)
        {
            // Pre-8.1 systems have no shcore.dll; nothing to do but assume the default.
        }
        catch (EntryPointNotFoundException)
        {
            // Same story for an unexpectedly old shcore.dll.
        }

        return 96;
    }
}
