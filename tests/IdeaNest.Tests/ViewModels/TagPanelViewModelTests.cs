using System.Collections.Generic;
using System;
using System.Linq;
using IdeaNest.Models;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class TagPanelViewModelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TagPanelViewModel Make(
        Action? onDirty = null, Action<string>? onTagSelected = null)
        => new(onDirty ?? (() => { }), onTagSelected ?? (_ => { }));

    private static TagItemViewModel Item(string name, int count = 1)
        => new(name, count);

    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void Defaults_PanelClosed_EmptyTagSearch_NoVisibleItems()
    {
        var vm = Make();
        Assert.False(vm.IsTagPanelOpen);
        Assert.Equal(string.Empty, vm.TagSearch);
        Assert.False(vm.HasTagSearch);
        Assert.Empty(vm.VisibleItems);
    }

    // ── IsTagPanelOpen ────────────────────────────────────────────────────────

    [Fact]
    public void IsTagPanelOpen_SetTrue_TriggersMarkDirty()
    {
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);

        vm.IsTagPanelOpen = true;

        Assert.True(vm.IsTagPanelOpen);
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void IsTagPanelOpen_SameValue_DoesNotTriggerMarkDirty()
    {
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);
        // default is false; setting false again → no callback

        vm.IsTagPanelOpen = false;

        Assert.Equal(0, dirty);
    }

    [Fact]
    public void IsTagPanelOpen_Change_RaisesPropertyChanged()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.IsTagPanelOpen = true;

        Assert.Contains(nameof(TagPanelViewModel.IsTagPanelOpen), fired);
        Assert.Contains(nameof(TagPanelViewModel.TagPanelButtonLabel), fired);
        Assert.Contains(nameof(TagPanelViewModel.TagPanelButtonTip), fired);
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    [Fact]
    public void Toggle_FlipsIsTagPanelOpen_FromFalse()
    {
        var vm = Make();
        vm.Toggle();
        Assert.True(vm.IsTagPanelOpen);
    }

    [Fact]
    public void Toggle_FlipsIsTagPanelOpen_FromTrue()
    {
        var vm = Make();
        vm.IsTagPanelOpen = true;

        vm.Toggle();

        Assert.False(vm.IsTagPanelOpen);
    }

    // ── TagPanelButtonLabel / Tip ─────────────────────────────────────────────

    [Fact]
    public void TagPanelButtonLabel_ReflectsOpenState()
    {
        var vm = Make();
        Assert.Equal("タグ ▶", vm.TagPanelButtonLabel);

        vm.IsTagPanelOpen = true;
        Assert.Equal("タグ ◀", vm.TagPanelButtonLabel);
    }

    [Fact]
    public void TagPanelButtonTip_ReflectsOpenState()
    {
        var vm = Make();
        Assert.Equal("タグパネルを表示", vm.TagPanelButtonTip);

        vm.IsTagPanelOpen = true;
        Assert.Equal("タグパネルを閉じる", vm.TagPanelButtonTip);
    }

    // ── TagSearch ─────────────────────────────────────────────────────────────

    [Fact]
    public void TagSearch_Change_DoesNotTriggerMarkDirty()
    {
        // TagSearch is a local display filter — it is not persisted to settings.
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);

        vm.TagSearch = "keyword";

        Assert.Equal(0, dirty);
    }

    [Fact]
    public void TagSearch_NullAssigned_TreatedAsEmpty()
    {
        var vm = Make();
        vm.TagSearch = null!;
        Assert.Equal(string.Empty, vm.TagSearch);
    }

    [Fact]
    public void HasTagSearch_False_WhenEmpty()
    {
        Assert.False(Make().HasTagSearch);
    }

    [Fact]
    public void HasTagSearch_True_WhenSet()
    {
        var vm = Make();
        vm.TagSearch = "UI";
        Assert.True(vm.HasTagSearch);
    }

    [Fact]
    public void HasTagSearch_False_WhenWhitespaceOnly()
    {
        var vm = Make();
        vm.TagSearch = "   ";
        Assert.False(vm.HasTagSearch);
    }

    [Fact]
    public void HasTagSearch_PropertyChanged_FiredOnTagSearchChange()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.TagSearch = "x";

        Assert.Contains(nameof(TagPanelViewModel.HasTagSearch), fired);
    }

    // ── ClearTagSearch ────────────────────────────────────────────────────────

    [Fact]
    public void ClearTagSearch_ResetsToEmpty()
    {
        var vm = Make();
        vm.TagSearch = "something";

        vm.ClearTagSearch();

        Assert.Equal(string.Empty, vm.TagSearch);
        Assert.False(vm.HasTagSearch);
    }

    [Fact]
    public void ClearTagSearch_WhenAlreadyEmpty_IsNoOp()
    {
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);
        // TagSearch is already empty; ClearTagSearch should not fire any callback.
        var firedProps = new List<string>();
        vm.PropertyChanged += (_, e) => firedProps.Add(e.PropertyName ?? "");

        vm.ClearTagSearch();

        Assert.Equal(0, dirty);
        Assert.DoesNotContain(nameof(TagPanelViewModel.TagSearch), firedProps);
    }

    // ── VisibleItems (tag search filtering) ───────────────────────────────────

    [Fact]
    public void SetAllItems_EmptySearch_ShowsAllTags()
    {
        var vm = Make();
        vm.SetAllItems(new[] { Item("UI"), Item("設計"), Item("開発") });

        Assert.Equal(3, vm.VisibleItems.Count);
    }

    [Fact]
    public void SetAllItems_WithActiveSearch_AppliesFilter()
    {
        var vm = Make();
        vm.TagSearch = "ui";
        vm.SetAllItems(new[] { Item("UI"), Item("設計"), Item("UIテスト") });

        Assert.Equal(2, vm.VisibleItems.Count);
        Assert.All(vm.VisibleItems, t =>
            Assert.Contains("ui", t.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TagSearch_Change_FiltersExistingItems()
    {
        var vm = Make();
        vm.SetAllItems(new[] { Item("UI"), Item("設計"), Item("UIテスト") });

        vm.TagSearch = "ui";

        Assert.Equal(2, vm.VisibleItems.Count);
    }

    [Fact]
    public void TagSearch_Empty_AfterFiltering_ShowsAllItems()
    {
        var vm = Make();
        vm.SetAllItems(new[] { Item("UI"), Item("設計") });
        vm.TagSearch = "UI";

        vm.TagSearch = string.Empty;

        Assert.Equal(2, vm.VisibleItems.Count);
    }

    [Fact]
    public void TagSearch_CaseInsensitive()
    {
        var vm = Make();
        vm.SetAllItems(new[] { Item("UI"), Item("ui"), Item("Ui"), Item("設計") });

        vm.TagSearch = "ui";

        Assert.Equal(3, vm.VisibleItems.Count);
    }

    [Fact]
    public void TagSearch_NoMatch_ShowsEmpty()
    {
        var vm = Make();
        vm.SetAllItems(new[] { Item("UI"), Item("設計") });

        vm.TagSearch = "xyz-not-found";

        Assert.Empty(vm.VisibleItems);
    }

    // ── SelectTag ─────────────────────────────────────────────────────────────

    [Fact]
    public void SelectTag_InvokesOnTagSelectedCallback()
    {
        string? received = null;
        var vm = Make(onTagSelected: tag => received = tag);

        vm.SelectTag("design");

        Assert.Equal("design", received);
    }

    [Fact]
    public void SelectTag_NullCoercedToEmpty()
    {
        string? received = null;
        var vm = Make(onTagSelected: tag => received = tag);

        vm.SelectTag(null!);

        Assert.Equal(string.Empty, received);
    }

    [Fact]
    public void SelectTag_EmptyString_InvokesCallbackWithEmpty()
    {
        int callCount = 0;
        var vm = Make(onTagSelected: _ => callCount++);

        vm.SelectTag(string.Empty);

        Assert.Equal(1, callCount);
    }

    // ── SyncToSettings ────────────────────────────────────────────────────────

    [Fact]
    public void SyncToSettings_WritesTagPanelOpen()
    {
        var vm = Make();
        vm.IsTagPanelOpen = true;

        var settings = new WorkspaceSettings();
        vm.SyncToSettings(settings);

        Assert.True(settings.TagPanelOpen);
    }

    [Fact]
    public void SyncToSettings_TagSearch_IsNotPersisted()
    {
        // TagSearch is a runtime UI filter; it must not be written to settings.
        var vm = Make();
        vm.TagSearch = "some-filter";
        var settings = new WorkspaceSettings();

        vm.SyncToSettings(settings);

        // WorkspaceSettings has no TagSearch field — verified by checking
        // that the call succeeds and settings is consistent.
        Assert.False(settings.TagPanelOpen); // only TagPanelOpen was synced
    }

    // ── LoadFromSettings ──────────────────────────────────────────────────────

    [Fact]
    public void LoadFromSettings_RestoresIsTagPanelOpen()
    {
        var vm = Make();
        vm.LoadFromSettings(new WorkspaceSettings { TagPanelOpen = true });
        Assert.True(vm.IsTagPanelOpen);
    }

    [Fact]
    public void LoadFromSettings_Default_FalseWhenNotSet()
    {
        var vm = Make();
        vm.IsTagPanelOpen = true; // set explicitly

        vm.LoadFromSettings(new WorkspaceSettings()); // default is false

        Assert.False(vm.IsTagPanelOpen);
    }

    [Fact]
    public void LoadFromSettings_FiresPropertyChangedForOpenState()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.LoadFromSettings(new WorkspaceSettings { TagPanelOpen = true });

        Assert.Contains(nameof(TagPanelViewModel.IsTagPanelOpen), fired);
        Assert.Contains(nameof(TagPanelViewModel.TagPanelButtonLabel), fired);
        Assert.Contains(nameof(TagPanelViewModel.TagPanelButtonTip), fired);
    }

    [Fact]
    public void LoadFromSettings_DoesNotFireMarkDirty()
    {
        int dirty = 0;
        var vm = Make(onDirty: () => dirty++);

        vm.LoadFromSettings(new WorkspaceSettings { TagPanelOpen = true });

        Assert.Equal(0, dirty);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public void SyncAndLoad_RoundTrips_TagPanelOpen()
    {
        var vm = Make();
        vm.IsTagPanelOpen = true;

        var settings = new WorkspaceSettings();
        vm.SyncToSettings(settings);

        var vm2 = Make();
        vm2.LoadFromSettings(settings);

        Assert.Equal(vm.IsTagPanelOpen, vm2.IsTagPanelOpen);
    }

    [Fact]
    public void OnMarkDirty_FiresAfterFieldUpdate_SoCallbackCanSyncLatestValue()
    {
        // Same ordering contract verified for FilterViewModel: by the time
        // onMarkDirty fires, the new value is already visible on the ViewModel.
        var settings = new WorkspaceSettings();
        TagPanelViewModel? capturedVm = null;
        capturedVm = new TagPanelViewModel(
            onMarkDirty: () => capturedVm!.SyncToSettings(settings),
            onTagSelected: _ => { });

        capturedVm.IsTagPanelOpen = true;

        Assert.True(settings.TagPanelOpen);
    }
}
