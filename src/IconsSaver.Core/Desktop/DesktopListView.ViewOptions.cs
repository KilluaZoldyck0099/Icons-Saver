using IconsSaver.Core.Models;
using IconsSaver.Core.Native;

namespace IconsSaver.Core.Desktop;

/// <summary>
/// The half of <see cref="DesktopListView"/> that reads and flips the desktop view settings.
/// </summary>
public sealed partial class DesktopListView
{
    /// <summary>Reads the current auto arrange and align-to-grid settings.</summary>
    public DesktopViewOptions ReadViewOptions()
    {
        ThrowIfDisposed();

        int style = User32.GetWindowLong(_windows.ListView, NativeConstants.GWL_STYLE);
        bool autoArrange = (style & NativeConstants.LVS_AUTOARRANGE) != 0;

        int extendedStyle = (int)Send(
            NativeConstants.LVM_GETEXTENDEDLISTVIEWSTYLE,
            IntPtr.Zero,
            IntPtr.Zero,
            "an extended style query");

        bool snapToGrid = (extendedStyle & NativeConstants.LVS_EX_SNAPTOGRID) != 0;

        return new DesktopViewOptions(autoArrange, snapToGrid);
    }

    /// <summary>Turns "Auto arrange icons" on or off.</summary>
    /// <returns><see langword="true"/> when the setting actually changed.</returns>
    public bool SetAutoArrange(bool enabled) =>
        ApplyToggle(NativeConstants.SFVIDM_ARRANGE_AUTO, ReadViewOptions().AutoArrange, enabled, "the auto arrange toggle");

    /// <summary>Turns "Align icons to grid" on or off.</summary>
    /// <returns><see langword="true"/> when the setting actually changed.</returns>
    public bool SetSnapToGrid(bool enabled) =>
        ApplyToggle(NativeConstants.SFVIDM_ARRANGE_GRID, ReadViewOptions().SnapToGrid, enabled, "the align to grid toggle");

    /// <summary>
    /// Turns auto arrange off for the lifetime of the returned scope, then puts it back the way the
    /// user had it.
    /// </summary>
    /// <remarks>
    /// Restoring a layout while auto arrange is on does nothing at all, so every restore runs inside
    /// one of these. The user's own preference is preserved: if they like auto arrange, it is on
    /// again by the time the scope is disposed.
    /// </remarks>
    public IDisposable SuspendAutoArrange()
    {
        ThrowIfDisposed();

        bool wasEnabled = ReadViewOptions().AutoArrange;
        if (wasEnabled)
        {
            SetAutoArrange(false);
        }

        return new AutoArrangeScope(this, wasEnabled);
    }

    /// <summary>
    /// Sends one of the shell's own view commands, but only when the setting is not already right.
    /// </summary>
    /// <remarks>
    /// The <c>SHELLDLL_DefView</c> command ids toggle rather than set, so blindly sending one would
    /// flip a setting that was already correct.
    /// </remarks>
    private bool ApplyToggle(int commandId, bool current, bool desired, string description)
    {
        if (current == desired)
        {
            return false;
        }

        SendTo(_windows.DefView, NativeConstants.WM_COMMAND, commandId, IntPtr.Zero, description);
        return true;
    }

    private sealed class AutoArrangeScope : IDisposable
    {
        private readonly DesktopListView _view;
        private readonly bool _restoreToEnabled;
        private bool _disposed;

        public AutoArrangeScope(DesktopListView view, bool restoreToEnabled)
        {
            _view = view;
            _restoreToEnabled = restoreToEnabled;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_restoreToEnabled)
            {
                _view.SetAutoArrange(true);
            }
        }
    }
}
