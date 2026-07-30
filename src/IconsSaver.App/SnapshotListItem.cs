using IconsSaver.Core.Models;
using IconsSaver.Core.Storage;

namespace IconsSaver.App;

/// <summary>
/// A saved layout together with the store it came from.
/// </summary>
/// <remarks>
/// Manual and automatic layouts are shown in one list but live in separate folders, so a list row
/// has to remember which store to write back to when the user renames or deletes it.
/// </remarks>
/// <param name="Snapshot">The layout itself.</param>
/// <param name="Store">The store that owns the file.</param>
/// <param name="IsAutomatic">Whether a background snapshot produced it.</param>
internal sealed record SnapshotListItem(LayoutSnapshot Snapshot, SnapshotStore Store, bool IsAutomatic)
{
    /// <summary>Label for the "source" column.</summary>
    public string SourceLabel => IsAutomatic ? "Auto" : "Manual";
}
