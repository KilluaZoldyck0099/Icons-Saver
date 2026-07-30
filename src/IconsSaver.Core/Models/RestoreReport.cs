namespace IconsSaver.Core.Models;

/// <summary>
/// What happened during a restore.
/// </summary>
/// <remarks>
/// A restore is rarely all-or-nothing: files get deleted and created between snapshots, so some
/// saved entries have nowhere to go and some live icons were never recorded. Reporting both keeps
/// the user informed instead of leaving them to guess why one icon did not move.
/// </remarks>
public sealed record RestoreReport
{
    /// <summary>Icons that were on the desktop when the restore ran.</summary>
    public required int DesktopIconCount { get; init; }

    /// <summary>Entries the snapshot contained.</summary>
    public required int SnapshotIconCount { get; init; }

    /// <summary>How the restore ended.</summary>
    public RestoreOutcome Outcome { get; init; } = RestoreOutcome.Completed;

    /// <summary>Snapshot entries that were paired with a live icon and fell inside the scope.</summary>
    public required int Matched { get; init; }

    /// <summary>
    /// Entries that were paired with a live icon but sit outside the part of the desktop being
    /// restored, and so were deliberately left alone.
    /// </summary>
    public int OutOfScope { get; init; }

    /// <summary>What the restore was narrowed to, or <see langword="null"/> for the whole desktop.</summary>
    public string? ScopeDescription { get; init; }

    /// <summary>Icons the shell actually accepted a new position for.</summary>
    public required int Moved { get; init; }

    /// <summary>Icons that were already exactly where the snapshot wanted them.</summary>
    public required int AlreadyInPlace { get; init; }

    /// <summary>Names present in the snapshot but no longer on the desktop.</summary>
    public IReadOnlyList<string> Missing { get; init; } = Array.Empty<string>();

    /// <summary>Names on the desktop that the snapshot never recorded, and so were left alone.</summary>
    public IReadOnlyList<string> Unknown { get; init; } = Array.Empty<string>();

    /// <summary>True when the snapshot was taken on a different display arrangement.</summary>
    public bool DisplayMismatch { get; init; }

    /// <summary>True when auto arrange had to be turned off and back on again.</summary>
    public bool AutoArrangeSuspended { get; init; }

    /// <summary>How long the restore took.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Set when the restore was refused, explaining why.</summary>
    public string? RefusedBecause { get; init; }

    /// <summary>
    /// True when every entry that could have been placed was placed.
    /// </summary>
    /// <remarks>
    /// Measured against the part of the layout that was actually in play. A restore narrowed to one
    /// screen is complete when that screen is right, not when the other screens are.
    /// </remarks>
    public bool IsComplete =>
        Missing.Count == 0 &&
        Outcome is RestoreOutcome.Completed or RestoreOutcome.NothingToDo &&
        (ScopeDescription is not null || Matched == SnapshotIconCount);

    /// <summary>A one-line summary for the tray balloon or the CLI.</summary>
    public string Summarize()
    {
        if (RefusedBecause is not null)
        {
            return RefusedBecause;
        }

        string summary = ScopeDescription is null
            ? $"Moved {Moved} of {Matched} matched icons"
            : $"Moved {Moved} of {Matched} icons in {ScopeDescription}";

        if (OutOfScope > 0)
        {
            summary += $", left {OutOfScope} elsewhere alone";
        }

        if (AlreadyInPlace > 0)
        {
            summary += $", {AlreadyInPlace} already in place";
        }

        if (Missing.Count > 0)
        {
            summary += $", {Missing.Count} no longer on the desktop";
        }

        if (Unknown.Count > 0)
        {
            summary += $", {Unknown.Count} new since the snapshot";
        }

        return summary + '.';
    }

    public override string ToString() => Summarize();
}
