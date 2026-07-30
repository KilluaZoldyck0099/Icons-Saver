namespace IconsSaver.Core.Models;

/// <summary>A saved entry paired with the live icon it will be applied to.</summary>
/// <param name="Entry">The recorded icon, holding the position to restore.</param>
/// <param name="Icon">The icon currently on the desktop, holding the list view index to address.</param>
public sealed record IconMatch(IconEntry Entry, DesktopIcon Icon)
{
    /// <summary>True when the icon is already exactly where the snapshot wants it.</summary>
    public bool IsAlreadyInPlace => Icon.Position == Entry.Position;

    public override string ToString() => $"{Entry.Name}: {Icon.Position} -> {Entry.Position}";
}

/// <summary>
/// The result of pairing a saved layout against the icons currently on the desktop.
/// </summary>
/// <param name="Matches">Entries that have somewhere to go.</param>
/// <param name="Missing">Entries whose icon is no longer on the desktop.</param>
/// <param name="Unknown">Icons on the desktop that the snapshot never recorded.</param>
public sealed record IconMatchResult(
    IReadOnlyList<IconMatch> Matches,
    IReadOnlyList<IconEntry> Missing,
    IReadOnlyList<DesktopIcon> Unknown)
{
    /// <summary>An empty result, used when there is nothing to restore.</summary>
    public static readonly IconMatchResult Empty = new(
        Array.Empty<IconMatch>(),
        Array.Empty<IconEntry>(),
        Array.Empty<DesktopIcon>());

    /// <summary>True when every saved entry found a home and nothing new has appeared.</summary>
    public bool IsExact => Missing.Count == 0 && Unknown.Count == 0;
}
