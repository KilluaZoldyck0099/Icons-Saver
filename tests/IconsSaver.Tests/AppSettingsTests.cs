using IconsSaver.Core.Storage;
using Xunit;

namespace IconsSaver.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void Defaults_are_immediately_useful()
    {
        var settings = new AppSettings();

        Assert.True(settings.AutoSaveEnabled);
        Assert.True(settings.AutoRestoreOnDisplayChange);
        Assert.True(settings.RestoreOnlyOnMatchingDisplays);
        Assert.True(settings.MinimizeToTray);
        Assert.True(settings.AutoRestoreDelaySeconds > 0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(30, 30)]
    [InlineData(99999, 1440)]
    public void Interval_is_clamped_to_something_the_timer_can_honour(int given, int expected)
    {
        var settings = new AppSettings { AutoSaveIntervalMinutes = given };

        settings.Normalize();

        Assert.Equal(expected, settings.AutoSaveIntervalMinutes);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(12, 12)]
    [InlineData(5000, 200)]
    public void History_count_is_clamped(int given, int expected)
    {
        var settings = new AppSettings { AutoSaveHistoryCount = given };

        settings.Normalize();

        Assert.Equal(expected, settings.AutoSaveHistoryCount);
    }

    [Fact]
    public void Restore_delay_is_clamped()
    {
        var settings = new AppSettings { AutoRestoreDelaySeconds = 9999 };

        settings.Normalize();

        Assert.Equal(120, settings.AutoRestoreDelaySeconds);
    }

    [Fact]
    public void Clone_is_independent_so_cancel_really_cancels()
    {
        var original = new AppSettings { AutoSaveIntervalMinutes = 30 };
        AppSettings copy = original.Clone();

        copy.AutoSaveIntervalMinutes = 5;

        Assert.Equal(30, original.AutoSaveIntervalMinutes);
    }

    [Fact]
    public void Settings_round_trip_through_the_store()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(folder.File("settings.json"));

        var settings = new AppSettings
        {
            AutoSaveIntervalMinutes = 17,
            SaveHotkey = "Ctrl+Shift+F9",
            ShowNotifications = false,
        };

        store.Save(settings);
        AppSettings loaded = store.Load();

        Assert.Equal(17, loaded.AutoSaveIntervalMinutes);
        Assert.Equal("Ctrl+Shift+F9", loaded.SaveHotkey);
        Assert.False(loaded.ShowNotifications);
    }

    [Fact]
    public void A_missing_settings_file_yields_defaults()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(folder.File("absent.json"));

        Assert.True(store.Load().AutoSaveEnabled);
    }

    [Fact]
    public void A_corrupt_settings_file_yields_defaults_instead_of_throwing()
    {
        using var folder = new TempFolder();
        string path = folder.File("settings.json");
        File.WriteAllText(path, "{ not json");

        var store = new SettingsStore(path);

        Assert.True(store.Load().AutoSaveEnabled);
    }
}
