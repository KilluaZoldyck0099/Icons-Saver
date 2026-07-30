using IconsSaver.Core.Diagnostics;
using IconsSaver.Core.Models;

namespace IconsSaver.Core.Displays;

/// <summary>Details of a display arrangement change that has finished settling.</summary>
/// <param name="Previous">The arrangement that was in force before.</param>
/// <param name="Current">The arrangement now in force.</param>
public sealed record DisplayChangedEventArgs(DisplayFingerprint Previous, DisplayFingerprint Current);

/// <summary>
/// Watches for display arrangement changes and reports them once they have settled.
/// </summary>
/// <remarks>
/// <para>
/// Plugging in a monitor produces a burst of notifications, not one: resolution changes, the taskbar
/// moves, DPI is recalculated, and the shell repacks the desktop icons — and that last step happens
/// <em>after</em> the notification a listener sees. Restoring a layout on the first event would be
/// overwritten seconds later by explorer's own tidy-up, so this class deliberately waits for quiet
/// before reporting anything.
/// </para>
/// <para>
/// Two triggers feed it. A host window can forward <c>WM_DISPLAYCHANGE</c> and friends into
/// <see cref="Notify"/>, which is immediate and precise; and an optional low-frequency poll catches
/// changes that arrive without a broadcast, such as a scaling change applied from Settings.
/// </para>
/// <para>
/// Events are raised on a thread pool thread. UI code must marshal them itself.
/// </para>
/// </remarks>
public sealed class DisplayChangeMonitor : IDisposable
{
    private readonly Timer _settleTimer;
    private readonly Timer _pollTimer;
    private readonly object _gate = new();

    private DisplayFingerprint _known;
    private bool _disposed;

    public DisplayChangeMonitor()
        : this(DisplayEnumerator.Capture())
    {
    }

    public DisplayChangeMonitor(DisplayFingerprint initial)
    {
        ArgumentNullException.ThrowIfNull(initial);

        _known = initial;
        _settleTimer = new Timer(OnSettled, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _pollTimer = new Timer(OnPoll, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>How long the arrangement must stay quiet before a change is reported.</summary>
    public TimeSpan SettleDelay { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>The arrangement this monitor believes is current.</summary>
    public DisplayFingerprint Known
    {
        get
        {
            lock (_gate)
            {
                return _known;
            }
        }
    }

    /// <summary>Raised once after a change has settled.</summary>
    public event EventHandler<DisplayChangedEventArgs>? Changed;

    /// <summary>
    /// Signals that something display-related happened. Safe to call as often as the messages arrive.
    /// </summary>
    public void Notify()
    {
        if (_disposed)
        {
            return;
        }

        // Restarting the timer on every call is the debounce: only the last one in a burst fires.
        _settleTimer.Change(SettleDelay, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Starts a background poll as a safety net for changes that broadcast nothing.</summary>
    public void StartPolling(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        _pollTimer.Change(interval, interval);
    }

    /// <summary>Stops the background poll.</summary>
    public void StopPolling() => _pollTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    /// <summary>
    /// Adopts the current arrangement as the known one without raising an event.
    /// </summary>
    /// <remarks>Used after a restore, so the app does not react to its own work.</remarks>
    public void AcceptCurrent()
    {
        lock (_gate)
        {
            _known = DisplayEnumerator.Capture();
        }
    }

    private void OnPoll(object? state)
    {
        if (_disposed)
        {
            return;
        }

        DisplayFingerprint current = DisplayEnumerator.Capture();
        if (!current.Matches(Known))
        {
            Notify();
        }
    }

    private void OnSettled(object? state)
    {
        if (_disposed)
        {
            return;
        }

        DisplayFingerprint current = DisplayEnumerator.Capture();
        DisplayFingerprint previous;

        lock (_gate)
        {
            if (current.Matches(_known))
            {
                return;
            }

            previous = _known;
            _known = current;
        }

        AppLog.Info($"Display arrangement changed: {previous.Describe()} -> {current.Describe()}.");
        Changed?.Invoke(this, new DisplayChangedEventArgs(previous, current));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _settleTimer.Dispose();
        _pollTimer.Dispose();
    }
}
