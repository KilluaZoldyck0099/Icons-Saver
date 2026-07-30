using System.Runtime.InteropServices;

namespace IconsSaver.Core.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;

    public POINT(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override readonly string ToString() => $"({X}, {Y})";
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;

    public override readonly string ToString() => $"[{Left},{Top} {Width}x{Height}]";
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MONITORINFOEX
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szDevice;

    public const uint MONITORINFOF_PRIMARY = 0x00000001;

    public static MONITORINFOEX Create() => new()
    {
        cbSize = Marshal.SizeOf<MONITORINFOEX>(),
        szDevice = string.Empty,
    };
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DISPLAY_DEVICE
{
    public int cb;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceString;

    public uint StateFlags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceKey;

    public const uint DISPLAY_DEVICE_ACTIVE = 0x00000001;
    public const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;

    public static DISPLAY_DEVICE Create() => new()
    {
        cb = Marshal.SizeOf<DISPLAY_DEVICE>(),
        DeviceName = string.Empty,
        DeviceString = string.Empty,
        DeviceID = string.Empty,
        DeviceKey = string.Empty,
    };
}

/// <summary>
/// Byte offsets of the fields of <c>LVITEMW</c> in a 64-bit process.
/// </summary>
/// <remarks>
/// <c>LVITEMW</c> cannot be marshalled with <see cref="StructLayoutAttribute"/> here because the
/// structure has to be written into <em>explorer's</em> address space by hand. Only the members
/// the desktop reader actually uses are listed; the rest are zero-filled.
/// <code>
/// offset  field
///      0  UINT   mask
///      4  int    iItem
///      8  int    iSubItem
///     12  UINT   state
///     16  UINT   stateMask
///     24  LPWSTR pszText        (8-byte aligned pointer)
///     32  int    cchTextMax
///     36  int    iImage
///     40  LPARAM lParam
/// </code>
/// </remarks>
internal static class LvItemLayout
{
    public const int Mask = 0;
    public const int IItem = 4;
    public const int ISubItem = 8;
    public const int State = 12;
    public const int StateMask = 16;
    public const int PszText = 24;
    public const int CchTextMax = 32;
    public const int IImage = 36;
    public const int LParam = 40;

    /// <summary>Total size of <c>LVITEMW</c> on x64, rounded up for safety.</summary>
    public const int Size = 96;
}
