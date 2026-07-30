using System.Runtime.InteropServices;

namespace IconsSaver.Core.Native;

/// <summary>
/// P/Invoke surface for <c>shell32.dll</c>.
/// </summary>
internal static class Shell32
{
    private const string Dll = "shell32.dll";

    /// <summary>The shell should re-read whatever it caches about the affected items.</summary>
    public const int SHCNE_ASSOCCHANGED = 0x08000000;

    public const int SHCNE_UPDATEDIR = 0x00001000;

    public const uint SHCNF_IDLIST = 0x0000;
    public const uint SHCNF_PATHW = 0x0005;
    public const uint SHCNF_FLUSH = 0x1000;

    [DllImport(Dll, CharSet = CharSet.Unicode)]
    public static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
