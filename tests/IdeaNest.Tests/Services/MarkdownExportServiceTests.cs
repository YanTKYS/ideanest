using System;
using System.Collections.Generic;
using IdeaNest.Models;
using IdeaNest.Services;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.Services;

public class MarkdownExportServiceTests
{
    [Theory]
    [InlineData("yellow", "黄")]
    [InlineData("pink", "ピンク")]
    [InlineData("blue", "青")]
    [InlineData("green", "緑")]
    [InlineData("purple", "紫")]
    [InlineData("orange", "オレンジ")]
    [InlineData("gray", "グレー")]
    [InlineData("white", "白")]
    [InlineData("unknown", "unknown")]
    [InlineData("", "")]
    public void ColorDisplayName_MapsKnownColors_PassesThroughUnknown(string color, string expected)
    {
        Assert.Equal(expected, MarkdownExportService.ColorDisplayName(color));
    }

    [Fact]
    public void FormatCard_IncludesTitleBodyTagsColorAndFlags()
    {
        var card = MakeCard(title: "Hello", body: "first\nsecond", tags: new[] { "UI", "design" },
            color: "blue", pinned: true, archived: false);

        var md = MarkdownExportService.FormatCard(card);

        Assert.Contains("## Hello", md);
        Assert.Contains("first", md);
        Assert.Contains("second", md);
        Assert.Contains("Tags: #UI #design", md);
        Assert.Contains("Color: 青", md);
        Assert.Contains("Pinned: true", md);
        Assert.Contains("Archived: false", md);
    }

    [Fact]
    public void FormatCard_FallsBackToDisplayTitle_WhenTitleEmpty()
    {
        var card = MakeCard(title: "", body: "first body line\nsecond", tags: Array.Empty<string>());

        var md = MarkdownExportService.FormatCard(card);

        Assert.Contains("## first body line", md);
    }

    [Fact]
    public void FormatCard_OmitsTagsLine_WhenNoTags()
    {
        var card = MakeCard(title: "T", body: "b", tags: Array.Empty<string>());

        var md = MarkdownExportService.FormatCard(card);

        Assert.DoesNotContain("Tags:", md);
    }

    [Fact]
    public void FormatAll_IncludesHeaderCountAndDividers()
    {
        var cards = new List<IdeaCardViewModel>
        {
            MakeCard("A", "body A", Array.Empty<string>()),
            MakeCard("B", "body B", Array.Empty<string>()),
        };

        var md = MarkdownExportService.FormatAll(cards, searchText: "", selectedTag: "", selectedColor: "", showArchived: false);

        Assert.Contains("# IdeaNest エクスポート", md);
        Assert.Contains("出力件数: 2", md);
        Assert.Contains("## A", md);
        Assert.Contains("## B", md);
        // One divider ("---") per card — split on the normalized form to be
        // robust to \r\n (Windows AppendLine) vs \n (Linux).
        var normalized = md.Replace("\r\n", "\n");
        var dividerCount = normalized.Split("\n---\n", StringSplitOptions.None).Length - 1;
        Assert.Equal(2, dividerCount);
    }

    [Fact]
    public void FormatAll_RendersFilterContext_WhenFiltersAreSet()
    {
        var md = MarkdownExportService.FormatAll(
            new List<IdeaCardViewModel>(),
            searchText: "alpha",
            selectedTag: "UI",
            selectedColor: "blue",
            showArchived: true);

        Assert.Contains("検索条件: alpha", md);
        Assert.Contains("タグ: #UI", md);
        Assert.Contains("色: 青", md);
        Assert.Contains("アーカイブ表示: する", md);
    }

    [Fact]
    public void FormatAll_OmitsFilterLines_WhenFiltersAreEmpty()
    {
        var md = MarkdownExportService.FormatAll(
            new List<IdeaCardViewModel>(),
            searchText: "",
            selectedTag: "",
            selectedColor: "",
            showArchived: false);

        Assert.DoesNotContain("検索条件:", md);
        Assert.DoesNotContain("タグ:", md);
        Assert.DoesNotContain("色:", md);
        Assert.Contains("アーカイブ表示: しない", md);
    }

    private static IdeaCardViewModel MakeCard(
        string title,
        string body,
        IEnumerable<string> tags,
        string color = "yellow",
        bool pinned = false,
        bool archived = false)
    {
        return new IdeaCardViewModel(new Idea
        {
            Title = title,
            Body = body,
            Tags = new List<string>(tags),
            Color = color,
            IsPinned = pinned,
            IsArchived = archived,
        });
    }
}
