namespace IconsSaver.Core.Models;

/// <summary>
/// One icon as recorded in a saved layout.
/// </summary>
/// <param name="Name">The display name, which is what a restore matches on.</param>
/// <param name="X">Horizontal position in desktop client coordinates.</param>
/// <param name="Y">Vertical position in desktop client coordinates.</param>
/// <param name="Index">
/// The list view index the icon had when the snapshot was taken. Kept only as a tie-breaker when
/// several icons share a display name, and as a last-resort fallback for unnamed items.
/// </param>
public sealed record IconEntry(string Name, int X, int Y, int Index)
{
    /// <summary>The recorded position as a value object.</summary>
    public IconPosition Position => new(X, Y);

    /// <summary>Creates an entry from a live desktop icon.</summary>
    public static IconEntry FromDesktopIcon(DesktopIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        return new IconEntry(icon.Name, icon.Position.X, icon.Position.Y, icon.Index);
    }

    public override string ToString() => $"{Name} @ {X},{Y}";
}
