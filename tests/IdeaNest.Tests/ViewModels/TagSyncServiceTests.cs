using System.Collections.Generic;
using System.Linq;
using IdeaNest.Models;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class TagSyncServiceTests
{
    private static IdeaCardViewModel Card(params string[] tags)
    {
        var idea = new Idea();
        idea.Tags.AddRange(tags);
        return new IdeaCardViewModel(idea);
    }

    [Fact]
    public void ComputeTagItems_NoCards_ReturnsEmpty()
    {
        var result = TagSyncService.ComputeTagItems(new List<IdeaCardViewModel>());
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeTagItems_SingleTag_ReturnsOneItemWithCountOne()
    {
        var result = TagSyncService.ComputeTagItems(new[] { Card("alpha") });

        Assert.Single(result);
        Assert.Equal("alpha", result[0].Name);
        Assert.Equal(1, result[0].Count);
    }

    [Fact]
    public void ComputeTagItems_SharedTag_AggregatesCount()
    {
        var cards = new[] { Card("shared"), Card("shared"), Card("shared") };
        var result = TagSyncService.ComputeTagItems(cards);

        Assert.Single(result);
        Assert.Equal(3, result[0].Count);
    }

    [Fact]
    public void ComputeTagItems_MultipleTags_SortedAlphabetically()
    {
        var cards = new[] { Card("zeta", "alpha"), Card("beta") };
        var result = TagSyncService.ComputeTagItems(cards);

        Assert.Equal(new[] { "alpha", "beta", "zeta" }, result.Select(t => t.Name));
    }

    [Fact]
    public void ComputeTagItems_BlankAndWhitespaceTags_AreIgnored()
    {
        var result = TagSyncService.ComputeTagItems(new[] { Card("", "  ", "valid") });

        Assert.Single(result);
        Assert.Equal("valid", result[0].Name);
    }

    [Fact]
    public void ComputeTagItems_TagsAreCaseSensitive_DistinctItems()
    {
        var cards = new[] { Card("Tag"), Card("tag"), Card("TAG") };
        var result = TagSyncService.ComputeTagItems(cards);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, t => t.Name == "Tag");
        Assert.Contains(result, t => t.Name == "tag");
        Assert.Contains(result, t => t.Name == "TAG");
    }
}
