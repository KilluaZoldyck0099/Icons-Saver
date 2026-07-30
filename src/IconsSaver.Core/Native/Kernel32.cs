using System.Runtime.InteropServices;

namespace IconsSaver.Core.Native;

/// <summary>
/// P/Invoke surface for <c>kernel32.dll</c>, used to allocate and exchange buffers inside
/// explorer's address space.
/// </summary>
internal static class Kernel32
{
    private const string Dll = "kernel32.dll";

    [DllImport(Dll, SetLastError = true)]
    public static extern IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport(Dll, SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, IntPtr size, uint allocationType, uint protect);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool VirtualFreeEx(IntPtr process, IntPtr address, IntPtr size, uint freeType);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReadProcessMemory(IntPtr process, IntPtr baseAddress, IntPtr buffer, IntPtr size, out IntPtr bytesRead);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WriteProcessMemory(IntPtr process, IntPtr baseAddress, IntPtr buffer, IntPtr size, out IntPtr bytesWritten);

    [DllImport(Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWow64Process(IntPtr process, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

    [DllImport(Dll)]
    public static extern IntPtr GetCurrentProcess();
}
