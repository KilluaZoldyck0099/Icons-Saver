using IconsSaver.Core.Diagnostics;
using IconsSaver.Core.Models;
using IconsSaver.Core.Storage;

namespace IconsSaver.Core.Services;

/// <summary>
/// Keeps a rolling history of automatic snapshots.
/// </summary>
/// <remarks>
/// The point of the whole application is to have a good layout to go back to. Relying on the user
/// to remember to press "Save" before the thing that scrambles their icons defeats that, so a
/// background timer keeps a short history for them. Automatic snapshots live in their own folder so
/// pruning them can never touch a layout the user saved deliberately.
/// </remarks>
public sealed class AutoSaveService : IDisposable
{
    private readonly LayoutService _layouts;
    private readonly SnapshotStore _store;
    private readonly Timer _timer;
    private readonly object _gate = new();

    private bool _disposed;
    private bool _running;

    public AutoSaveService(LayoutService layouts)
        : this(layouts, new SnapshotStore(AppPaths.AutoSnapshotsFolder))
    {
    }

    public AutoSaveService(LayoutService layouts, SnapshotStore store)
    {
        ArgumentNullException.ThrowIfNull(layouts);
        ArgumentNullException.ThrowIfNull(store);

        _layouts = layouts;
        _store = store;
        _timer = new Timer(OnTick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>How often a snapshot is taken.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>How many automatic snapshots to keep.</summary>
    public int HistoryCount { get; set; } = 12;

    /// <summary>Skip a snapshot when nothing has moved since the previous one.</summary>
    public bool OnlyWhenChanged { get; set; } = true;

    /// <summary>The store automatic snapshots are written to.</summary>
    public SnapshotStore Store => _store;

    /// <summary>Whether the timer is currently running.</summary>
    public bool IsRunning => _running;

    /// <summary>Raised after a snapshot is written.</summary>
    public event EventHandler<LayoutSnapshot>? Saved;

    /// <summary>Raised when a snapshot could not be taken. Never rethrown onto the timer thread.</summary>
    public event EventHandler<Exception>? Failed;

    /// <summary>Starts taking snapshots on the configured interval.</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _running = true;
        _timer.Change(Interval, Interval);
        AppLog.Info($"Automatic snapshots every {Interval.TotalMinutes:0} minutes, keeping {HistoryCount}.");
    }

    /// <summary>Stops taking snapshots.</summary>
    public void Stop()
    {
        _running = false;
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Applies changed settings to a service that may already be running.</summary>
    public void Reconfigure(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Interval = TimeSpan.FromMinutes(settings.AutoSaveIntervalMinutes);
        HistoryCount = settings.AutoSaveHistoryCount;
        OnlyWhenChanged = settings.AutoSaveOnlyWhenChanged;

        if (settings.AutoSaveEnabled)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    /// <summary>
    /// Takes one snapshot now, honouring <see cref="OnlyWhenChanged"/>.
    /// </summary>
    /// <returns>The snapshot that was written, or <see langword="null"/> when it was skipped.</returns>
    public LayoutSnapshot? RunOnce()
    {
        lock (_gate)
        {
            LayoutSnapshot candidate = _layouts.Capture(
                $"Auto {DateTimeOffset.Now.LocalDateTime:yyyy-MM-dd HH:mm}",
                automatic: true);

            if (candidate.IconCount == 0)
            {
                // "Show desktop icons" is off, so there is nothing to record and a snapshot now
                // would only overwrite the history with an empty layout.
                return null;
            }

            if (OnlyWhenChanged)
            {
                LayoutSnapshot? previous = _store.LoadAll().FirstOrDefault();
                if (LayoutComparer.HaveSameLayout(previous, candidate))
                {
                    return null;
                }
            }

            _store.Save(candidate);
            Prune();
            return candidate;
        }
    }

    /// <summary>Deletes the oldest automatic snapshots beyond <see cref="HistoryCount"/>.</summary>
    public int Prune()
    {
        int deleted = 0;

        foreach (LayoutSnapshot snapshot in _store.LoadAll().Where(s => !s.IsPinned).Skip(Math.Max(HistoryCount, 1)))
        {
            if (_store.Delete(snapshot.Id))
            {
                deleted++;
            }
        }

        return deleted;
    }

    private void OnTick(object? state)
    {
        try
        {
            LayoutSnapshot? snapshot = RunOnce();
            if (snapshot is not null)
            {
                Saved?.Invoke(this, snapshot);
            }
        }
        catch (Exception exception)
        {
            // An automatic snapshot failing must never take the application down with it.
            AppLog.Error("Automatic snapshot failed.", exception);
            Failed?.Invoke(this, exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _running = false;
        _timer.Dispose();
    }
}
