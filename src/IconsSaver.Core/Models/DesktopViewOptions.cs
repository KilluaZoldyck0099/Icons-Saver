namespace IconsSaver.Core.Models;

/// <summary>
/// The two desktop view settings that decide whether free icon positioning is possible at all.
/// </summary>
/// <param name="AutoArrange">
/// When on, the shell packs icons into columns and immediately undoes any position written to it,
/// so it has to be turned off for the duration of a restore.
/// </param>
/// <param name="SnapToGrid">
/// When on, positions are rounded to the nearest cell. Restores still work, but the icons land on
/// grid intersections rather than on the exact pixels that were captured.
/// </param>
public readonly record struct DesktopViewOptions(bool AutoArrange, bool SnapToGrid)
{
    /// <summary>The configuration in which saved positions round-trip exactly.</summary>
    public static readonly DesktopViewOptions FreePositioning = new(AutoArrange: false, SnapToGrid: false);

    /// <summary>True when positions written to the shell will survive unchanged.</summary>
    public bool AllowsExactPositions => !AutoArrange;

    public override string ToString() =>
        $"auto arrange {(AutoArrange ? "on" : "off")}, align to grid {(SnapToGrid ? "on" : "off")}";
}
