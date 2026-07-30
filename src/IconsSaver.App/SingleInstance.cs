namespace IconsSaver.App;

/// <summary>
/// Ensures only one copy of the application runs, and lets a second launch bring the first to front.
/// </summary>
/// <remarks>
/// Two instances would both register the same global hotkeys, both run an automatic snapshot timer,
/// and both show a tray icon. Rather than fail, a second launch behaves the way users expect from a
/// tray utility: it opens the window of the instance that is already there and exits.
/// </remarks>
internal sealed class SingleInstance : IDisposable
{
    /// <summary>
    /// A privately registered message id. Registered window messages are guaranteed unique
    /// system-wide for a given string, so this cannot collide with another application's.
    /// </summary>
    public static readonly uint ShowWindowMessage = NativeMethods.RegisterWindowMessage("IconsSaver.ShowMainWindow");

    private readonly Mutex _mutex;
    private readonly bool _owned;
    private bool _disposed;

    public SingleInstance(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Local\ scopes the mutex to the session, so two users can each run their own copy.
        _mutex = new Mutex(initiallyOwned: true, $@"Local\{name}", out _owned);
        IsFirstInstance = _owned;
    }

    /// <summary>Whether this process is the first instance.</summary>
    public bool IsFirstInstance { get; }

    /// <summary>Asks the instance that is already running to show its window.</summary>
    public static void SignalExisting() =>
        NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, ShowWindowMessage, IntPtr.Zero, IntPtr.Zero);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_owned)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
