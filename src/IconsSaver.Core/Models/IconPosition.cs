namespace IconsSaver.Core.Models;

/// <summary>
/// The top-left corner of an icon, in desktop list view client coordinates.
/// </summary>
/// <remarks>
/// The origin is the top-left corner of the <em>virtual screen</em>, not of the primary monitor.
/// On a setup where a second monitor sits to the left of the primary one, the virtual screen
/// begins at a negative screen coordinate, yet list view coordinates stay non-negative: an icon
/// on the far-left monitor is simply near <c>X = 0</c>. Positions therefore only mean something
/// relative to the display arrangement they were captured on, which is why every snapshot stores
/// a display fingerprint alongside them.
/// </remarks>
/// <param name="X">Horizontal offset from the left edge of the virtual screen.</param>
/// <param name="Y">Vertical offset from the top edge of the virtual screen.</param>
public readonly record struct IconPosition(int X, int Y)
{
    /// <summary>The origin of the desktop coordinate space.</summary>
    public static readonly IconPosition Origin = new(0, 0);

    /// <summary>Straight-line distance to <paramref name="other"/>, in pixels.</summary>
    public double DistanceTo(IconPosition other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>Returns this position shifted by the given deltas.</summary>
    public IconPosition Offset(int dx, int dy) => new(X + dx, Y + dy);

    public override string ToString() => $"{X},{Y}";
}
