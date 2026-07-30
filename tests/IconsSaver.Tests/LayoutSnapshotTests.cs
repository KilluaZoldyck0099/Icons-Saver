using IconsSaver.Core.Models;
using Xunit;

namespace IconsSaver.Tests;

public sealed class LayoutSnapshotTests
{
    [Fact]
    public void Create_copies_every_icon_with_its_position()
    {
        var icons = new[]
        {
            new DesktopIcon(0, "Recycle Bin", new IconPosition(1453, 1184)),
            new DesktopIcon(1, "Notes.txt", new IconPosition(13, 695)),
        };

        LayoutSnapshot snapshot = LayoutSnapshot.Create("evening", icons, new DisplayFingerprint(), default);

        Assert.Equal("evening", snapshot.Name);
        Assert.Equal(2, snapshot.IconCount);
        Assert.Equal("Recycle Bin", snapshot.Icons[0].Name);
        Assert.Equal(new IconPosition(1453, 1184), snapshot.Icons[0].Position);
        Assert.Equal(1, snapshot.Icons[1].Index);
    }

    [Fact]
    public void Every_snapshot_gets_its_own_id()
    {
        Assert.NotEqual(new LayoutSnapshot().Id, new LayoutSnapshot().Id);
    }

    [Fact]
    public void New_snapshots_carry_the_current_schema_version()
    {
        Assert.Equal(LayoutSnapshot.CurrentSchemaVersion, new LayoutSnapshot().SchemaVersion);
    }

    [Fact]
    public void An_unnamed_layout_falls_back_to_its_timestamp()
    {
        var snapshot = new LayoutSnapshot { Name = "   ", CapturedAt = new DateTimeOffset(2026, 7, 30, 19, 24, 5, TimeSpan.Zero) };

        Assert.Contains("2026-07-30", snapshot.DisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public void A_named_layout_uses_its_name()
    {
        Assert.Equal("before the update", new LayoutSnapshot { Name = "before the update" }.DisplayName);
    }

    [Fact]
    public void Automatic_snapshots_are_flagged()
    {
        LayoutSnapshot snapshot = LayoutSnapshot.Create(
            "auto",
            Array.Empty<DesktopIcon>(),
            new DisplayFingerprint(),
            default,
            isAutomatic: true);

        Assert.True(snapshot.IsAutomatic);
    }

    [Fact]
    public void FromDesktopIcon_preserves_the_index_for_tie_breaking()
    {
        IconEntry entry = IconEntry.FromDesktopIcon(new DesktopIcon(7, "A", new IconPosition(1, 2)));

        Assert.Equal(7, entry.Index);
        Assert.Equal(1, entry.X);
        Assert.Equal(2, entry.Y);
    }
}
