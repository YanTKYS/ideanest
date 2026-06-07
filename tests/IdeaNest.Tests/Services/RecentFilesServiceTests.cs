using System.Collections.Generic;
using System.Linq;
using IdeaNest.Services;
using Xunit;

namespace IdeaNest.Tests.Services;

public class RecentFilesServiceTests
{
    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_NewPath_PrependsToList()
    {
        var current = new List<string> { @"C:\old\a.ideanest", @"C:\old\b.ideanest" };

        var result = RecentFilesService.Add(current, @"C:\new\x.ideanest");

        Assert.Equal(
            new[] { @"C:\new\x.ideanest", @"C:\old\a.ideanest", @"C:\old\b.ideanest" },
            result);
    }

    [Fact]
    public void Add_DuplicatePath_MovesToFront_WithoutDuplicating()
    {
        var current = new List<string>
        {
            @"C:\a.ideanest", @"C:\b.ideanest", @"C:\c.ideanest",
        };

        var result = RecentFilesService.Add(current, @"C:\b.ideanest");

        Assert.Equal(
            new[] { @"C:\b.ideanest", @"C:\a.ideanest", @"C:\c.ideanest" },
            result);
    }

    [Fact]
    public void Add_DuplicatePath_IsCaseInsensitive()
    {
        var current = new List<string> { @"C:\Project\Ideas.ideanest" };

        var result = RecentFilesService.Add(current, @"c:\project\ideas.ideanest");

        Assert.Single(result);
        Assert.Equal(@"c:\project\ideas.ideanest", result[0]);
    }

    [Fact]
    public void Add_ExceedsMax_TrimsToFiveEntries()
    {
        var current = new List<string>
        {
            @"C:\a.ideanest", @"C:\b.ideanest", @"C:\c.ideanest",
            @"C:\d.ideanest", @"C:\e.ideanest",
        };

        var result = RecentFilesService.Add(current, @"C:\new.ideanest");

        Assert.Equal(RecentFilesService.MaxRecentFiles, result.Count);
        Assert.Equal(@"C:\new.ideanest", result[0]);
        Assert.Equal(@"C:\d.ideanest", result[^1]);
        Assert.DoesNotContain(@"C:\e.ideanest", result);
    }

    [Fact]
    public void Add_EmptyPath_ReturnsCurrentUnchanged()
    {
        var current = new List<string> { @"C:\a.ideanest" };

        var result = RecentFilesService.Add(current, "");

        Assert.Equal(current, result);
    }

    [Fact]
    public void Add_WhitespacePath_ReturnsCurrentUnchanged()
    {
        var current = new List<string> { @"C:\a.ideanest" };

        var result = RecentFilesService.Add(current, "   ");

        Assert.Equal(current, result);
    }

    [Fact]
    public void Add_OnEmptyList_ReturnsSingletonList()
    {
        var result = RecentFilesService.Add(new List<string>(), @"C:\a.ideanest");

        Assert.Single(result);
        Assert.Equal(@"C:\a.ideanest", result[0]);
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Fact]
    public void Remove_RemovesMatchingEntry()
    {
        var current = new List<string> { @"C:\a.ideanest", @"C:\b.ideanest" };

        var result = RecentFilesService.Remove(current, @"C:\a.ideanest");

        Assert.Equal(new[] { @"C:\b.ideanest" }, result);
    }

    [Fact]
    public void Remove_IsCaseInsensitive()
    {
        var current = new List<string> { @"C:\Project\Ideas.ideanest" };

        var result = RecentFilesService.Remove(current, @"c:\PROJECT\ideas.ideanest");

        Assert.Empty(result);
    }

    [Fact]
    public void Remove_NoMatch_ReturnsCurrentUnchanged()
    {
        var current = new List<string> { @"C:\a.ideanest" };

        var result = RecentFilesService.Remove(current, @"C:\other.ideanest");

        Assert.Equal(current, result);
    }

    // ── FilterExisting ────────────────────────────────────────────────────────

    [Fact]
    public void FilterExisting_DropsMissingFiles()
    {
        var current = new List<string>
        {
            @"C:\exists.ideanest",
            @"C:\missing.ideanest",
            @"C:\also-exists.ideanest",
        };
        bool Exists(string p) => p.Contains("exists");

        var result = RecentFilesService.FilterExisting(current, Exists);

        Assert.Equal(
            new[] { @"C:\exists.ideanest", @"C:\also-exists.ideanest" },
            result);
    }

    [Fact]
    public void FilterExisting_AllMissing_ReturnsEmpty()
    {
        var current = new List<string> { @"C:\a.ideanest", @"C:\b.ideanest" };

        var result = RecentFilesService.FilterExisting(current, _ => false);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterExisting_AllPresent_PreservesOrder()
    {
        var current = new List<string> { @"C:\a", @"C:\b", @"C:\c" };

        var result = RecentFilesService.FilterExisting(current, _ => true);

        Assert.Equal(current, result);
    }

    [Fact]
    public void FilterExisting_DropsEmptyAndWhitespace()
    {
        var current = new List<string> { @"C:\a", "", "   ", @"C:\b" };

        var result = RecentFilesService.FilterExisting(current, _ => true);

        Assert.Equal(new[] { @"C:\a", @"C:\b" }, result);
    }
}
