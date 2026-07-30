namespace IconsSaver.App;

/// <summary>
/// An invisible top level window that exists purely to receive broadcast messages.
/// </summary>
/// <remarks>
/// <para>
/// A message-only window would be the obvious choice, except that Windows does not deliver
/// broadcasts such as <c>WM_DISPLAYCHANGE</c> to them: those go to top level windows only. So this
/// is a real top level window that is simply never shown and parked far off screen.
/// </para>
/// <para>
/// It also carries the global hotkey registrations and the "another instance wants you to open"
/// message, so the application keeps working when the main window has been closed to the tray.
/// </para>
/// </remarks>
internal sealed class MessageWindow : Form
{
    public MessageWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-32000, -32000);
        Size = new Size(1, 1);
        Text = "Icons Saver message window";

        // The handle must exist before anything can be delivered to it, and the window is never
        // shown, so it has to be created explicitly.
        CreateHandle();
    }

    /// <summary>Raised when anything display related happened. Fires often; the listener debounces.</summary>
    public event EventHandler? DisplayEvent;

    /// <summary>Raised when a registered global hotkey was pressed, with its id.</summary>
    public event EventHandler<int>? HotkeyPressed;

    /// <summary>Raised when another instance asked for the main window to be shown.</summary>
    public event EventHandler? ShowRequested;

    /// <summary>Keeps the window hidden no matter who asks.</summary>
    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case NativeMethods.WM_DISPLAYCHANGE:
            case NativeMethods.WM_SETTINGCHANGE:
            case NativeMethods.WM_DPICHANGED:
                DisplayEvent?.Invoke(this, EventArgs.Empty);
                break;

            case NativeMethods.WM_HOTKEY:
                HotkeyPressed?.Invoke(this, (int)m.WParam);
                break;

            default:
                if (m.Msg == SingleInstance.ShowWindowMessage)
                {
                    ShowRequested?.Invoke(this, EventArgs.Empty);
                }

                break;
        }

        base.WndProc(ref m);
    }
}
