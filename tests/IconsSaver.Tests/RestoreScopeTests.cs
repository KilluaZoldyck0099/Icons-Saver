using IconsSaver.Core.Models;
using Xunit;

namespace IconsSaver.Tests;

/// <summary>
/// Scoping a restore is the one feature that can move the wrong icons rather than simply doing
/// nothing, so the arithmetic is pinned down here with numbers taken from a real three-monitor
/// setup rather than with round ones that hide sign errors.
/// </summary>
public sealed class RestoreScopeTests
{
    // Virtual screen origin (-1440, -1080), size 5280x3253.
    //   DISPLAY1  3840x2160 at screen (0, 0)        primary  -> icon region (1440, 1080)
    //   DISPLAY3  1440x2560 at screen (-1440, -387)          -> icon region (0, 693)
    //   DISPLAY2  1920x1080 at screen (182, -1080)           -> icon region (1622, 0)
    private static readonly MonitorDescriptor Primary =
        new(@"\\.\DISPLAY1", "DISPLAY#PRIMARY", 0, 0, 3840, 2160, true, 96);

    private static readonly MonitorDescriptor Portrait =
        new(@"\\.\DISPLAY3", "DISPLAY#PORTRAIT", -1440, -387, 1440, 2560, false, 96);

    private static readonly MonitorDescriptor Above =
        new(@"\\.\DISPLAY2", "DISPLAY#ABOVE", 182, -1080, 1920, 1080, false, 96);

    [Fact]
    public void A_monitor_left_of_the_primary_starts_at_icon_x_zero()
    {
        IconRegion region = IconRegion.ForMonitor(Display(), Portrait);

        Assert.Equal(new IconRegion(0, 693, 1440, 2560), region);
    }

    [Fact]
    public void A_real_icon_read_from_the_shell_lands_on_the_monitor_it_is_actually_on()
    {
        // 'emerald_premium_icons' was read back at (13, 695) on the machine this was developed on,
        // and it sits at the top-left of the portrait monitor.
        IconRegion portrait = IconRegion.ForMonitor(Display(), Portrait);
        IconRegion primary = IconRegion.ForMonitor(Display(), Primary);

        var icon = new IconPosition(13, 695);

        Assert.True(portrait.Contains(icon));
        Assert.False(primary.Contains(icon));
    }

    [Fact]
    public void The_primary_monitor_region_is_offset_by_the_virtual_origin()
    {
        Assert.Equal(new IconRegion(1440, 1080, 3840, 2160), IconRegion.ForMonitor(Display(), Primary));
    }

    [Fact]
    public void An_icon_on_the_seam_between_two_monitors_belongs_to_exactly_one()
    {
        // The portrait monitor's region ends at x = 1440 and the primary's begins there.
        IconRegion portrait = IconRegion.ForMonitor(Display(), Portrait);
        IconRegion primary = IconRegion.ForMonitor(Display(), Primary);

        var onTheSeam = new IconPosition(1440, 1200);

        Assert.False(portrait.Contains(onTheSeam));
        Assert.True(primary.Contains(onTheSeam));
    }

    [Fact]
    public void The_far_corner_of_a_region_is_outside_it()
    {
        var region = new IconRegion(100, 100, 50, 50);

        Assert.True(region.Contains(new IconPosition(100, 100)));
        Assert.True(region.Contains(new IconPosition(149, 149)));
        Assert.False(region.Contains(new IconPosition(150, 150)));
    }

    [Fact]
    public void A_region_from_corners_works_whichever_way_it_was_dragged()
    {
        Assert.Equal(
            IconRegion.FromCorners(10, 10, 60, 40),
            IconRegion.FromCorners(60, 40, 10, 10));
    }

    [Fact]
    public void Everything_includes_every_icon()
    {
        DisplayFingerprint display = Display();

        Assert.True(RestoreScope.Everything.IsEverything);
        Assert.Null(RestoreScope.Everything.Resolve(display));
        Assert.True(RestoreScope.Everything.Includes(new IconEntry("A", 99999, 99999, 0), display));
    }

    [Fact]
    public void A_monitor_scope_admits_only_that_monitors_icons()
    {
        DisplayFingerprint display = Display();
        RestoreScope scope = RestoreScope.ForMonitor(display, Portrait);

        Assert.True(scope.Includes(new IconEntry("on the portrait", 13, 695, 0), display));
        Assert.False(scope.Includes(new IconEntry("on the primary", 1453, 1184, 1), display));
        Assert.False(scope.Includes(new IconEntry("above", 1700, 40, 2), display));
    }

    [Fact]
    public void A_monitor_whose_device_id_is_blank_still_scopes_to_that_monitor()
    {
        // Regression. Windows declines to report a device interface path in several situations, and
        // MonitorDescriptor.DeviceId is then empty. An earlier version resolved a monitor scope by
        // looking that id up, which made an empty one indistinguishable from "no scope at all" —
        // so clicking a single screen quietly restored the entire desktop.
        DisplayFingerprint display = Display();
        var anonymous = new MonitorDescriptor(@"\\.\DISPLAY9", string.Empty, -1440, -387, 1440, 2560, false, 96);

        RestoreScope scope = RestoreScope.ForMonitor(display, anonymous);

        Assert.False(scope.IsEverything);
        Assert.Equal(new IconRegion(0, 693, 1440, 2560), scope.Resolve(display));
        Assert.False(scope.Includes(new IconEntry("on the primary", 1453, 1184, 0), display));
    }

    [Fact]
    public void A_monitor_scope_keeps_its_region_even_if_the_monitor_list_changes()
    {
        // The region is resolved when the scope is made, so nothing can later reinterpret it.
        DisplayFingerprint display = Display();
        RestoreScope scope = RestoreScope.ForMonitor(display, Portrait);

        var reduced = new DisplayFingerprint
        {
            VirtualLeft = display.VirtualLeft,
            VirtualTop = display.VirtualTop,
            VirtualWidth = display.VirtualWidth,
            VirtualHeight = display.VirtualHeight,
            Monitors = new[] { Primary },
        };

        Assert.Equal(new IconRegion(0, 693, 1440, 2560), scope.Resolve(reduced));
    }

    [Fact]
    public void Regions_that_touch_but_do_not_overlap_do_not_intersect()
    {
        DisplayFingerprint display = Display();
        IconRegion portrait = IconRegion.ForMonitor(display, Portrait);
        IconRegion primary = IconRegion.ForMonitor(display, Primary);

        // The portrait monitor ends at x = 1440 and the primary starts there.
        Assert.False(portrait.IntersectsWith(new IconRegion(portrait.Right, portrait.Y, 100, 100)));
        Assert.True(portrait.IntersectsWith(primary with { X = primary.X - 1 }));
        Assert.True(primary.IntersectsWith(primary));
    }

    private static DisplayFingerprint Display() => new()
    {
        VirtualLeft = -1440,
        VirtualTop = -1080,
        VirtualWidth = 5280,
        VirtualHeight = 3253,
        Monitors = new[] { Primary, Portrait, Above },
    };
}
