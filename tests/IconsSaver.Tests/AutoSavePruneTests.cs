using IconsSaver.Core.Models;
using IconsSaver.Core.Services;
using IconsSaver.Core.Storage;
using Xunit;

namespace IconsSaver.Tests;

/// <summary>
/// Locking a layout is a promise that the automatic history will not eat it, so the pruner is
/// worth testing directly rather than trusting by inspection.
/// </summary>
public sealed class AutoSavePruneTests
{
    [Fact]
    public void Prune_keeps_only_the_newest_unlocked_snapshots()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        for (int age = 0; age < 5; age++)
        {
            store.Save(Snapshot($"auto {age}", age, locked: false));
        }

        using AutoSaveService service = Service(store, historyCount: 2);
        int deleted = service.Prune();

        List<string> remaining = store.LoadAll().Select(s => s.Name).ToList();

        Assert.Equal(3, deleted);
        Assert.Equal(new[] { "auto 0", "auto 1" }, remaining);
    }

    [Fact]
    public void Prune_never_deletes_a_locked_snapshot_however_old_it_is()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        store.Save(Snapshot("recent", 0, locked: false));
        store.Save(Snapshot("also recent", 1, locked: false));
        store.Save(Snapshot("ancient but locked", 90, locked: true));
        store.Save(Snapshot("ancient and not locked", 91, locked: false));

        using AutoSaveService service = Service(store, historyCount: 2);
        service.Prune();

        List<string> remaining = store.LoadAll().Select(s => s.Name).ToList();

        Assert.Contains("ancient but locked", remaining);
        Assert.DoesNotContain("ancient and not locked", remaining);
    }

    [Fact]
    public void Locked_snapshots_do_not_count_against_the_history_limit()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        store.Save(Snapshot("locked one", 10, locked: true));
        store.Save(Snapshot("locked two", 11, locked: true));
        store.Save(Snapshot("new one", 0, locked: false));
        store.Save(Snapshot("new two", 1, locked: false));

        using AutoSaveService service = Service(store, historyCount: 2);
        service.Prune();

        // Two locked plus the two most recent unlocked: keeping a layout must not cost the user
        // room in their rolling history.
        Assert.Equal(4, store.LoadAll().Count);
    }

    [Fact]
    public void A_history_count_of_zero_is_treated_as_one()
    {
        using var folder = new TempFolder();
        var store = new SnapshotStore(folder.Path);

        store.Save(Snapshot("newest", 0, locked: false));
        store.Save(Snapshot("older", 1, locked: false));

        using AutoSaveService service = Service(store, historyCount: 0);
        service.Prune();

        Assert.Single(store.LoadAll());
    }

    private static AutoSaveService Service(SnapshotStore store, int historyCount) =>
        new(new LayoutService(store), store) { HistoryCount = historyCount };

    private static LayoutSnapshot Snapshot(string name, int ageInDays, bool locked) => new()
    {
        Name = name,
        CapturedAt = DateTimeOffset.Now.AddDays(-ageInDays),
        IsPinned = locked,
        IsAutomatic = true,
        Icons = new[] { new IconEntry("An icon", 10, 10, 0) },
    };
}
