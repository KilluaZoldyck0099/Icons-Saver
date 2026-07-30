using IconsSaver.Core.Models;
using Xunit;

namespace IconsSaver.Tests;

/// <summary>
/// The preview's coordinate mapping. A sign or rounding error here would leave the picture looking
/// perfectly correct while the rectangle the user dragged quietly covered different icons, so the
/// round trip is pinned down rather than eyeballed.
/// </summary>
public sealed class IconProjectionTests
{
    // The real virtual screen this was developed against.
    private const int VirtualWidth = 5280;
    private const int VirtualHeight = 3253;

    [Fact]
    public void A_surface_too_small_to_draw_on_is_unusable()
    {
        Assert.False(IconProjection.Fit(VirtualWidth, VirtualHeight, 10, 10, 10).IsUsable);
        Assert.False(IconProjection.Fit(0, 0, 400, 300, 10).IsUsable);
        Assert.False(IconProjection.None.IsUsable);
    }

    [Fact]
    public void An_unusable_projection_maps_everything_to_the_origin()
    {
        Assert.Equal(IconPosition.Origin, IconProjection.None.ToIcon(123, 456));
    }

    [Fact]
    public void The_drawing_is_centred_on_the_surface()
    {
        IconProjection projection = IconProjection.Fit(VirtualWidth, VirtualHeight, 400, 400, 10);

        (float left, float top) = projection.ToSurface(IconPosition.Origin);
        (float right, float bottom) = projection.ToSurface(new IconPosition(VirtualWidth, VirtualHeight));

        // Equal gaps on opposite sides, in both directions.
        Assert.Equal(left, 400 - right, precision: 3);
        Assert.Equal(top, 400 - bottom, precision: 3);
    }

    [Fact]
    public void Aspect_ratio_is_preserved()
    {
        IconProjection projection = IconProjection.Fit(VirtualWidth, VirtualHeight, 400, 900, 10);

        (float _, float _, float width, float height) = projection.ToSurface(new IconRegion(0, 0, VirtualWidth, VirtualHeight));

        Assert.Equal((double)VirtualWidth / VirtualHeight, width / height, precision: 3);
    }

    [Fact]
    public void The_drawing_fits_inside_the_inset()
    {
        const int Inset = 10;
        IconProjection projection = IconProjection.Fit(VirtualWidth, VirtualHeight, 400, 300, Inset);

        (float x, float y, float width, float height) = projection.ToSurface(new IconRegion(0, 0, VirtualWidth, VirtualHeight));

        Assert.True(x >= Inset - 0.01f);
        Assert.True(y >= Inset - 0.01f);
        Assert.True(x + width <= 400 - Inset + 0.01f);
        Assert.True(y + height <= 300 - Inset + 0.01f);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(13, 695)]
    [InlineData(1453, 1184)]
    [InlineData(5279, 3252)]
    public void A_position_survives_the_round_trip(int x, int y)
    {
        IconProjection projection = IconProjection.Fit(VirtualWidth, VirtualHeight, 400, 300, 10);

        var original = new IconPosition(x, y);
        (float surfaceX, float surfaceY) = projection.ToSurface(original);
        IconPosition returned = projection.ToIcon(surfaceX, surfaceY);

        // The drawing is about a fourteenth of full size here, so one drawing pixel is many icon
        // pixels; the round trip should still land within one drawing pixel's worth.
        double tolerance = 1.0 / projection.Scale;

        Assert.True(Math.Abs(returned.X - original.X) <= tolerance, $"x drifted from {original.X} to {returned.X}");
        Assert.True(Math.Abs(returned.Y - original.Y) <= tolerance, $"y drifted from {original.Y} to {returned.Y}");
    }

    [Fact]
    public void Clicking_the_top_left_of_the_drawing_gives_icon_origin()
    {
        IconProjection projection = IconProjection.Fit(VirtualWidth, VirtualHeight, 400, 300, 10);

        (float x, float y) = projection.ToSurface(IconPosition.Origin);
        IconPosition clicked = projection.ToIcon(x, y);

        Assert.Equal(0, clicked.X);
        Assert.Equal(0, clicked.Y);
    }

    [Fact]
    public void Clicking_left_of_the_drawing_gives_a_negative_coordinate()
    {
        // The backdrop around the monitors is not part of any screen; the caller has to see that
        // rather than be handed a clamped zero that looks like a real position.
        IconProjection projection = IconProjection.Fit(VirtualWidth, VirtualHeight, 400, 300, 10);

        (float x, float y) = projection.ToSurface(IconPosition.Origin);
        IconPosition clicked = projection.ToIcon(x - 20, y);

        Assert.True(clicked.X < 0);
    }

    [Fact]
    public void A_wider_surface_produces_a_bigger_scale()
    {
        IconProjection small = IconProjection.Fit(VirtualWidth, VirtualHeight, 200, 2000, 10);
        IconProjection large = IconProjection.Fit(VirtualWidth, VirtualHeight, 800, 2000, 10);

        Assert.True(large.Scale > small.Scale);
    }
}
