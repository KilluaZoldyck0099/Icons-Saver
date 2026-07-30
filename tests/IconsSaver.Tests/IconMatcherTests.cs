using IconsSaver.Core.Models;
using IconsSaver.Core.Services;
using Xunit;

namespace IconsSaver.Tests;

public sealed class IconMatcherTests
{
    [Fact]
    public void Matches_by_name_even_when_every_index_shifted()
    {
        // A single new file on the desktop renumbers everything after it. Matching must survive it.
        var entries = new[]
        {
            new IconEntry("Recycle Bin", 10, 10, 0),
            new IconEntry("Notes.txt", 10, 120, 1),
        };

        var live = new[]
        {
            new DesktopIcon(0, "Brand new download.zip", new IconPosition(0, 0)),
            new DesktopIcon(1, "Recycle Bin", new IconPosition(500, 500)),
            new DesktopIcon(2, "Notes.txt", new IconPosition(600, 600)),
        };

        IconMatchResult result = IconMatcher.Match(entries, live);

        Assert.Equal(2, result.Matches.Count);
        Assert.Empty(result.Missing);
        Assert.Single(result.Unknown);
        Assert.Equal("Brand new download.zip", result.Unknown[0].Name);
    }

    [Fact]
    public void Reports_entries_whose_icon_is_gone()
    {
        var entries = new[] { new IconEntry("Deleted.txt", 10, 10, 0) };
        var live = Array.Empty<DesktopIcon>();

        IconMatchResult result = IconMatcher.Match(entries, live);

        Assert.Empty(result.Matches);
        Assert.Single(result.Missing);
        Assert.Equal("Deleted.txt", result.Missing[0].Name);
    }

    [Fact]
    public void Exact_case_wins_over_a_case_insensitive_candidate()
    {
        // The desktop merges the user's Desktop folder with the shared public one, so two items
        // really can differ only in case.
        var entries = new[] { new IconEntry("readme.txt", 10, 10, 5) };

        var live = new[]
        {
            new DesktopIcon(0, "README.TXT", new IconPosition(0, 0)),
            new DesktopIcon(1, "readme.txt", new IconPosition(100, 100)),
        };

        IconMatchResult result = IconMatcher.Match(entries, live);

        Assert.Single(result.Matches);
        Assert.Equal(1, result.Matches[0].Icon.Index);
    }

    [Fact]
    public void Falls_back_to_a_case_insensitive_match()
    {
        var entries = new[] { new IconEntry("Readme.txt", 10, 10, 0) };
        var live = new[] { new DesktopIcon(0, "README.TXT", new IconPosition(0, 0)) };

        IconMatchResult result = IconMatcher.Match(entries, live);

        Assert.Single(result.Matches);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void Duplicate_names_are_paired_by_nearest_index()
    {
        var entries = new[]
        {
            new IconEntry("Shortcut", 10, 10, 0),
            new IconEntry("Shortcut", 10, 200, 9),
        };

        var live = new[]
        {
            new DesktopIcon(0, "Shortcut", new IconPosition(0, 0)),
            new DesktopIcon(9, "Shortcut", new IconPosition(0, 0)),
        };

        IconMatchResult result = IconMatcher.Match(entries, live);

        Assert.Equal(2, result.Matches.Count);
        Assert.Equal(0, result.Matches[0].Icon.Index);
        Assert.Equal(9, result.Matches[1].Icon.Index);
    }

    [Fact]
    public void Never_pairs_one_live_icon_with_two_entries()
    {
        var entries = new[]
        {
            new IconEntry("Shortcut", 10, 10, 0),
            new IconEntry("Shortcut", 10, 200, 1),
        };

        var live = new[] { new DesktopIcon(0, "Shortcut", new IconPosition(0, 0)) };

        IconMatchResult result = IconMatcher.Match(entries, live);

        Assert.Single(result.Matches);
        Assert.Single(result.Missing);
        Assert.Empty(result.Unknown);
    }

    [Fact]
    public void Unnamed_entries_are_never_matched()
    {
        var entries = new[] { new IconEntry(string.Empty, 10, 10, 0) };
        var live = new[] { new DesktopIcon(0, string.Empty, new IconPosition(0, 0)) };

        IconMatchResult result = IconMatcher.Match(entries, live);

        Assert.Empty(result.Matches);
        Assert.Single(result.Missing);
    }

    [Fact]
    public void IsAlreadyInPlace_compares_positions()
    {
        var entry = new IconEntry("A", 10, 20, 0);

        Assert.True(new IconMatch(entry, new DesktopIcon(0, "A", new IconPosition(10, 20))).IsAlreadyInPlace);
        Assert.False(new IconMatch(entry, new DesktopIcon(0, "A", new IconPosition(11, 20))).IsAlreadyInPlace);
    }
}
