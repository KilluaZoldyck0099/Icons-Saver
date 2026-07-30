using IconsSaver.Core.Models;
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
public sealed partial class DesktopListView : IDisposable
{
    /// <summary>How long to wait for explorer to answer a single message.</summary>
    private const uint MessageTimeoutMs = 5_000;

    // Layout of the shared scratch page. Offsets are byte offsets from its base.
    private const int ItemOffset = 0;
    private const int TextOffset = 256;
    private const int TextCapacityChars = 512;
    private const int PointOffset = TextOffset + (TextCapacityChars * 2);
    private const int PointSize = 8;
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

    /// <summary>Reads every icon on the desktop, in list view order.</summary>
    public IReadOnlyList<DesktopIcon> ReadAll()
    {
        ThrowIfDisposed();

        int count = Count;
        if (count <= 0)
        {
            return Array.Empty<DesktopIcon>();
        }

        var icons = new List<DesktopIcon>(count);
        for (int index = 0; index < count; index++)
        {
            icons.Add(ReadAt(index));
        }

        return icons;
    }

    /// <summary>Reads a single icon by its current list view index.</summary>
    public DesktopIcon ReadAt(int index)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        IconPosition position = ReadPosition(index);
        string name = ReadName(index);
        return new DesktopIcon(index, name, position);
    }

    /// <summary>Reads where a single item currently sits.</summary>
    /// <remarks>
    /// <c>LVM_GETITEMPOSITION</c> writes a <c>POINT</c> through the pointer it is given, so the
    /// scratch page is cleared, handed over by remote address, and read back afterwards.
    /// </remarks>
    public IconPosition ReadPosition(int index)
    {
        ThrowIfDisposed();

        _memory.ClearLocal(PointOffset, PointSize);
        _memory.Push(PointOffset, PointSize);

        IntPtr ok = Send(
            NativeConstants.LVM_GETITEMPOSITION,
            index,
            _memory.Remote(PointOffset),
            $"a position request for item {index}");

        if (ok == IntPtr.Zero)
        {
            return IconPosition.Origin;
        }

        _memory.Pull(PointOffset, PointSize);
        return new IconPosition(_memory.ReadInt32(PointOffset), _memory.ReadInt32(PointOffset + 4));
    }

    /// <summary>Reads the display name shown under a single item.</summary>
    /// <remarks>
    /// <c>LVM_GETITEMTEXTW</c> needs an <c>LVITEMW</c> whose <c>pszText</c> points at a caller
    /// supplied buffer. Both live in the scratch page, so the structure is assembled by hand at the
    /// documented x64 offsets and its <c>pszText</c> is set to the <em>remote</em> address of the
    /// text area.
    /// </remarks>
    public string ReadName(int index)
    {
        ThrowIfDisposed();

        _memory.ClearLocal(ItemOffset, LvItemLayout.Size);
        _memory.WriteInt32(ItemOffset + LvItemLayout.Mask, NativeConstants.LVIF_TEXT);
        _memory.WriteInt32(ItemOffset + LvItemLayout.IItem, index);
        _memory.WriteInt32(ItemOffset + LvItemLayout.ISubItem, 0);
        _memory.WritePointer(ItemOffset + LvItemLayout.PszText, _memory.Remote(TextOffset));
        _memory.WriteInt32(ItemOffset + LvItemLayout.CchTextMax, TextCapacityChars);
        _memory.Push(ItemOffset, LvItemLayout.Size);

        int length = (int)Send(
            NativeConstants.LVM_GETITEMTEXTW,
            index,
            _memory.Remote(ItemOffset),
            $"a name request for item {index}");

        if (length <= 0)
        {
            return string.Empty;
        }

        length = Math.Min(length, TextCapacityChars - 1);
        _memory.Pull(TextOffset, (length + 1) * 2);
        return _memory.ReadUnicode(TextOffset, length);
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
