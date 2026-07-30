using IconsSaver.Core.Models;
using IconsSaver.Core.Storage;
using Xunit;

namespace IconsSaver.Tests;

public sealed class SnapshotStoreTests
{
    [Fact]
    public void Saves_and_loads_a_layout_unchanged()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        LayoutSnapshot original = Sample();
        store.Save(original);

        LayoutSnapshot? loaded = store.Load(original.Id);

        Assert.NotNull(loaded);
        Assert.Equal(original.Name, loaded!.Name);
        Assert.Equal(original.IconCount, loaded.IconCount);
        Assert.Equal(original.Icons[0].Name, loaded.Icons[0].Name);
        Assert.Equal(original.Icons[0].Position, loaded.Icons[0].Position);
        Assert.Equal(original.Display.Signature, loaded.Display.Signature);
        Assert.Equal(original.ViewOptions, loaded.ViewOptions);
    }

    [Fact]
    public void Non_ascii_names_survive_the_round_trip()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        var snapshot = new LayoutSnapshot
        {
            Name = "Γραφείο",
            Display = Sample().Display,
            Icons = new[] { new IconEntry("οπεκα.txt", 13, 1205, 0), new IconEntry("日本語.png", 90, 20, 1) },
        };

        store.Save(snapshot);

        LayoutSnapshot? loaded = store.Load(snapshot.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Γραφείο", loaded!.Name);
        Assert.Equal("οπεκα.txt", loaded.Icons[0].Name);
        Assert.Equal("日本語.png", loaded.Icons[1].Name);

        // Readability is the point of the relaxed encoder, so check the file itself.
        Assert.Contains("οπεκα.txt", File.ReadAllText(store.PathFor(snapshot.Id)), StringComparison.Ordinal);
    }

    [Fact]
    public void LoadAll_returns_newest_first()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        store.Save(new LayoutSnapshot { Name = "older", CapturedAt = DateTimeOffset.Now.AddHours(-2) });
        store.Save(new LayoutSnapshot { Name = "newer", CapturedAt = DateTimeOffset.Now });

        IReadOnlyList<LayoutSnapshot> all = store.LoadAll();

        Assert.Equal(2, all.Count);
        Assert.Equal("newer", all[0].Name);
    }

    [Fact]
    public void A_corrupt_file_is_skipped_rather_than_fatal()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        store.Save(new LayoutSnapshot { Name = "good" });
        File.WriteAllText(folder.File("broken.json"), "{ this is not json");

        IReadOnlyList<LayoutSnapshot> all = store.LoadAll();

        Assert.Single(all);
        Assert.Equal("good", all[0].Name);
    }

    [Fact]
    public void A_layout_from_a_newer_schema_is_ignored()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        store.Save(new LayoutSnapshot { Name = "from the future", SchemaVersion = LayoutSnapshot.CurrentSchemaVersion + 1 });

        Assert.Empty(store.LoadAll());
    }

    [Fact]
    public void Delete_removes_the_file_and_reports_whether_it_existed()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        LayoutSnapshot snapshot = Sample();
        store.Save(snapshot);

        Assert.True(store.Delete(snapshot.Id));
        Assert.False(store.Delete(snapshot.Id));
        Assert.Null(store.Load(snapshot.Id));
    }

    [Fact]
    public void Loading_an_unknown_id_returns_null()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        Assert.Null(store.Load("does-not-exist"));
    }

    [Fact]
    public void Import_gives_the_layout_a_fresh_id()
    {
        using var folder = new TempFolder();

        LayoutSnapshot original = Sample();
        string path = folder.File("exported.json");
        SnapshotStore.Export(original, path);

        LayoutSnapshot imported = SnapshotStore.Import(path);

        Assert.NotEqual(original.Id, imported.Id);
        Assert.Equal(original.Name, imported.Name);
        Assert.Equal(original.IconCount, imported.IconCount);
        Assert.False(imported.IsAutomatic);
    }

    [Fact]
    public void Importing_something_that_is_not_a_layout_throws()
    {
        using var folder = new TempFolder();
        string path = folder.File("nonsense.json");
        File.WriteAllText(path, "not json at all");

        Assert.Throws<InvalidDataException>(() => SnapshotStore.Import(path));
    }

    private static LayoutSnapshot Sample() => new()
    {
        Name = "sample",
        ViewOptions = new DesktopViewOptions(AutoArrange: false, SnapToGrid: true),
        Display = new DisplayFingerprint
        {
            VirtualLeft = -1440,
            VirtualTop = -1080,
            VirtualWidth = 5280,
            VirtualHeight = 3253,
            Monitors = new[] { new MonitorDescriptor(@"\\.\DISPLAY1", "DISPLAY#AAA", 0, 0, 3840, 2160, true, 96) },
        },
        Icons = new[]
        {
            new IconEntry("Recycle Bin", 1453, 1184, 0),
            new IconEntry("Notes.txt", 13, 695, 1),
        },
    };
}
