using System.Diagnostics;
using IconsSaver.Core.Desktop;
using IconsSaver.Core.Diagnostics;
using IconsSaver.Core.Displays;
using IconsSaver.Core.Models;
using IconsSaver.Core.Storage;

namespace IconsSaver.Core.Services;

/// <summary>
/// The operations the whole application is built on: take a snapshot, put one back.
/// </summary>
/// <remarks>
/// Each call opens its own short-lived connection to the shell rather than holding one open.
/// Explorer restarts more often than people think — a crash, a theme change, a "restart Windows
/// Explorer" from Task Manager — and a cached window handle would quietly stop working.
/// </remarks>
public sealed class LayoutService
{
    private readonly SnapshotStore _snapshots;

    public LayoutService()
        : this(new SnapshotStore())
    {
    }

    public LayoutService(SnapshotStore snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        _snapshots = snapshots;
    }

    /// <summary>The store layouts are read from and written to.</summary>
    public SnapshotStore Snapshots => _snapshots;

    /// <summary>Whether the desktop icon view can currently be reached.</summary>
    public static bool IsDesktopAvailable => DesktopWindowLocator.TryLocate(out _);

    /// <summary>Reads the desktop without saving anything.</summary>
    public IReadOnlyList<DesktopIcon> ReadDesktop()
    {
        using DesktopListView view = DesktopListView.Open();
        return view.ReadAll();
    }

    /// <summary>Reads the current auto arrange and align-to-grid settings.</summary>
    public DesktopViewOptions ReadViewOptions()
    {
        using DesktopListView view = DesktopListView.Open();
        return view.ReadViewOptions();
    }

    /// <summary>Captures the current layout in memory.</summary>
    /// <exception cref="DesktopInteropException">The desktop could not be read.</exception>
    public LayoutSnapshot Capture(string name, bool automatic = false)
    {
        using DesktopListView view = DesktopListView.Open();

        IReadOnlyList<DesktopIcon> icons = view.ReadAll();
        DesktopViewOptions viewOptions = view.ReadViewOptions();
        DisplayFingerprint display = DisplayEnumerator.Capture();

        LayoutSnapshot snapshot = LayoutSnapshot.Create(name, icons, display, viewOptions, automatic);
        AppLog.Info($"Captured {snapshot.IconCount} icons on {display}.");
        return snapshot;
    }

    /// <summary>Captures the current layout and writes it to the store.</summary>
    public LayoutSnapshot CaptureAndSave(string name, bool automatic = false)
    {
        LayoutSnapshot snapshot = Capture(name, automatic);
        _snapshots.Save(snapshot);
        return snapshot;
    }

    /// <summary>Applies a saved layout to the desktop.</summary>
    /// <exception cref="DesktopInteropException">The desktop could not be reached.</exception>
    public RestoreReport Restore(LayoutSnapshot snapshot, RestoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        options ??= RestoreOptions.Manual;

        var stopwatch = Stopwatch.StartNew();

        using DesktopListView view = DesktopListView.Open();
        IReadOnlyList<DesktopIcon> live = view.ReadAll();
        DisplayFingerprint current = DisplayEnumerator.Capture();
        bool displayMismatch = !current.Matches(snapshot.Display);

        if (displayMismatch && options.RequireMatchingDisplay)
        {
            AppLog.Warn(
                $"Skipped restoring '{snapshot.DisplayName}': saved on {snapshot.Display.Describe()}, " +
                $"current setup is {current.Describe()}.");

            return new RestoreReport
            {
                DesktopIconCount = live.Count,
                SnapshotIconCount = snapshot.IconCount,
                Matched = 0,
                Moved = 0,
                AlreadyInPlace = 0,
                Outcome = RestoreOutcome.SkippedDisplayMismatch,
                DisplayMismatch = true,
                Duration = stopwatch.Elapsed,
            };
        }

        IconMatchResult match = IconMatcher.Match(snapshot.Icons, live);

        int moved = 0;
        int alreadyInPlace = 0;
        bool autoArrangeSuspended = false;

        if (options.DryRun)
        {
            alreadyInPlace = match.Matches.Count(m => m.IsAlreadyInPlace);
        }
        else
        {
            autoArrangeSuspended = view.ReadViewOptions().AutoArrange && !options.ReenableAutoArrange;

            using (view.SuspendAutoArrange(options.ReenableAutoArrange))
            {
                foreach (IconMatch candidate in match.Matches)
                {
                    if (candidate.IsAlreadyInPlace)
                    {
                        alreadyInPlace++;
                        continue;
                    }

                    if (view.SetPosition(candidate.Icon.Index, candidate.Entry.Position))
                    {
                        moved++;
                    }
                }
            }

            view.Redraw();
        }

        var report = new RestoreReport
        {
            DesktopIconCount = live.Count,
            SnapshotIconCount = snapshot.IconCount,
            Matched = match.Matches.Count,
            Moved = moved,
            AlreadyInPlace = alreadyInPlace,
            Missing = match.Missing.Select(e => e.Name).ToList(),
            Unknown = match.Unknown.Select(i => i.Name).ToList(),
            DisplayMismatch = displayMismatch,
            AutoArrangeSuspended = autoArrangeSuspended,
            Duration = stopwatch.Elapsed,
            Outcome = DetermineOutcome(options, match, moved),
        };

        AppLog.Info($"Restored '{snapshot.DisplayName}': {report.Summarize()}");
        return report;
    }

    /// <summary>Loads a layout by id and restores it.</summary>
    /// <exception cref="FileNotFoundException">No layout with that id exists.</exception>
    public RestoreReport RestoreById(string id, RestoreOptions? options = null)
    {
        LayoutSnapshot snapshot = _snapshots.Load(id)
            ?? throw new FileNotFoundException($"No saved layout with id '{id}'.");

        return Restore(snapshot, options);
    }

    /// <summary>
    /// Finds the newest saved layout that belongs to the current display arrangement.
    /// </summary>
    /// <remarks>
    /// Falls back to a geometry-only comparison, which covers the common case of a monitor being
    /// replaced by an identical one: the device id changes but no icon position becomes invalid.
    /// </remarks>
    public LayoutSnapshot? FindBestForCurrentDisplay(IEnumerable<LayoutSnapshot>? candidates = null)
    {
        DisplayFingerprint current = DisplayEnumerator.Capture();
        List<LayoutSnapshot> pool = (candidates ?? _snapshots.LoadAll())
            .OrderByDescending(s => s.CapturedAt)
            .ToList();

        return pool.FirstOrDefault(s => current.Matches(s.Display))
            ?? pool.FirstOrDefault(s => current.MatchesGeometry(s.Display));
    }

    private static RestoreOutcome DetermineOutcome(RestoreOptions options, IconMatchResult match, int moved)
    {
        if (options.DryRun)
        {
            return RestoreOutcome.Previewed;
        }

        if (match.Matches.Count == 0)
        {
            return RestoreOutcome.NothingToDo;
        }

        if (match.Missing.Count > 0)
        {
            return RestoreOutcome.CompletedWithGaps;
        }

        return moved == 0 ? RestoreOutcome.NothingToDo : RestoreOutcome.Completed;
    }
}
