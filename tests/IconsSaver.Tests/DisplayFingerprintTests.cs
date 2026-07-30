using IconsSaver.Core.Models;
using Xunit;

namespace IconsSaver.Tests;

public sealed class DisplayFingerprintTests
{
    [Fact]
    public void The_same_arrangement_produces_the_same_signature()
    {
        Assert.Equal(TwoMonitors().Signature, TwoMonitors().Signature);
    }

    [Fact]
    public void Monitor_order_does_not_change_the_signature()
    {
        DisplayFingerprint forwards = TwoMonitors();
        DisplayFingerprint backwards = new()
        {
            VirtualLeft = forwards.VirtualLeft,
            VirtualTop = forwards.VirtualTop,
            VirtualWidth = forwards.VirtualWidth,
            VirtualHeight = forwards.VirtualHeight,
            Monitors = forwards.Monitors.Reverse().ToList(),
        };

        Assert.Equal(forwards.Signature, backwards.Signature);
    }

    [Fact]
    public void Unplugging_a_monitor_changes_the_signature()
    {
        DisplayFingerprint two = TwoMonitors();
        DisplayFingerprint one = new()
        {
            VirtualLeft = 0,
            VirtualTop = 0,
            VirtualWidth = 1920,
            VirtualHeight = 1080,
            Monitors = new[] { two.Monitors[0] },
        };

        Assert.NotEqual(two.Signature, one.Signature);
        Assert.False(two.Matches(one));
    }

    [Fact]
    public void Changing_only_the_scaling_changes_the_signature()
    {
        DisplayFingerprint normal = TwoMonitors();
        DisplayFingerprint scaled = new()
        {
            VirtualLeft = normal.VirtualLeft,
            VirtualTop = normal.VirtualTop,
            VirtualWidth = normal.VirtualWidth,
            VirtualHeight = normal.VirtualHeight,
            Monitors = normal.Monitors.Select(m => m with { Dpi = 144 }).ToList(),
        };

        Assert.NotEqual(normal.Signature, scaled.Signature);
    }

    [Fact]
    public void Geometry_matching_ignores_monitor_identity()
    {
        // Swapping a monitor for an identical model changes its device id but leaves every icon
        // position perfectly valid.
        DisplayFingerprint original = TwoMonitors();
        DisplayFingerprint replaced = new()
        {
            VirtualLeft = original.VirtualLeft,
            VirtualTop = original.VirtualTop,
            VirtualWidth = original.VirtualWidth,
            VirtualHeight = original.VirtualHeight,
            Monitors = original.Monitors.Select(m => m with { DeviceId = m.DeviceId + "-replacement" }).ToList(),
        };

        Assert.False(original.Matches(replaced));
        Assert.True(original.MatchesGeometry(replaced));
    }

    [Fact]
    public void Signature_is_short_and_hex()
    {
        string signature = TwoMonitors().Signature;

        Assert.Equal(16, signature.Length);
        Assert.All(signature, c => Assert.Contains(c, "0123456789abcdef"));
    }

    [Fact]
    public void Matches_rejects_null()
    {
        Assert.False(TwoMonitors().Matches(null));
    }

    [Fact]
    public void Describe_reads_naturally()
    {
        Assert.Equal("2 monitors, 3840x1080", TwoMonitors().Describe());
    }

    private static DisplayFingerprint TwoMonitors() => new()
    {
        VirtualLeft = 0,
        VirtualTop = 0,
        VirtualWidth = 3840,
        VirtualHeight = 1080,
        Monitors = new[]
        {
            new MonitorDescriptor(@"\\.\DISPLAY1", "DISPLAY#AAA111", 0, 0, 1920, 1080, true, 96),
            new MonitorDescriptor(@"\\.\DISPLAY2", "DISPLAY#BBB222", 1920, 0, 1920, 1080, false, 96),
        },
    };
}
