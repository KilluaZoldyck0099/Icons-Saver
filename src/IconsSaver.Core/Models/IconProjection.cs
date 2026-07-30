namespace IconsSaver.Core.Models;

/// <summary>
/// Maps between desktop icon coordinates and a scaled drawing of them.
/// </summary>
/// <remarks>
/// This lives here rather than inside the preview control because it is the piece where a sign or
/// rounding error would be invisible: the picture would still look right, and only the rectangle
/// the user dragged would silently cover the wrong icons. Keeping it as a pure value type means it
/// can be tested without a window.
/// </remarks>
/// <param name="Scale">Drawing pixels per icon-coordinate pixel.</param>
/// <param name="OriginX">Where icon coordinate 0 sits horizontally in the drawing.</param>
/// <param name="OriginY">Where icon coordinate 0 sits vertically in the drawing.</param>
public readonly record struct IconProjection(float Scale, float OriginX, float OriginY)
{
    /// <summary>A projection that draws nothing, for a surface too small to use.</summary>
    public static readonly IconProjection None = new(0f, 0f, 0f);

    /// <summary>True when there is enough room to draw at all.</summary>
    public bool IsUsable => Scale > 0f;

    /// <summary>
    /// Fits a virtual screen of the given size into a surface, centred, preserving aspect ratio.
    /// </summary>
    public static IconProjection Fit(int virtualWidth, int virtualHeight, int surfaceWidth, int surfaceHeight, int inset)
    {
        if (virtualWidth <= 0 || virtualHeight <= 0)
        {
            return None;
        }

        float usableWidth = surfaceWidth - (inset * 2f);
        float usableHeight = surfaceHeight - (inset * 2f);

        if (usableWidth <= 0f || usableHeight <= 0f)
        {
            return None;
        }

        float scale = Math.Min(usableWidth / virtualWidth, usableHeight / virtualHeight);

        return new IconProjection(
            scale,
            (surfaceWidth - (virtualWidth * scale)) / 2f,
            (surfaceHeight - (virtualHeight * scale)) / 2f);
    }

    /// <summary>Where an icon position lands in the drawing.</summary>
    public (float X, float Y) ToSurface(IconPosition position) =>
        (OriginX + (position.X * Scale), OriginY + (position.Y * Scale));

    /// <summary>Where a region lands in the drawing, as left, top, width, height.</summary>
    public (float X, float Y, float Width, float Height) ToSurface(IconRegion region) =>
        (OriginX + (region.X * Scale), OriginY + (region.Y * Scale), region.Width * Scale, region.Height * Scale);

    /// <summary>
    /// Which icon coordinate a point in the drawing refers to.
    /// </summary>
    /// <remarks>
    /// Truncating towards zero would bias every selection up and to the left by up to a whole
    /// icon-coordinate pixel per drawing pixel, which at the scales this is used at is tens of real
    /// pixels. Rounding keeps the mapping symmetric.
    /// </remarks>
    public IconPosition ToIcon(float surfaceX, float surfaceY) => IsUsable
        ? new IconPosition(
            (int)Math.Round((surfaceX - OriginX) / Scale),
            (int)Math.Round((surfaceY - OriginY) / Scale))
        : IconPosition.Origin;
}
