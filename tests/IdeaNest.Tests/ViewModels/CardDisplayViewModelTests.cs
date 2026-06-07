using System;
using System.Collections.Generic;
using System.Linq;
using IdeaNest.Models;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class CardDisplayViewModelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CardDisplayViewModel Make(
        Action? onRefresh = null, Action? onDirty = null)
        => new(onRefresh ?? (() => { }), onDirty ?? (() => { }));

    private static IdeaCardViewModel Card(string id, bool pinned = false)
        => new(new Idea { Id = id, IsPinned = pinned });

    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void Defaults_AreMediumFixedUpdatedDesc()
    {
        var vm = Make();
        Assert.Equal("medium", vm.CardSize);
        Assert.Equal("fixed", vm.CardHeightMode);
        Assert.Equal("UpdatedDesc", vm.SortMode);
        Assert.True(vm.IsCardSizeMedium);
        Assert.True(vm.IsCardHeightFixed);
        Assert.False(vm.IsShuffleMode);
    }

    // ── CardWidth ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("small",  184)]
    [InlineData("medium", 252)]
    [InlineData("large",  340)]
    public void CardWidth_MatchesSize(string size, double expected)
    {
        var vm = Make();
        vm.CardSize = size;
        Assert.Equal(expected, vm.CardWidth);
    }

    // ── CardHeight (fixed mode) ───────────────────────────────────────────────

    [Theory]
    [InlineData("small",  148)]
    [InlineData("medium", 212)]
    [InlineData("large",  280)]
    public void CardHeight_Fixed_MatchesSize(string size, double expected)
    {
        var vm = Make();
        vm.CardSize = size;
        Assert.Equal(expected, vm.CardHeight);
    }

    [Fact]
    public void CardMinHeight_Fixed_IsZero()
    {
        var vm = Make();
        Assert.Equal(0, vm.CardMinHeight);
    }

    [Fact]
    public void CardMaxHeight_Fixed_IsPositiveInfinity()
    {
        var vm = Make();
        Assert.Equal(double.PositiveInfinity, vm.CardMaxHeight);
    }

    // ── CardHeight (auto mode) ────────────────────────────────────────────────

    [Theory]
    [InlineData("small")]
    [InlineData("medium")]
    [InlineData("large")]
    public void CardHeight_Auto_IsNaN_RegardlessOfSize(string size)
    {
        var vm = Make();
        vm.CardSize = size;
        vm.CardHeightMode = "auto";
        Assert.True(double.IsNaN(vm.CardHeight));
    }

    [Theory]
    [InlineData("small",  110, 200)]
    [InlineData("medium", 140, 280)]
    [InlineData("large",  180, 380)]
    public void CardMinAndMaxHeight_Auto_MatchSize(string size, double min, double max)
    {
        var vm = Make();
        vm.CardSize = size;
        vm.CardHeightMode = "auto";
        Assert.Equal(min, vm.CardMinHeight);
        Assert.Equal(max, vm.CardMaxHeight);
    }

    // ── Size flags ────────────────────────────────────────────────────────────

    [Fact]
    public void SizeFlags_OnlyOneActiveAtATime()
    {
        var vm = Make();

        vm.CardSize = "small";
        Assert.True(vm.IsCardSizeSmall);
        Assert.False(vm.IsCardSizeMedium);
        Assert.False(vm.IsCardSizeLarge);

        vm.CardSize = "large";
        Assert.False(vm.IsCardSizeSmall);
        Assert.False(vm.IsCardSizeMedium);
        Assert.True(vm.IsCardSizeLarge);

        vm.CardSize = "medium";
        Assert.False(vm.IsCardSizeSmall);
        Assert.True(vm.IsCardSizeMedium);
        Assert.False(vm.IsCardSizeLarge);
    }

    // ── Height-mode flags ─────────────────────────────────────────────────────

    [Fact]
    public void HeightModeFlags_ToggleMutuallyExclusive()
    {
        var vm = Make();

        Assert.True(vm.IsCardHeightFixed);
        Assert.False(vm.IsCardHeightAuto);

        vm.CardHeightMode = "auto";
        Assert.False(vm.IsCardHeightFixed);
        Assert.True(vm.IsCardHeightAuto);

        vm.CardHeightMode = "anything-else";
        Assert.True(vm.IsCardHeightFixed);
        Assert.False(vm.IsCardHeightAuto);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public void CardSize_UnknownValue_DefaultsToMedium()
    {
        var vm = Make();
        vm.CardSize = "huge";
        Assert.Equal("medium", vm.CardSize);
        Assert.True(vm.IsCardSizeMedium);
    }

    [Fact]
    public void SortMode_UnknownValue_DefaultsToUpdatedDesc()
    {
        var vm = Make();
        vm.SortMode = "random-string";
        Assert.Equal("UpdatedDesc", vm.SortMode);
        Assert.False(vm.IsShuffleMode);
    }

    // ── IsShuffleMode ─────────────────────────────────────────────────────────

    [Fact]
    public void IsShuffleMode_TrueOnlyForShuffle()
    {
        var vm = Make();

        vm.SortMode = "Shuffle";
        Assert.True(vm.IsShuffleMode);

        vm.SortMode = "UpdatedDesc";
        Assert.False(vm.IsShuffleMode);
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    [Fact]
    public void CardSize_Change_InvokesMarkDirty()
    {
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);
        vm.CardSize = "large";
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void CardSize_SameValue_DoesNotInvokeMarkDirty()
    {
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);
        vm.CardSize = "medium"; // default already
        Assert.Equal(0, dirty);
    }

    [Fact]
    public void CardHeightMode_Change_InvokesMarkDirty()
    {
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);
        vm.CardHeightMode = "auto";
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void SortMode_Change_InvokesBothCallbacks()
    {
        int refresh = 0, dirty = 0;
        var vm = Make(onRefresh: () => refresh++, onDirty: () => dirty++);
        vm.SortMode = "CreatedDesc";
        Assert.Equal(1, refresh);
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void Reshuffle_InvokesRefreshVisible()
    {
        int refresh = 0;
        var vm = Make(onRefresh: () => refresh++);
        vm.Reshuffle(new[] { "id1", "id2" });
        Assert.Equal(1, refresh);
    }

    // ── PropertyChanged ───────────────────────────────────────────────────────

    [Fact]
    public void CardSize_Change_RaisesExpectedPropertyChangedEvents()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.CardSize = "large";

        Assert.Contains(nameof(CardDisplayViewModel.CardSize), fired);
        Assert.Contains(nameof(CardDisplayViewModel.CardWidth), fired);
        Assert.Contains(nameof(CardDisplayViewModel.CardHeight), fired);
        Assert.Contains(nameof(CardDisplayViewModel.CardMinHeight), fired);
        Assert.Contains(nameof(CardDisplayViewModel.CardMaxHeight), fired);
        Assert.Contains(nameof(CardDisplayViewModel.IsCardSizeSmall), fired);
        Assert.Contains(nameof(CardDisplayViewModel.IsCardSizeMedium), fired);
        Assert.Contains(nameof(CardDisplayViewModel.IsCardSizeLarge), fired);
    }

    [Fact]
    public void CardHeightMode_Change_RaisesExpectedPropertyChangedEvents()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.CardHeightMode = "auto";

        Assert.Contains(nameof(CardDisplayViewModel.CardHeightMode), fired);
        Assert.Contains(nameof(CardDisplayViewModel.CardHeight), fired);
        Assert.Contains(nameof(CardDisplayViewModel.IsCardHeightFixed), fired);
        Assert.Contains(nameof(CardDisplayViewModel.IsCardHeightAuto), fired);
    }

    // ── SyncToSettings / LoadFromSettings ─────────────────────────────────────

    [Fact]
    public void SyncToSettings_WritesAllThreeFields()
    {
        var vm = Make();
        vm.CardSize = "large";
        vm.CardHeightMode = "auto";
        vm.SortMode = "TitleAsc";

        var settings = new WorkspaceSettings();
        vm.SyncToSettings(settings);

        Assert.Equal("large", settings.CardSize);
        Assert.Equal("auto", settings.CardHeightMode);
        Assert.Equal("TitleAsc", settings.SortMode);
    }

    [Fact]
    public void LoadFromSettings_RestoresAllThreeFields()
    {
        var vm = Make();
        var settings = new WorkspaceSettings
        {
            CardSize = "small",
            CardHeightMode = "auto",
            SortMode = "CreatedDesc",
        };

        vm.LoadFromSettings(settings);

        Assert.Equal("small", vm.CardSize);
        Assert.Equal("auto", vm.CardHeightMode);
        Assert.Equal("CreatedDesc", vm.SortMode);
        Assert.True(vm.IsCardSizeSmall);
        Assert.True(vm.IsCardHeightAuto);
        Assert.False(vm.IsShuffleMode);
    }

    [Fact]
    public void LoadFromSettings_InvalidValues_FallBackToDefaults()
    {
        var vm = Make();
        var settings = new WorkspaceSettings
        {
            CardSize = "xxl",
            CardHeightMode = "stretch",
            SortMode = "random",
        };

        vm.LoadFromSettings(settings);

        Assert.Equal("medium", vm.CardSize);
        Assert.Equal("fixed", vm.CardHeightMode);
        Assert.Equal("UpdatedDesc", vm.SortMode);
    }

    [Fact]
    public void ClearShuffleOrder_EmptiesTheSnapshot()
    {
        var vm = Make();
        vm.Reshuffle(new[] { "a", "b", "c" });
        Assert.Equal(3, vm.ShuffleOrderSnapshot.Count);

        vm.ClearShuffleOrder();

        Assert.Empty(vm.ShuffleOrderSnapshot);
    }

    [Fact]
    public void LoadFromSettings_ClearsPreviousShuffleOrder()
    {
        var vm = Make();
        vm.Reshuffle(new[] { "old-1", "old-2" });
        Assert.NotEmpty(vm.ShuffleOrderSnapshot);

        vm.LoadFromSettings(new WorkspaceSettings { SortMode = "Shuffle" });

        // The shuffle order itself must be empty — not merely "OrderByShuffle returns 2".
        // Without ClearShuffleOrder(), old-1 / old-2 would still be present here.
        Assert.Empty(vm.ShuffleOrderSnapshot);
    }

    [Fact]
    public void LoadFromSettings_NextOrderByShuffle_ReseedsFromCurrentCards()
    {
        var vm = Make();
        vm.Reshuffle(new[] { "old-1", "old-2" });

        vm.LoadFromSettings(new WorkspaceSettings { SortMode = "Shuffle" });

        var cards = new[] { Card("new-1"), Card("new-2") };
        var result = vm.OrderByShuffle(cards, cards).ToList();

        Assert.Equal(2, result.Count);
        // Re-seeded order must contain only the current card ids — no leftover old-* entries.
        Assert.Equal(new[] { "new-1", "new-2" }.OrderBy(x => x),
                     vm.ShuffleOrderSnapshot.OrderBy(x => x));
    }

    // ── Shuffle / OrderByShuffle ──────────────────────────────────────────────

    [Fact]
    public void GenerateShuffleOrder_ProducesPermutationOfInputIds()
    {
        var vm = Make();
        var ids = new[] { "a", "b", "c", "d", "e" };

        vm.GenerateShuffleOrder(ids);

        var allCards = ids.Select(id => Card(id)).ToList();
        var result = vm.OrderByShuffle(allCards, allCards).Select(c => c.Id).ToList();
        Assert.Equal(ids.Length, result.Count);
        Assert.Equal(ids.OrderBy(x => x), result.OrderBy(x => x));
    }

    [Fact]
    public void OrderByShuffle_SeedsLazily_OnFirstCall()
    {
        var vm = Make();
        var cards = new[] { Card("x"), Card("y"), Card("z") };

        // No explicit Reshuffle — OrderByShuffle should seed itself.
        var result = vm.OrderByShuffle(cards, cards).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(
            cards.Select(c => c.Id).OrderBy(x => x),
            result.Select(c => c.Id).OrderBy(x => x));
    }

    [Fact]
    public void OrderByShuffle_PrependNewCards_ToFrontOfExistingOrder()
    {
        var vm = Make();
        var original = new[] { Card("a"), Card("b") };
        vm.OrderByShuffle(original, original).ToList(); // seed

        var newCard = Card("new");
        var allCards = original.Concat(new[] { newCard }).ToList();

        var result = vm.OrderByShuffle(allCards, allCards).ToList();

        Assert.Equal(3, result.Count);
        // "new" should appear at index 0 (inserted at the front when unknown)
        Assert.Equal("new", result[0].Id);
    }

    [Fact]
    public void OrderByShuffle_IgnoresPinnedCards_WhenSeeding()
    {
        var vm = Make();
        var pinned = Card("pinned", pinned: true);
        var unpinned1 = Card("u1");
        var unpinned2 = Card("u2");
        var all = new[] { pinned, unpinned1, unpinned2 };

        // source is only the non-pinned cards (as RefreshVisible filters them)
        var result = vm.OrderByShuffle(new[] { unpinned1, unpinned2 }, all).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, c => c.Id == "pinned");
    }
}
