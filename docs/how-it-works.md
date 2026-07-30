# How Icons Saver works

The desktop is not special. It is a folder view like any other, drawn by a
`SysListView32` control that `explorer.exe` owns. Everything Icons Saver does comes down
to reading item positions out of that control and writing them back.

Everything below was verified on Windows 11 Pro (build 26200) against a three-monitor
setup with 218 desktop icons.

---

## Finding the list view

The usual hierarchy is:

```
Progman
└── SHELLDLL_DefView
    └── SysListView32      <- the icons live here
```

That is not the only arrangement. Once a wallpaper slideshow or a desktop compositing
effect is active, explorer creates `WorkerW` windows and moves the `SHELLDLL_DefView`
under one of them. Both layouts have to be probed, so the search walks `Progman` first
and then every top-level `WorkerW` looking for a `SHELLDLL_DefView` with a
`SysListView32` inside it.

Handles are never cached between operations. Explorer restarts more often than people
expect — a crash, a theme change, "Restart Windows Explorer" in Task Manager — and every
one of those recreates these windows.

## Reading positions across a process boundary

`LVM_GETITEMPOSITION` takes a pointer and writes a `POINT` through it. `LVM_GETITEMTEXTW`
takes a pointer to an `LVITEMW` whose `pszText` field is itself a pointer to a caller
supplied buffer. Both pointers are dereferenced by the *receiving* window — which lives
in explorer, not here. A pointer into this process's heap would be meaningless there.

So the classic approach applies:

1. `OpenProcess` on explorer with `PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE`.
   No elevation is needed: both processes run as the same user at the same integrity level.
2. `VirtualAllocEx` a scratch page *inside* explorer.
3. Keep a local mirror of that page, assemble structures in it, `WriteProcessMemory` them
   across, send the message, and `ReadProcessMemory` the answer back.

`LVITEMW` is assembled by hand rather than marshalled, because its field offsets have to
match the 64-bit layout explorer's comctl32 expects:

| Offset | Field |
| --- | --- |
| 0 | `mask` |
| 4 | `iItem` |
| 8 | `iSubItem` |
| 12 | `state` |
| 16 | `stateMask` |
| 24 | `pszText` (8-byte aligned pointer) |
| 32 | `cchTextMax` |

### Why the app refuses to run 32-bit

Under WOW64 the same structure is 60 bytes with 32-bit pointers, and comctl32 reads it as
88 bytes. The failure is nastier than a crash: `LVM_GETITEMPOSITION` keeps working,
because a `POINT` is 8 bytes either way, while `LVM_GETITEMTEXTW` returns **zero
characters for every item** with no error and no last-error value. The result is a layout
file full of correct coordinates attached to blank names — one that saves without
complaint and restores nothing. `EnvironmentGuard` makes that a hard startup failure.

## Writing positions

There are two messages that move an item, and only one of them is safe.

`LVM_SETITEMPOSITION` packs both coordinates into a single 32-bit `lParam`, 16 bits each.
That silently truncates anything past 32767 and mangles negative values. The virtual
screen on the machine this was developed against is 5280 × 3253 pixels starting at
(-1440, -1080) — precisely the shape of desktop this tool exists for.

`LVM_SETITEMPOSITION32` instead takes a pointer to a `POINT`, which is exact. That is the
one Icons Saver uses.

## Auto arrange

Writing a position while "Auto arrange icons" is on does nothing at all — the shell
immediately repacks the item into its grid slot. So a restore reads the current state,
turns auto arrange off, applies every position, and then **leaves it off**.

Leaving it off is deliberate. Switching auto arrange back on repacks every icon the
instant it happens, which would undo the restore that just finished. The restore report
carries `AutoArrangeSuspended` so the user can be told the setting was changed rather
than discovering it later.

There is no documented API to set these flags on the desktop, so the app sends the same
`WM_COMMAND` ids the desktop's own View menu sends to `SHELLDLL_DefView`:

| Id | Command |
| --- | --- |
| `0x7051` | Auto arrange icons |
| `0x7052` | Align icons to grid |
| `0x7103` | Refresh |
| `0x7402` | Show desktop icons |

These **toggle** rather than set, so the current state is always read first.

## Coordinates, monitors and DPI

Positions come back in list view client coordinates, whose origin is the top-left of the
**virtual screen** — not of the primary monitor. On a setup with a monitor to the left of
the primary one, the virtual screen starts at a negative screen coordinate, yet list view
coordinates stay non-negative: an icon on the far-left monitor is simply near `X = 0`.

That makes a position meaningful only relative to the display arrangement it was captured
on, which is why every layout stores a **display fingerprint**: the virtual screen bounds
plus, for each monitor, its stable device interface path, position, size, primary flag and
effective DPI. The fingerprint is hashed into a short signature, and automatic restores
only fire when the signatures match. A weaker geometry-only comparison is available as a
fallback, which covers swapping a monitor for an identical model.

Monitors are identified by device interface path rather than by `\\.\DISPLAY1`, because
Windows hands those numbers out in adapter enumeration order and reshuffles them across
reboots even when nothing about the hardware has changed.

Both executables declare per-monitor v2 DPI awareness in their manifest. The icon
coordinates themselves are raw values read out of explorer's memory and are not affected
by DPI virtualisation, but `GetSystemMetrics` is: a DPI-unaware process is told about a
rescaled virtual screen, and the fingerprint it computed would not match the one the tray
app computed for the same physical setup.

## Reacting to display changes

Plugging in a monitor produces a burst of notifications, not one, and the shell repacks
the desktop icons *after* the notification anything can observe. Restoring on the first
event would be overwritten by explorer's own tidy-up seconds later.

So `DisplayChangeMonitor` debounces: every `WM_DISPLAYCHANGE`, `WM_DPICHANGED` or
`WM_SETTINGCHANGE` restarts a settle timer, and only when that timer expires is the
fingerprint recaptured and compared. A low-frequency background poll catches changes that
broadcast nothing, such as a scaling change applied from Settings.

The window receiving those broadcasts is a real top-level window that is simply never
shown. A message-only window would be the obvious choice, but Windows does not deliver
broadcast messages to those.

## Matching icons on restore

Item indices are useless as identifiers: creating one file renumbers everything after it.
Display name is the practical key, but it is not unique either — the desktop is a merged
view of `%USERPROFILE%\Desktop` and `%PUBLIC%\Desktop`, so the same name can genuinely
appear twice.

Matching therefore runs in two passes. The first pairs names that agree exactly; the
second gives whatever is left a case-insensitive chance. Within a pass, ties are broken by
whichever candidate sat closest to the recorded index. Every live icon can be claimed only
once, so two saved entries never fight over one icon.

## A known limitation

Since Windows 10 1809 the shell keeps its own record of desktop icon placement, and it
does not learn about positions written through the list view. A restored layout is correct
immediately, but the shell may reassert its own idea of the arrangement after an explorer
restart. The supported alternative is `IFolderView::SelectAndPositionItems` reached
through `IShellWindows` / `IFolderView2`, which is a considerably larger COM surface;
moving to it is tracked as future work.
