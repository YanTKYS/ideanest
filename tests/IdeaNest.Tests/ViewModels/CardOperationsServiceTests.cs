using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdeaNest.Models;
using IdeaNest.Services;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class CardOperationsServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly DateTime FixedNow = new(2024, 6, 1, 12, 0, 0);

    private sealed class Counters
    {
        public int Dirty;
        public int Tags;
        public int Visible;
    }

    private static CardOperationsService MakeSvc(
        out Counters counters,
        List<Idea>? ideas = null,
        ObservableCollection<IdeaCardViewModel>? allCards = null,
        Func<DateTime>? now = null)
    {
        ideas    ??= new List<Idea>();
        allCards ??= new ObservableCollection<IdeaCardViewModel>();
        var c = new Counters();
        counters = c;
        return new CardOperationsService(
            ideas, allCards,
            onDirty:          () => c.Dirty++,
            onRefreshTags:    () => c.Tags++,
            onRefreshVisible: () => c.Visible++,
            now:              now ?? (() => FixedNow));
    }

    private static CardOperationsService MakeSvc(
        List<Idea>? ideas = null,
        ObservableCollection<IdeaCardViewModel>? allCards = null,
        Func<DateTime>? now = null)
        => MakeSvc(out _, ideas, allCards, now);

    private static IdeaCardViewModel MakeCard(
        string id = "id1", bool pinned = false, bool archived = false)
    {
        var idea = new Idea { Id = id, IsPinned = pinned, IsArchived = archived };
        return new IdeaCardViewModel(idea);
    }

    // ── CommitAdd ─────────────────────────────────────────────────────────────

    [Fact]
    public void CommitAdd_BothTitleAndBodyEmpty_ReturnsFalseAndDoesNotAdd()
    {
        var ideas    = new List<Idea>();
        var allCards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeSvc(out var c, ideas: ideas, allCards: allCards);

        var result = svc.CommitAdd(new Idea { Title = "  ", Body = "" });

        Assert.False(result);
        Assert.Empty(ideas);
        Assert.Empty(allCards);
        Assert.Equal(0, c.Dirty);
    }

    [Fact]
    public void CommitAdd_TitleOnly_ReturnsTrueAndAddsToBothCollections()
    {
        var ideas    = new List<Idea>();
        var allCards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeSvc(ideas: ideas, allCards: allCards);

        var result = svc.CommitAdd(new Idea { Title = "Hello" });

        Assert.True(result);
        Assert.Single(ideas);
        Assert.Single(allCards);
    }

    [Fact]
    public void CommitAdd_BodyOnly_AutoTitleFromFirstLine()
    {
        var draft = new Idea { Title = "", Body = "First line\nSecond line" };
        MakeSvc().CommitAdd(draft);

        Assert.Equal("First line", draft.Title);
    }

    [Fact]
    public void CommitAdd_BodyFirstLineOver40Chars_TruncatesAutoTitle()
    {
        var longLine = new string('x', 50);
        var draft = new Idea { Title = "", Body = longLine };
        MakeSvc().CommitAdd(draft);

        Assert.Equal(40, draft.Title.Length);
        Assert.Equal(longLine[..40], draft.Title);
    }

    [Fact]
    public void CommitAdd_SetsCreatedAtAndUpdatedAt_ToInjectedClock()
    {
        var draft = new Idea { Title = "T" };
        MakeSvc(now: () => FixedNow).CommitAdd(draft);

        Assert.Equal(FixedNow, draft.CreatedAt);
        Assert.Equal(FixedNow, draft.UpdatedAt);
    }

    [Fact]
    public void CommitAdd_InvokesDirtyRefreshTagsAndRefreshVisible_EachOnce()
    {
        var svc = MakeSvc(out var c);

        svc.CommitAdd(new Idea { Title = "T" });

        Assert.Equal(1, c.Dirty);
        Assert.Equal(1, c.Tags);
        Assert.Equal(1, c.Visible);
    }

    // ── CommitAddFromText ─────────────────────────────────────────────────────

    [Fact]
    public void CommitAddFromText_Empty_ReturnsFalseAndDoesNotAdd()
    {
        var ideas    = new List<Idea>();
        var allCards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeSvc(out var c, ideas: ideas, allCards: allCards);

        Assert.False(svc.CommitAddFromText(""));
        Assert.False(svc.CommitAddFromText("   \n  "));
        Assert.Empty(ideas);
        Assert.Empty(allCards);
        Assert.Equal(0, c.Dirty);
    }

    [Fact]
    public void CommitAddFromText_AddsCardWithBodyAndAutoTitle()
    {
        var ideas    = new List<Idea>();
        var allCards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeSvc(out var c, ideas: ideas, allCards: allCards);

        var body = "Pasted heading\nrest of clipboard";
        Assert.True(svc.CommitAddFromText(body));

        Assert.Single(ideas);
        Assert.Equal(body, ideas[0].Body);
        Assert.Equal("Pasted heading", ideas[0].Title);
        Assert.Equal(1, c.Dirty);
        Assert.Equal(1, c.Tags);
        Assert.Equal(1, c.Visible);
    }

    // ── CommitAddFromFileContent ──────────────────────────────────────────────

    [Fact]
    public void CommitAddFromFileContent_UsesFileNameAsTitleAndContentAsBody()
    {
        var ideas    = new List<Idea>();
        var allCards = new ObservableCollection<IdeaCardViewModel>();
        var svc = MakeSvc(out var c, ideas: ideas, allCards: allCards);

        Assert.True(svc.CommitAddFromFileContent("meeting-notes", "line 1\nline 2"));

        Assert.Single(ideas);
        Assert.Equal("meeting-notes", ideas[0].Title);
        Assert.Equal("line 1\nline 2", ideas[0].Body);
        Assert.Equal(1, c.Dirty);
    }

    [Fact]
    public void CommitAddFromFileContent_EmptyBodyButTitled_StillAdds()
    {
        var ideas    = new List<Idea>();
        var svc = MakeSvc(ideas: ideas);

        Assert.True(svc.CommitAddFromFileContent("empty", ""));

        Assert.Single(ideas);
        Assert.Equal("empty", ideas[0].Title);
        Assert.Equal("", ideas[0].Body);
    }

    [Fact]
    public void CommitAddFromFileContent_BothEmpty_ReturnsFalse()
    {
        var ideas = new List<Idea>();
        var svc = MakeSvc(ideas: ideas);

        Assert.False(svc.CommitAddFromFileContent("", ""));
        Assert.Empty(ideas);
    }

    // ── CommitEdit ────────────────────────────────────────────────────────────

    [Fact]
    public void CommitEdit_UpdatesCardUpdatedAt()
    {
        var idea = new Idea { Title = "Old", UpdatedAt = new DateTime(2020, 1, 1) };
        var card = new IdeaCardViewModel(idea);
        var before = DateTime.Now;

        MakeSvc(
            ideas:    new List<Idea> { idea },
            allCards: new ObservableCollection<IdeaCardViewModel> { card })
            .CommitEdit(card);

        Assert.True(card.UpdatedAt >= before);
    }

    [Fact]
    public void CommitEdit_InvokesDirtyRefreshTagsAndRefreshVisible_EachOnce()
    {
        var idea = new Idea { Title = "T" };
        var card = new IdeaCardViewModel(idea);
        var svc = MakeSvc(
            out var c,
            ideas:    new List<Idea> { idea },
            allCards: new ObservableCollection<IdeaCardViewModel> { card });

        svc.CommitEdit(card);

        Assert.Equal(1, c.Dirty);
        Assert.Equal(1, c.Tags);
        Assert.Equal(1, c.Visible);
    }

    // ── CommitDelete ──────────────────────────────────────────────────────────

    [Fact]
    public void CommitDelete_RemovesFromBothCollections()
    {
        var idea = new Idea { Title = "T" };
        var card = new IdeaCardViewModel(idea);
        var ideas    = new List<Idea> { idea };
        var allCards = new ObservableCollection<IdeaCardViewModel> { card };

        MakeSvc(ideas: ideas, allCards: allCards).CommitDelete(card);

        Assert.Empty(ideas);
        Assert.Empty(allCards);
    }

    [Fact]
    public void CommitDelete_InvokesDirtyRefreshTagsAndRefreshVisible_EachOnce()
    {
        var idea = new Idea { Title = "T" };
        var card = new IdeaCardViewModel(idea);
        var svc = MakeSvc(
            out var c,
            ideas:    new List<Idea> { idea },
            allCards: new ObservableCollection<IdeaCardViewModel> { card });

        svc.CommitDelete(card);

        Assert.Equal(1, c.Dirty);
        Assert.Equal(1, c.Tags);
        Assert.Equal(1, c.Visible);
    }

    // ── TogglePin ─────────────────────────────────────────────────────────────

    [Fact]
    public void TogglePin_FalseToTrue()
    {
        var card = MakeCard(pinned: false);
        MakeSvc().TogglePin(card);
        Assert.True(card.IsPinned);
    }

    [Fact]
    public void TogglePin_TrueToFalse()
    {
        var card = MakeCard(pinned: true);
        MakeSvc().TogglePin(card);
        Assert.False(card.IsPinned);
    }

    [Fact]
    public void TogglePin_InvokesDirtyAndRefreshVisible_ButNotRefreshTags()
    {
        var svc = MakeSvc(out var c);
        svc.TogglePin(MakeCard());

        Assert.Equal(1, c.Dirty);
        Assert.Equal(0, c.Tags);
        Assert.Equal(1, c.Visible);
    }

    // ── ToggleArchive ─────────────────────────────────────────────────────────

    [Fact]
    public void ToggleArchive_FalseToTrue()
    {
        var card = MakeCard(archived: false);
        MakeSvc().ToggleArchive(card);
        Assert.True(card.IsArchived);
    }

    [Fact]
    public void ToggleArchive_InvokesDirtyAndRefreshVisible_ButNotRefreshTags()
    {
        var svc = MakeSvc(out var c);
        svc.ToggleArchive(MakeCard());

        Assert.Equal(1, c.Dirty);
        Assert.Equal(0, c.Tags);
        Assert.Equal(1, c.Visible);
    }
}
