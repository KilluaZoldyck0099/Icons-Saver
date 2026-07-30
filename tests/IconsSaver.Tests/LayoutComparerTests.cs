using IconsSaver.Core.Models;
using IconsSaver.Core.Services;
using Xunit;

namespace IconsSaver.Tests;

public sealed class LayoutComparerTests
{
    [Fact]
    public void Identical_layouts_compare_equal()
    {
        LayoutSnapshot left = Snapshot(("A", 0, 0), ("B", 100, 0));
        LayoutSnapshot right = Snapshot(("A", 0, 0), ("B", 100, 0));

        Assert.True(LayoutComparer.HaveSameLayout(left, right));
    }

    [Fact]
    public void Order_does_not_matter()
    {
        // Explorer renumbers items constantly; a reordered list is not a changed layout.
        LayoutSnapshot left = Snapshot(("A", 0, 0), ("B", 100, 0));
        LayoutSnapshot right = Snapshot(("B", 100, 0), ("A", 0, 0));

        Assert.True(LayoutComparer.HaveSameLayout(left, right));
    }

    [Fact]
    public void A_moved_icon_is_a_difference()
    {
        LayoutSnapshot left = Snapshot(("A", 0, 0));
        LayoutSnapshot right = Snapshot(("A", 0, 1));

        Assert.False(LayoutComparer.HaveSameLayout(left, right));
        Assert.Equal(2, LayoutComparer.CountDifferences(left, right));
    }

    [Fact]
    public void A_different_icon_count_is_a_difference()
    {
        Assert.False(LayoutComparer.HaveSameLayout(Snapshot(("A", 0, 0)), Snapshot(("A", 0, 0), ("B", 1, 1))));
    }

    [Fact]
    public void Null_on_one_side_is_not_equal()
    {
        Assert.False(LayoutComparer.HaveSameLayout(null, Snapshot(("A", 0, 0))));
        Assert.False(LayoutComparer.HaveSameLayout(Snapshot(("A", 0, 0)), null));
        Assert.True(LayoutComparer.HaveSameLayout(null, null));
    }

    [Fact]
    public void Duplicate_names_at_different_places_are_tracked_separately()
    {
        LayoutSnapshot left = Snapshot(("Shortcut", 0, 0), ("Shortcut", 50, 50));
        LayoutSnapshot same = Snapshot(("Shortcut", 50, 50), ("Shortcut", 0, 0));
        LayoutSnapshot moved = Snapshot(("Shortcut", 0, 0), ("Shortcut", 60, 50));

        Assert.True(LayoutComparer.HaveSameLayout(left, same));
        Assert.False(LayoutComparer.HaveSameLayout(left, moved));
    }

    private static LayoutSnapshot Snapshot(params (string Name, int X, int Y)[] icons) => new()
    {
        Name = "test",
        Icons = icons.Select((icon, index) => new IconEntry(icon.Name, icon.X, icon.Y, index)).ToList(),
    };
}
