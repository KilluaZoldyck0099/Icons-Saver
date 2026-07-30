using IconsSaver.Core.Native;

namespace IconsSaver.Core.Desktop;

/// <summary>
/// A live connection to the desktop icon list view.
/// </summary>
/// <remarks>
/// <para>
/// The instance owns a scratch page inside explorer for the duration of its lifetime, so it should
/// be opened, used and disposed around a single operation rather than held indefinitely: explorer
/// may restart at any time, which invalidates the window handles.
/// </para>
/// <para>
/// Every message goes out through <c>SendMessageTimeout</c>. A plain <c>SendMessage</c> to a wedged
/// shell would block this process forever, and a tray utility that freezes with the shell is worse
/// than one that reports a failure.
/// </para>
/// </remarks>
public sealed class DesktopListView : IDisposable
{
    /// <summary>How long to wait for explorer to answer a single message.</summary>
    private const uint MessageTimeoutMs = 5_000;

    // Layout of the shared scratch page. Offsets are byte offsets from its base.
    private const int ItemOffset = 0;
    private const int TextOffset = 256;
    private const int TextCapacityChars = 512;
    private const int PointOffset = TextOffset + (TextCapacityChars * 2);
    private const int ScratchSize = 8 * 1024;

    private readonly DesktopWindows _windows;
    private readonly RemoteProcessMemory _memory;
    private bool _disposed;

    private DesktopListView(DesktopWindows windows, RemoteProcessMemory memory)
    {
        _windows = windows;
        _memory = memory;
    }

    /// <summary>The shell windows this instance is bound to.</summary>
    public DesktopWindows Windows => _windows;

    /// <summary>Identifier of the explorer process that owns the desktop.</summary>
    public uint ExplorerProcessId => _memory.ProcessId;

    /// <summary>
    /// Opens a connection to the desktop icon view.
    /// </summary>
    /// <exception cref="DesktopInteropException">The desktop could not be found or opened.</exception>
    public static DesktopListView Open()
    {
        DesktopWindows windows = DesktopWindowLocator.Locate();
        RemoteProcessMemory memory = RemoteProcessMemory.OpenForWindow(windows.ListView, ScratchSize);
        return new DesktopListView(windows, memory);
    }

    /// <summary>Opens a connection, returning <see langword="false"/> instead of throwing.</summary>
    public static bool TryOpen(out DesktopListView? view)
    {
        try
        {
            view = Open();
            return true;
        }
        catch (DesktopInteropException)
        {
            view = null;
            return false;
        }
    }

    /// <summary>Number of icons currently on the desktop.</summary>
    /// <remarks>Zero when "Show desktop icons" is turned off.</remarks>
    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return (int)Send(NativeConstants.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero, "a request for the icon count");
        }
    }

    /// <summary>Sends a message to the list view and returns its result.</summary>
    private IntPtr Send(int message, IntPtr wParam, IntPtr lParam, string description) =>
        SendTo(_windows.ListView, message, wParam, lParam, description);

    private static IntPtr SendTo(IntPtr window, int message, IntPtr wParam, IntPtr lParam, string description)
    {
        IntPtr delivered = User32.SendMessageTimeout(
            window,
            message,
            wParam,
            lParam,
            NativeConstants.SMTO_ABORTIFHUNG,
            MessageTimeoutMs,
            out IntPtr result);

        if (delivered == IntPtr.Zero)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw DesktopInteropException.FromWin32($"The desktop did not respond to {description}.", error);
        }

        return result;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _memory.Dispose();
    }
}
