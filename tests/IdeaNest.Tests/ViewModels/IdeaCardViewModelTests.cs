using System;
using IdeaNest.Models;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class IdeaCardViewModelTests
{
    [Fact]
    public void DisplayTitle_ReturnsTitle_WhenTitleIsSet()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "Hello", Body = "ignored body" });
        Assert.Equal("Hello", card.DisplayTitle);
    }

    [Fact]
    public void DisplayTitle_FallsBackToFirstBodyLine_WhenTitleEmpty()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "", Body = "first line\nsecond line" });
        Assert.Equal("first line", card.DisplayTitle);
    }

    [Fact]
    public void DisplayTitle_TruncatesLongFirstLine_WithEllipsis()
    {
        var longLine = new string('a', 60);
        var card = new IdeaCardViewModel(new Idea { Title = "", Body = longLine });

        var displayed = card.DisplayTitle;

        Assert.Equal(43, displayed.Length); // 40 chars + "..."
        Assert.EndsWith("...", displayed);
        Assert.StartsWith(new string('a', 40), displayed);
    }

    [Fact]
    public void DisplayTitle_Returns_Untitled_WhenTitleAndBodyEmpty()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "", Body = "" });
        Assert.Equal("(無題)", card.DisplayTitle);
    }

    [Fact]
    public void BodyPreview_LimitsToFirstFourLines()
    {
        var card = new IdeaCardViewModel(new Idea { Body = "1\n2\n3\n4\n5\n6" });

        var preview = card.BodyPreview;

        Assert.Equal("1\n2\n3\n4", preview);
    }

    [Fact]
    public void BodyPreview_TruncatesAt200Chars_WithEllipsis()
    {
        var line = new string('x', 80);
        var body = string.Join('\n', line, line, line); // ~242 chars across 3 lines

        var card = new IdeaCardViewModel(new Idea { Body = body });

        var preview = card.BodyPreview;

        Assert.EndsWith("...", preview);
        Assert.Equal(203, preview.Length); // 200 + "..."
    }

    [Fact]
    public void TagsText_PrefixesEachTagWithHash()
    {
        var card = new IdeaCardViewModel(new Idea { Tags = { "UI", "design" } });
        Assert.Equal("#UI #design", card.TagsText);
    }

    [Fact]
    public void TagsText_IsEmpty_WhenNoTags()
    {
        var card = new IdeaCardViewModel(new Idea());
        Assert.Equal(string.Empty, card.TagsText);
    }

    [Theory]
    [InlineData("yellow", "#FFF7CC")]
    [InlineData("pink",   "#FCE7F3")]
    [InlineData("blue",   "#DBEAFE")]
    [InlineData("green",  "#DCFCE7")]
    [InlineData("purple", "#EDE9FE")]
    [InlineData("orange", "#FFEDD5")]
    [InlineData("gray",   "#F1F3F5")]
    [InlineData("white",  "#FFFFFF")]
    [InlineData("not-a-real-color", "#FFFFFF")]
    public void BackgroundBrush_MapsColorNameToHex(string color, string expected)
    {
        var card = new IdeaCardViewModel(new Idea { Color = color });
        Assert.Equal(expected, card.BackgroundBrush);
    }

    [Fact]
    public void Touch_UpdatesUpdatedAt()
    {
        var card = new IdeaCardViewModel(new Idea { UpdatedAt = new DateTime(2020, 1, 1) });

        card.Touch();

        Assert.True(card.UpdatedAt > new DateTime(2020, 1, 1));
    }

    [Fact]
    public void SetTitle_RaisesPropertyChanged_ForTitleAndDisplayTitle()
    {
        var card = new IdeaCardViewModel(new Idea());
        var raised = new System.Collections.Generic.List<string>();
        card.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? "");

        card.Title = "New";

        Assert.Contains(nameof(IdeaCardViewModel.Title), raised);
        Assert.Contains(nameof(IdeaCardViewModel.DisplayTitle), raised);
    }
}
