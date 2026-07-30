using IconsSaver.Core.Models;
using IconsSaver.Core.Storage;
using Xunit;

namespace IconsSaver.Tests;

public sealed class RestoreReportTests
{
    [Fact]
    public void Summary_reports_what_moved()
    {
        var report = new RestoreReport
        {
            DesktopIconCount = 10,
            SnapshotIconCount = 10,
            Matched = 10,
            Moved = 4,
            AlreadyInPlace = 6,
        };

        Assert.Contains("Moved 4 of 10", report.Summarize(), StringComparison.Ordinal);
        Assert.Contains("6 already in place", report.Summarize(), StringComparison.Ordinal);
    }

    [Fact]
    public void Summary_mentions_icons_that_have_gone_and_icons_that_are_new()
    {
        var report = new RestoreReport
        {
            DesktopIconCount = 9,
            SnapshotIconCount = 10,
            Matched = 8,
            Moved = 8,
            AlreadyInPlace = 0,
            Missing = new[] { "Old.txt", "Gone.png" },
            Unknown = new[] { "New download.zip" },
        };

        string summary = report.Summarize();

        Assert.Contains("2 no longer on the desktop", summary, StringComparison.Ordinal);
        Assert.Contains("1 new since the snapshot", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void A_report_with_no_gaps_is_complete()
    {
        var report = new RestoreReport
        {
            DesktopIconCount = 5,
            SnapshotIconCount = 5,
            Matched = 5,
            Moved = 5,
            AlreadyInPlace = 0,
        };

        Assert.True(report.IsComplete);
    }

    [Fact]
    public void A_report_with_missing_icons_is_not_complete()
    {
        var report = new RestoreReport
        {
            DesktopIconCount = 4,
            SnapshotIconCount = 5,
            Matched = 4,
            Moved = 4,
            AlreadyInPlace = 0,
            Missing = new[] { "Gone.txt" },
        };

        Assert.False(report.IsComplete);
    }

    [Fact]
    public void The_outcome_defaults_to_completed()
    {
        var report = new RestoreReport
        {
            DesktopIconCount = 0,
            SnapshotIconCount = 0,
            Matched = 0,
            Moved = 0,
            AlreadyInPlace = 0,
        };

        Assert.Equal(RestoreOutcome.Completed, report.Outcome);
    }

    [Theory]
    [InlineData("normal name", "normal name")]
    [InlineData("has/slash", "has_slash")]
    [InlineData("bad:chars*here?", "bad_chars_here_")]
    [InlineData("   ", "layout")]
    [InlineData("", "layout")]
    public void Export_file_names_are_sanitised(string given, string expected)
    {
        Assert.Equal(expected, AppPaths.SanitizeFileName(given));
    }
}
