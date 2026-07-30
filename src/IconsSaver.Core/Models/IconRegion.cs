namespace IconsSaver.Core.Models;

/// <summary>
/// A rectangle in desktop icon coordinates.
/// </summary>
/// <remarks>
/// <para>
/// The same coordinate space as <see cref="IconPosition"/>: the origin is the top-left of the
/// virtual screen. Monitor bounds, by contrast, are in screen coordinates, which is why
/// <see cref="ForMonitor"/> exists rather than callers subtracting origins by hand.
/// </para>
/// <para>
/// This is deliberately not <c>System.Drawing.Rectangle</c>. IconsSaver.Core has no drawing
/// dependency and should not acquire one just to hold four integers.
/// </para>
/// </remarks>
public readonly record struct IconRegion(int X, int Y, int Width, int Height)
{
    /// <summary>One past the rightmost column in the region.</summary>
    public int Right => X + Width;

    /// <summary>One past the bottom row in the region.</summary>
    public int Bottom => Y + Height;

    /// <summary>True when the region cannot contain anything.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Whether an icon placed at <paramref name="position"/> belongs to this region.
    /// </summary>
    /// <remarks>
    /// The test is on the icon's top-left corner, and the interval is half-open: <c>[X, Right)</c>
    /// and <c>[Y, Bottom)</c>. That matters at the seam between two adjacent monitors, where the
    /// right edge of one is the exact left edge of the next — a closed interval would let both
    /// claim the same icon, and a restore scoped to either monitor would then move it.
    /// </remarks>
    public bool Contains(IconPosition position) =>
        position.X >= X && position.X < Right &&
        position.Y >= Y && position.Y < Bottom;

    /// <summary>Whether this region and <paramref name="other"/> share any area at all.</summary>
    public bool IntersectsWith(IconRegion other) =>
        X < other.Right && Right > other.X &&
        Y < other.Bottom && Bottom > other.Y;

    /// <summary>Builds a region from two opposite corners, in any order.</summary>
    public static IconRegion FromCorners(int x1, int y1, int x2, int y2) => new(
        Math.Min(x1, x2),
        Math.Min(y1, y2),
        Math.Abs(x2 - x1),
        Math.Abs(y2 - y1));

    /// <summary>
    /// The area a monitor covers, translated out of screen coordinates into icon coordinates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="MonitorDescriptor.Left"/> and <see cref="MonitorDescriptor.Top"/> are screen
    /// coordinates, which are negative for any monitor placed left of or above the primary. Icon
    /// coordinates start at the top-left of the virtual screen and so are never negative. The
    /// difference between the two is exactly the virtual screen origin.
    /// </para>
    /// <para>
    /// Worked example from a real three-monitor setup: the virtual screen starts at
    /// <c>(-1440, -1080)</c> and a portrait monitor sits at screen <c>(-1440, -387)</c>. Its region
    /// is therefore <c>(0, 693)</c> — and an icon read back from the shell at <c>(13, 695)</c> does
    /// land just inside it, which is where that monitor's top-left icon actually is.
    /// </para>
    /// </remarks>
    public static IconRegion ForMonitor(DisplayFingerprint display, MonitorDescriptor monitor)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(monitor);

        return new IconRegion(
            monitor.Left - display.VirtualLeft,
            monitor.Top - display.VirtualTop,
            monitor.Width,
            monitor.Height);
    }

    public override string ToString() => $"{Width}x{Height} at {X},{Y}";
}
