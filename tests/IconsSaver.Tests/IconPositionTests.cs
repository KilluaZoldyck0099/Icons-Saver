using IconsSaver.Core.Models;
using Xunit;

namespace IconsSaver.Tests;

public sealed class IconPositionTests
{
    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(new IconPosition(10, 20), new IconPosition(10, 20));
        Assert.NotEqual(new IconPosition(10, 20), new IconPosition(20, 10));
    }

    [Fact]
    public void Offset_shifts_both_axes()
    {
        IconPosition moved = new IconPosition(100, 200).Offset(-40, 15);

        Assert.Equal(60, moved.X);
        Assert.Equal(215, moved.Y);
    }

    [Fact]
    public void DistanceTo_measures_a_straight_line()
    {
        double distance = new IconPosition(0, 0).DistanceTo(new IconPosition(3, 4));

        Assert.Equal(5d, distance, precision: 6);
    }

    [Fact]
    public void Origin_is_zero()
    {
        Assert.Equal(new IconPosition(0, 0), IconPosition.Origin);
    }

    [Fact]
    public void Negative_coordinates_round_trip()
    {
        // A monitor above and to the left of the primary produces negative virtual coordinates,
        // which is exactly the case the packed 16-bit list view message cannot represent.
        var position = new IconPosition(-1440, -1080);

        Assert.Equal(-1440, position.X);
        Assert.Equal(-1080, position.Y);
    }
}
