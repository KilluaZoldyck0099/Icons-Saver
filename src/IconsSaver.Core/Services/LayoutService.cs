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

        if (!view.IconsAreVisible)
        {
            throw new DesktopInteropException(
                "The desktop icons are hidden, so there is nothing to capture. Turn " +
                "'View -> Show desktop icons' back on and try again.");
        }

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

        IconRegion? region = options.Scope.Resolve(snapshot.Display);

        if (region is IconRegion scoped && Unreachable(scoped, snapshot, current) is string reason)
        {
            AppLog.Warn($"Refused to restore {options.Scope} of '{snapshot.DisplayName}': {reason}");

            return new RestoreReport
            {
                DesktopIconCount = live.Count,
                SnapshotIconCount = snapshot.IconCount,
                Matched = 0,
                Moved = 0,
                AlreadyInPlace = 0,
                Outcome = RestoreOutcome.SkippedScopeUnavailable,
                ScopeDescription = options.Scope.ToString(),
                RefusedBecause = reason,
                DisplayMismatch = displayMismatch,
                Duration = stopwatch.Elapsed,
            };
        }

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

        // Narrowing happens on the SAVED position, not the current one. When a monitor is
        // unplugged the shell packs its icons onto whatever screen is left, so the icons that
        // belong to the missing area are precisely the ones that are no longer anywhere near it.
        List<IconMatch> inScope = region is null
            ? match.Matches.ToList()
            : match.Matches.Where(m => region.Value.Contains(m.Entry.Position)).ToList();

        int outOfScope = match.Matches.Count - inScope.Count;

        int moved = 0;
        int alreadyInPlace = 0;
        bool autoArrangeSuspended = false;

        if (options.DryRun)
        {
            alreadyInPlace = inScope.Count(m => m.IsAlreadyInPlace);
        }
        else
        {
            // Switching auto arrange back on repacks the WHOLE desktop, which would undo every
            // region the user did not ask to touch. A narrowed restore can never do that.
            bool reenableAutoArrange = options.ReenableAutoArrange && options.Scope.IsEverything;
            autoArrangeSuspended = view.ReadViewOptions().AutoArrange && !reenableAutoArrange;

            using (view.SuspendAutoArrange(reenableAutoArrange))
            {
                foreach (IconMatch candidate in inScope)
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
            Matched = inScope.Count,
            OutOfScope = outOfScope,
            ScopeDescription = options.Scope.IsEverything ? null : options.Scope.ToString(),
            Moved = moved,
            AlreadyInPlace = alreadyInPlace,
            // Reported within the scope only. Telling someone restoring their left-hand screen
            // about a file that vanished from the right-hand one is noise dressed as a warning.
            Missing = (region is IconRegion m
                    ? match.Missing.Where(e => m.Contains(e.Position))
                    : match.Missing)
                .Select(e => e.Name).ToList(),
            Unknown = (region is IconRegion u
                    ? match.Unknown.Where(i => u.Contains(i.Position))
                    : match.Unknown)
                .Select(i => i.Name).ToList(),
            DisplayMismatch = displayMismatch,
            AutoArrangeSuspended = autoArrangeSuspended,
            Duration = stopwatch.Elapsed,
            Outcome = DetermineOutcome(options, inScope.Count, match.Missing.Count, moved),
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

    /// <summary>
    /// Whether a narrowed restore would write coordinates that cannot land where they are meant to,
    /// and why.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The coordinate handed to the shell is interpreted against the <em>live</em> virtual screen
    /// origin, while the scope was chosen against the snapshot's. When the two origins agree the
    /// spaces are identical and everything is exact; when they do not, the same numbers describe a
    /// different place, and a restore aimed at one screen could scatter icons across another.
    /// </para>
    /// <para>
    /// Refusing rather than approximating is deliberate. A whole-desktop restore at least moves
    /// every icon consistently and can be undone by restoring another layout, but a narrowed one
    /// that silently misses would leave the desktop in a state the user never asked for and cannot
    /// name. The wider restore is still available, which is the point: the dangerous action stays
    /// the explicit one.
    /// </para>
    /// </remarks>
    private static string? Unreachable(IconRegion region, LayoutSnapshot snapshot, DisplayFingerprint current)
    {
        if (snapshot.Display.VirtualLeft != current.VirtualLeft ||
            snapshot.Display.VirtualTop != current.VirtualTop)
        {
            return "the desktop origin has moved since this layout was saved, so part of it can no " +
                   "longer be restored on its own. Restoring the whole layout still works.";
        }

        bool reachable = current.Monitors
            .Select(monitor => IconRegion.ForMonitor(snapshot.Display, monitor))
            .Any(area => area.IntersectsWith(region));

        return reachable
            ? null
            : "that part of the desktop is not attached right now, so its icons would have nowhere to go.";
    }

    private static RestoreOutcome DetermineOutcome(RestoreOptions options, int matched, int missing, int moved)
    {
        if (options.DryRun)
        {
            return RestoreOutcome.Previewed;
        }

        if (matched == 0)
        {
            return RestoreOutcome.NothingToDo;
        }

        // Missing entries are counted against the whole snapshot, not just the scope, so a narrowed
        // restore is not reported as having gaps merely because something elsewhere was deleted.
        if (missing > 0 && options.Scope.IsEverything)
        {
            return RestoreOutcome.CompletedWithGaps;
        }

        return moved == 0 ? RestoreOutcome.NothingToDo : RestoreOutcome.Completed;
    }
}
