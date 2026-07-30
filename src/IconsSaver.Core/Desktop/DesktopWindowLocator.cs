using IconsSaver.Core.Native;

namespace IconsSaver.Core.Desktop;

/// <summary>Which shell window is currently hosting the desktop icon view.</summary>
public enum DesktopHostKind
{
    None = 0,

    /// <summary>The usual arrangement: <c>Progman</c> owns the icon view directly.</summary>
    Progman,

    /// <summary>
    /// The icon view has been reparented to a <c>WorkerW</c> window, which happens once the
    /// wallpaper slideshow or a desktop compositing effect kicks in.
    /// </summary>
    WorkerW,
}

/// <summary>The pair of shell windows needed to inspect and drive the desktop icons.</summary>
/// <param name="Host">The top level window hosting the view (<c>Progman</c> or a <c>WorkerW</c>).</param>
/// <param name="DefView">The <c>SHELLDLL_DefView</c> that owns the view commands.</param>
/// <param name="ListView">The <c>SysListView32</c> holding the icons themselves.</param>
/// <param name="Kind">Which host window the view was found under.</param>
public readonly record struct DesktopWindows(IntPtr Host, IntPtr DefView, IntPtr ListView, DesktopHostKind Kind)
{
    /// <summary>True when all three handles are live windows.</summary>
    public bool IsValid =>
        ListView != IntPtr.Zero &&
        DefView != IntPtr.Zero &&
        User32.IsWindow(ListView) &&
        User32.IsWindow(DefView);
}

/// <summary>
/// Finds the window that actually holds the desktop icons.
/// </summary>
/// <remarks>
/// The normal hierarchy is <c>Progman -&gt; SHELLDLL_DefView -&gt; SysListView32</c>. When a
/// wallpaper slideshow is active, explorer spawns <c>WorkerW</c> windows and moves the
/// <c>SHELLDLL_DefView</c> under one of them, so both layouts have to be probed. Handles are
/// never cached across calls because explorer recreates these windows when it restarts.
/// </remarks>
public static class DesktopWindowLocator
{
    /// <summary>Attempts to locate the desktop icon windows.</summary>
    public static bool TryLocate(out DesktopWindows windows)
    {
        IntPtr progman = User32.FindWindow(NativeConstants.ClassProgman, null);
        if (progman != IntPtr.Zero && TryDescend(progman, DesktopHostKind.Progman, out windows))
        {
            return true;
        }

        IntPtr worker = IntPtr.Zero;
        while (true)
        {
            worker = User32.FindWindowEx(IntPtr.Zero, worker, NativeConstants.ClassWorkerW, null);
            if (worker == IntPtr.Zero)
            {
                break;
            }

            if (TryDescend(worker, DesktopHostKind.WorkerW, out windows))
            {
                return true;
            }
        }

        windows = default;
        return false;
    }

    /// <summary>Locates the desktop icon windows or throws when the shell is not reachable.</summary>
    /// <exception cref="DesktopInteropException">The desktop icon view could not be found.</exception>
    public static DesktopWindows Locate()
    {
        if (TryLocate(out DesktopWindows windows))
        {
            return windows;
        }

        throw new DesktopInteropException(
            "Could not find the desktop icon view. Explorer may be restarting, or the desktop may be " +
            "hosted by a replacement shell.");
    }

    private static bool TryDescend(IntPtr host, DesktopHostKind kind, out DesktopWindows windows)
    {
        IntPtr defView = User32.FindWindowEx(host, IntPtr.Zero, NativeConstants.ClassDefView, null);
        if (defView != IntPtr.Zero)
        {
            IntPtr listView = User32.FindWindowEx(defView, IntPtr.Zero, NativeConstants.ClassListView, null);
            if (listView != IntPtr.Zero)
            {
                windows = new DesktopWindows(host, defView, listView, kind);
                return true;
            }
        }

        windows = default;
        return false;
    }
}
