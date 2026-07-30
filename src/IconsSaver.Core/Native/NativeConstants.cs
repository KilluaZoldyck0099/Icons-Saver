namespace IconsSaver.Core.Native;

/// <summary>
/// Win32 constants used by the desktop interop layer.
/// </summary>
/// <remarks>
/// The desktop icons live in a <c>SysListView32</c> owned by <c>explorer.exe</c>, so every
/// value here relates either to driving that list view by window message or to reaching
/// into explorer's address space to exchange structures with it.
/// </remarks>
internal static class NativeConstants
{
    // ---------------------------------------------------------------- messages

    public const int WM_COMMAND = 0x0111;
    public const int WM_SETTINGCHANGE = 0x001A;
    public const int WM_DISPLAYCHANGE = 0x007E;
    public const int WM_DPICHANGED = 0x02E0;

    /// <summary>Base of the list view message range; every LVM_* value is an offset from it.</summary>
    private const int LVM_FIRST = 0x1000;

    public const int LVM_GETITEMCOUNT = LVM_FIRST + 4;
    public const int LVM_SETITEMPOSITION = LVM_FIRST + 15;
    public const int LVM_GETITEMPOSITION = LVM_FIRST + 16;
    public const int LVM_REDRAWITEMS = LVM_FIRST + 21;
    public const int LVM_ARRANGE = LVM_FIRST + 22;
    public const int LVM_UPDATE = LVM_FIRST + 42;
    public const int LVM_SETITEMPOSITION32 = LVM_FIRST + 49;
    public const int LVM_GETITEMSPACING = LVM_FIRST + 51;
    public const int LVM_GETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 55;
    public const int LVM_GETITEMW = LVM_FIRST + 75;
    public const int LVM_GETITEMTEXTW = LVM_FIRST + 115;

    // ------------------------------------------------------------ item members

    public const int LVIF_TEXT = 0x0001;
    public const int LVIF_IMAGE = 0x0002;
    public const int LVIF_PARAM = 0x0004;

    // ---------------------------------------------------------------- styles

    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

    /// <summary>When set, the list view snaps items back into a packed grid and refuses free positioning.</summary>
    public const int LVS_AUTOARRANGE = 0x0100;

    public const int LVS_EX_SNAPTOGRID = 0x00080000;

    // ------------------------------------------------- SHELLDLL_DefView commands
    //
    // These identifiers are not documented by Microsoft, but they are the command ids the
    // desktop context menu itself posts, and they have been stable from Windows XP through
    // Windows 11. Each one TOGGLES, so the current state must be read before sending them.

    /// <summary>Toggles "View -> Auto arrange icons".</summary>
    public const int SFVIDM_ARRANGE_AUTO = 0x7051;

    /// <summary>Toggles "View -> Align icons to grid".</summary>
    public const int SFVIDM_ARRANGE_GRID = 0x7052;

    /// <summary>Equivalent of pressing F5 on the desktop.</summary>
    public const int SFVIDM_VIEW_REFRESH = 0x7103;

    /// <summary>Toggles "View -> Show desktop icons".</summary>
    public const int SFVIDM_MISC_SHOWDESKTOPICONS = 0x7402;

    // -------------------------------------------------------- SendMessageTimeout

    public const uint SMTO_NORMAL = 0x0000;
    public const uint SMTO_BLOCK = 0x0001;
    public const uint SMTO_ABORTIFHUNG = 0x0002;

    // ------------------------------------------------------------ process access

    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_VM_OPERATION = 0x0008;
    public const uint PROCESS_VM_READ = 0x0010;
    public const uint PROCESS_VM_WRITE = 0x0020;

    // ------------------------------------------------------------ virtual memory

    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint MEM_RELEASE = 0x8000;
    public const uint PAGE_READWRITE = 0x04;

    // ----------------------------------------------------------- system metrics

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;
    public const int SM_CMONITORS = 80;

    // ------------------------------------------------------------- window classes

    public const string ClassProgman = "Progman";
    public const string ClassWorkerW = "WorkerW";
    public const string ClassDefView = "SHELLDLL_DefView";
    public const string ClassListView = "SysListView32";
}
