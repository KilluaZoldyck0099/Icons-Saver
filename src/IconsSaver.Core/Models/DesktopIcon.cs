namespace IconsSaver.Core.Models;

/// <summary>
/// One icon as it currently exists on the desktop.
/// </summary>
/// <param name="Index">
/// Position of the item in the list view. Useful for issuing further list view messages during the
/// same read, but worthless as a persistent identifier: adding, deleting or re-sorting items
/// renumbers everything.
/// </param>
/// <param name="Name">The display name shown under the icon.</param>
/// <param name="Position">Where the icon sits, in desktop client coordinates.</param>
public sealed record DesktopIcon(int Index, string Name, IconPosition Position)
{
    /// <summary>True when the shell returned an empty display name for this item.</summary>
    public bool IsUnnamed => string.IsNullOrWhiteSpace(Name);

    public override string ToString() => $"[{Index}] {Name} @ {Position}";
}
