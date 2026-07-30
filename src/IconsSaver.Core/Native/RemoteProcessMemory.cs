using System.Runtime.InteropServices;

namespace IconsSaver.Core.Native;

/// <summary>
/// A scratch buffer that exists simultaneously in this process and in the process that owns a
/// given window, so structures can be handed to that process by pointer.
/// </summary>
/// <remarks>
/// <para>
/// List view messages such as <c>LVM_GETITEMPOSITION</c> take a pointer and the receiving window
/// dereferences it in <em>its own</em> address space. Because the desktop list view belongs to
/// <c>explorer.exe</c>, a pointer into our heap would be meaningless there. The fix is the classic
/// one: allocate a page inside explorer with <c>VirtualAllocEx</c>, mirror it with a local buffer,
/// and copy back and forth with <c>ReadProcessMemory</c> / <c>WriteProcessMemory</c>.
/// </para>
/// <para>
/// The local mirror is a fixed unmanaged allocation rather than a pinned managed array so that its
/// address is stable for the lifetime of the instance.
/// </para>
/// </remarks>
internal sealed class RemoteProcessMemory : IDisposable
{
    private const uint ProcessAccess =
        NativeConstants.PROCESS_QUERY_INFORMATION |
        NativeConstants.PROCESS_VM_OPERATION |
        NativeConstants.PROCESS_VM_READ |
        NativeConstants.PROCESS_VM_WRITE;

    private IntPtr _process;
    private IntPtr _remoteBase;
    private IntPtr _localBase;
    private bool _disposed;

    private RemoteProcessMemory(IntPtr process, IntPtr remoteBase, IntPtr localBase, int capacity, uint processId)
    {
        _process = process;
        _remoteBase = remoteBase;
        _localBase = localBase;
        Capacity = capacity;
        ProcessId = processId;
    }

    /// <summary>Size in bytes of both the local mirror and the remote allocation.</summary>
    public int Capacity { get; }

    /// <summary>Identifier of the process the remote page lives in.</summary>
    public uint ProcessId { get; }

    /// <summary>
    /// Opens the process that owns <paramref name="window"/> and reserves a shared scratch page in it.
    /// </summary>
    public static RemoteProcessMemory OpenForWindow(IntPtr window, int capacity = 16 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1024);

        if (User32.GetWindowThreadProcessId(window, out uint processId) == 0 || processId == 0)
        {
            throw DesktopInteropException.FromLastWin32Error("Could not determine which process owns the desktop list view.");
        }

        IntPtr process = Kernel32.OpenProcess(ProcessAccess, false, processId);
        if (process == IntPtr.Zero)
        {
            throw DesktopInteropException.FromLastWin32Error(
                $"Could not open explorer (process {processId}) for memory access.");
        }

        IntPtr remoteBase = Kernel32.VirtualAllocEx(
            process,
            IntPtr.Zero,
            capacity,
            NativeConstants.MEM_COMMIT | NativeConstants.MEM_RESERVE,
            NativeConstants.PAGE_READWRITE);

        if (remoteBase == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            Kernel32.CloseHandle(process);
            throw DesktopInteropException.FromWin32("Could not reserve a scratch buffer inside explorer.", error);
        }

        IntPtr localBase = Marshal.AllocHGlobal(capacity);
        NativeMemoryHelpers.Clear(localBase, capacity);

        return new RemoteProcessMemory(process, remoteBase, localBase, capacity, processId);
    }

    /// <summary>Address, in the remote process, of the byte at <paramref name="offset"/>.</summary>
    public IntPtr Remote(int offset)
    {
        ThrowIfDisposed();
        EnsureRange(offset, 1);
        return _remoteBase + offset;
    }

    /// <summary>Address, in this process, of the byte at <paramref name="offset"/> of the mirror.</summary>
    public IntPtr Local(int offset)
    {
        ThrowIfDisposed();
        EnsureRange(offset, 1);
        return _localBase + offset;
    }

    /// <summary>Copies <paramref name="count"/> bytes of the local mirror into the remote page.</summary>
    public void Push(int offset, int count)
    {
        ThrowIfDisposed();
        EnsureRange(offset, count);

        if (!Kernel32.WriteProcessMemory(_process, _remoteBase + offset, _localBase + offset, count, out IntPtr written)
            || (int)written != count)
        {
            throw DesktopInteropException.FromLastWin32Error(
                $"Could not write {count} bytes into explorer's scratch buffer.");
        }
    }

    /// <summary>Copies <paramref name="count"/> bytes of the remote page into the local mirror.</summary>
    public void Pull(int offset, int count)
    {
        ThrowIfDisposed();
        EnsureRange(offset, count);

        if (!Kernel32.ReadProcessMemory(_process, _remoteBase + offset, _localBase + offset, count, out IntPtr read)
            || (int)read != count)
        {
            throw DesktopInteropException.FromLastWin32Error(
                $"Could not read {count} bytes back from explorer's scratch buffer.");
        }
    }

    public void WriteInt32(int offset, int value)
    {
        EnsureRange(offset, sizeof(int));
        Marshal.WriteInt32(_localBase, offset, value);
    }

    public int ReadInt32(int offset)
    {
        EnsureRange(offset, sizeof(int));
        return Marshal.ReadInt32(_localBase, offset);
    }

    public void WritePointer(int offset, IntPtr value)
    {
        EnsureRange(offset, IntPtr.Size);
        Marshal.WriteIntPtr(_localBase, offset, value);
    }

    /// <summary>Reads a UTF-16 string of <paramref name="length"/> characters from the local mirror.</summary>
    public string ReadUnicode(int offset, int length)
    {
        if (length <= 0)
        {
            return string.Empty;
        }

        EnsureRange(offset, checked((length + 1) * 2));
        return Marshal.PtrToStringUni(_localBase + offset, length) ?? string.Empty;
    }

    /// <summary>Zero-fills a span of the local mirror.</summary>
    public void ClearLocal(int offset, int count)
    {
        ThrowIfDisposed();
        EnsureRange(offset, count);
        NativeMemoryHelpers.Clear(_localBase + offset, count);
    }

    private void EnsureRange(int offset, int count)
    {
        if (offset < 0 || count < 0 || offset + count > Capacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Range [{offset}, {offset + count}) does not fit in the {Capacity} byte scratch buffer.");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_localBase != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_localBase);
            _localBase = IntPtr.Zero;
        }

        if (_process != IntPtr.Zero)
        {
            if (_remoteBase != IntPtr.Zero)
            {
                Kernel32.VirtualFreeEx(_process, _remoteBase, IntPtr.Zero, NativeConstants.MEM_RELEASE);
                _remoteBase = IntPtr.Zero;
            }

            Kernel32.CloseHandle(_process);
            _process = IntPtr.Zero;
        }
    }
}

internal static class NativeMemoryHelpers
{
    /// <summary>A reusable block of zeroes; avoids an allocation on every clear.</summary>
    private static readonly byte[] Zeroes = new byte[4096];

    /// <summary>Zero-fills unmanaged memory without requiring an unsafe context.</summary>
    public static void Clear(IntPtr address, int count)
    {
        int written = 0;
        while (written < count)
        {
            int chunk = Math.Min(Zeroes.Length, count - written);
            Marshal.Copy(Zeroes, 0, address + written, chunk);
            written += chunk;
        }
    }
}
