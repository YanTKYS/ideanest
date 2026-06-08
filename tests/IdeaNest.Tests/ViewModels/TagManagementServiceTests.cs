using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IdeaNest.Models;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class TagManagementServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class Counters
    {
        public int Dirty;
        public int Tags;
        public int Visible;
    }

    private sealed class Harness
    {
        public ObservableCollection<IdeaCardViewModel> AllCards { get; } = new();
        public string SelectedTag { get; set; } = string.Empty;
        public Counters Counters { get; } = new();
        public TagManagementService Service { get; }

        public Harness()
        {
            Service = new TagManagementService(
                AllCards,
                getSelectedTag: () => SelectedTag,
                setSelectedTag: t => SelectedTag = t,
                onDirty:          () => Counters.Dirty++,
                onRefreshTags:    () => Counters.Tags++,
                onRefreshVisible: () => Counters.Visible++);
        }

        public IdeaCardViewModel AddCard(params string[] tags)
        {
            var idea = new Idea();
            idea.Tags.AddRange(tags);
            var card = new IdeaCardViewModel(idea);
            AllCards.Add(card);
            return card;
        }
    }

    // ── RenameTag ─────────────────────────────────────────────────────────────

    [Fact]
    public void RenameTag_UpdatesAllCardsCarryingTheOldName()
    {
        var h = new Harness();
        h.AddCard("old", "other");
        h.AddCard("untouched");
        h.AddCard("old");

        var changed = h.Service.RenameTag("old", "new");

        Assert.True(changed);
        Assert.Equal(new[] { "new", "other" }, h.AllCards[0].Tags);
        Assert.Equal(new[] { "untouched" },    h.AllCards[1].Tags);
        Assert.Equal(new[] { "new" },          h.AllCards[2].Tags);
    }

    [Fact]
    public void RenameTag_OnCardCarryingBoth_CollapsesIntoMergedTag()
    {
        var h = new Harness();
        var card = h.AddCard("old", "new", "extra");

        h.Service.RenameTag("old", "new");

        // Duplicate is collapsed by NormalizeTags so the merged card has no duplicate.
        Assert.Equal(2, card.Tags.Count);
        Assert.Contains("new", card.Tags);
        Assert.Contains("extra", card.Tags);
    }

    [Fact]
    public void RenameTag_MergePath_CardsKeepOnlyTheMergedTag()
    {
        // Two cards: one carries "old", one carries "new". After rename both end
        // up with "new" — the merge is achieved without a dedicated code path.
        var h = new Harness();
        h.AddCard("old");
        h.AddCard("new");

        h.Service.RenameTag("old", "new");

        Assert.Equal(new[] { "new" }, h.AllCards[0].Tags);
        Assert.Equal(new[] { "new" }, h.AllCards[1].Tags);
    }

    [Fact]
    public void RenameTag_NormalisesLeadingHash()
    {
        var h = new Harness();
        h.AddCard("old");

        h.Service.RenameTag("old", "#fresh");

        Assert.Equal(new[] { "fresh" }, h.AllCards[0].Tags);
    }

    [Fact]
    public void RenameTag_EmptyOrWhitespaceNewName_NoOpAndNoCallbacks()
    {
        var h = new Harness();
        h.AddCard("old");

        var changed = h.Service.RenameTag("old", "   ");

        Assert.False(changed);
        Assert.Equal(new[] { "old" }, h.AllCards[0].Tags);
        Assert.Equal(0, h.Counters.Dirty);
        Assert.Equal(0, h.Counters.Tags);
        Assert.Equal(0, h.Counters.Visible);
    }

    [Fact]
    public void RenameTag_SameNormalisedName_NoOpAndNoCallbacks()
    {
        var h = new Harness();
        h.AddCard("same");

        var changed = h.Service.RenameTag("same", "#same");

        Assert.False(changed);
        Assert.Equal(0, h.Counters.Dirty);
    }

    [Fact]
    public void RenameTag_InvokesDirtyRefreshTagsAndRefreshVisible_EachOnce()
    {
        var h = new Harness();
        h.AddCard("old");

        h.Service.RenameTag("old", "new");

        Assert.Equal(1, h.Counters.Dirty);
        Assert.Equal(1, h.Counters.Tags);
        Assert.Equal(1, h.Counters.Visible);
    }

    [Fact]
    public void RenameTag_WhenSelectedTagMatches_SelectionFollowsToNewName()
    {
        var h = new Harness { SelectedTag = "old" };
        h.AddCard("old");

        h.Service.RenameTag("old", "new");

        Assert.Equal("new", h.SelectedTag);
    }

    [Fact]
    public void RenameTag_WhenSelectedTagDoesNotMatch_SelectionUnchanged()
    {
        var h = new Harness { SelectedTag = "other" };
        h.AddCard("old");

        h.Service.RenameTag("old", "new");

        Assert.Equal("other", h.SelectedTag);
    }

    [Fact]
    public void RenameTag_BumpsUpdatedAt_OnTouchedCardsOnly()
    {
        var h = new Harness();
        var touched   = h.AddCard("old");
        var untouched = h.AddCard("keep");
        var before = System.DateTime.Now;
        untouched.Model.UpdatedAt = new System.DateTime(2020, 1, 1);

        h.Service.RenameTag("old", "new");

        Assert.True(touched.UpdatedAt >= before);
        Assert.Equal(new System.DateTime(2020, 1, 1), untouched.UpdatedAt);
    }

    [Fact]
    public void RenameTag_OldTagAbsentFromCardsAndSelection_ReturnsFalseAndSkipsCallbacks()
    {
        var h = new Harness { SelectedTag = "something-else" };
        h.AddCard("keep");

        var changed = h.Service.RenameTag("never-existed", "new");

        Assert.False(changed);
        Assert.Equal(new[] { "keep" }, h.AllCards[0].Tags);
        Assert.Equal("something-else", h.SelectedTag);
        Assert.Equal(0, h.Counters.Dirty);
        Assert.Equal(0, h.Counters.Tags);
        Assert.Equal(0, h.Counters.Visible);
    }

    [Fact]
    public void RenameTag_OldTagAbsentFromCardsButMatchesSelection_ReturnsTrueAndFollows()
    {
        // Changing the selected-tag filter is itself a mutation even when no card carries oldName.
        var h = new Harness { SelectedTag = "old" };
        h.AddCard("keep");

        var changed = h.Service.RenameTag("old", "new");

        Assert.True(changed);
        Assert.Equal("new", h.SelectedTag);
        Assert.Equal(1, h.Counters.Dirty);
    }

    // ── DeleteTag ─────────────────────────────────────────────────────────────

    [Fact]
    public void DeleteTag_RemovesTagFromAllCardsButLeavesCards()
    {
        var h = new Harness();
        h.AddCard("doomed", "keep");
        h.AddCard("untouched");
        h.AddCard("doomed");

        h.Service.DeleteTag("doomed");

        Assert.Equal(3, h.AllCards.Count); // cards survive
        Assert.Equal(new[] { "keep" },      h.AllCards[0].Tags);
        Assert.Equal(new[] { "untouched" }, h.AllCards[1].Tags);
        Assert.Empty(h.AllCards[2].Tags);
    }

    [Fact]
    public void DeleteTag_WhenSelectedTagMatches_SelectionIsCleared()
    {
        var h = new Harness { SelectedTag = "doomed" };
        h.AddCard("doomed");

        h.Service.DeleteTag("doomed");

        Assert.Equal(string.Empty, h.SelectedTag);
    }

    [Fact]
    public void DeleteTag_WhenSelectedTagDoesNotMatch_SelectionUnchanged()
    {
        var h = new Harness { SelectedTag = "other" };
        h.AddCard("doomed");

        h.Service.DeleteTag("doomed");

        Assert.Equal("other", h.SelectedTag);
    }

    [Fact]
    public void DeleteTag_InvokesDirtyRefreshTagsAndRefreshVisible_EachOnce()
    {
        var h = new Harness();
        h.AddCard("doomed");

        h.Service.DeleteTag("doomed");

        Assert.Equal(1, h.Counters.Dirty);
        Assert.Equal(1, h.Counters.Tags);
        Assert.Equal(1, h.Counters.Visible);
    }

    [Fact]
    public void DeleteTag_BumpsUpdatedAt_OnTouchedCardsOnly()
    {
        var h = new Harness();
        var touched   = h.AddCard("doomed");
        var untouched = h.AddCard("keep");
        var before = System.DateTime.Now;
        untouched.Model.UpdatedAt = new System.DateTime(2020, 1, 1);

        h.Service.DeleteTag("doomed");

        Assert.True(touched.UpdatedAt >= before);
        Assert.Equal(new System.DateTime(2020, 1, 1), untouched.UpdatedAt);
    }

    [Fact]
    public void DeleteTag_TagAbsent_StillFiresCallbacks()
    {
        // Matches the v0.8.8 behaviour: DeleteTag unconditionally fires callbacks.
        // The cost is one extra empty refresh; the benefit is that callers
        // (e.g. the dialog code-behind) don't have to predict whether anything matched.
        var h = new Harness();
        h.AddCard("keep");

        h.Service.DeleteTag("never-existed");

        Assert.Equal(1, h.Counters.Dirty);
        Assert.Equal(1, h.Counters.Tags);
        Assert.Equal(1, h.Counters.Visible);
        Assert.Equal(new[] { "keep" }, h.AllCards[0].Tags);
    }

    // ── Case sensitivity (Ordinal) ────────────────────────────────────────────

    [Fact]
    public void RenameTag_IsCaseSensitive_LowerDoesNotMatchUpper()
    {
        var h = new Harness();
        h.AddCard("UI");
        h.AddCard("ui");

        h.Service.RenameTag("ui", "fresh");

        Assert.Equal(new[] { "UI" },    h.AllCards[0].Tags);
        Assert.Equal(new[] { "fresh" }, h.AllCards[1].Tags);
    }

    [Fact]
    public void DeleteTag_IsCaseSensitive_LowerDoesNotMatchUpper()
    {
        var h = new Harness();
        h.AddCard("UI");
        h.AddCard("ui");

        h.Service.DeleteTag("ui");

        Assert.Equal(new[] { "UI" }, h.AllCards[0].Tags);
        Assert.Empty(h.AllCards[1].Tags);
    }
}
