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

    /// <summary>Snapshot entries that were paired with a live icon.</summary>
    public required int Matched { get; init; }

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

    /// <summary>True when every entry that could be matched was placed.</summary>
    public bool IsComplete => Missing.Count == 0 && Matched == SnapshotIconCount;

    /// <summary>A one-line summary for the tray balloon or the CLI.</summary>
    public string Summarize()
    {
        string summary = $"Moved {Moved} of {Matched} matched icons";

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
