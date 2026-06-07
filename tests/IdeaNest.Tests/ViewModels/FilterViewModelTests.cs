using System.Collections.Generic;
using System;
using IdeaNest.Models;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class FilterViewModelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FilterViewModel Make(
        Action? onRefresh = null, Action? onDirty = null)
        => new(onRefresh ?? (() => { }), onDirty ?? (() => { }));

    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void Defaults_AreEmpty_AndArchivedHidden()
    {
        var vm = Make();
        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal(string.Empty, vm.SelectedTag);
        Assert.Equal(string.Empty, vm.SelectedColor);
        Assert.False(vm.ShowArchived);
        Assert.False(vm.HasActiveFilter);
    }

    // ── SearchText ────────────────────────────────────────────────────────────

    [Fact]
    public void SearchText_Change_TriggersRefreshAndDirty()
    {
        int refresh = 0, dirty = 0;
        var vm = Make(onRefresh: () => refresh++, onDirty: () => dirty++);

        vm.SearchText = "hello";

        Assert.Equal("hello", vm.SearchText);
        Assert.Equal(1, refresh);
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void SearchText_SameValue_DoesNotTriggerCallbacks()
    {
        int refresh = 0;
        var vm = Make(onRefresh: () => refresh++);
        vm.SearchText = "x";
        refresh = 0;

        vm.SearchText = "x"; // same value

        Assert.Equal(0, refresh);
    }

    [Fact]
    public void SearchText_NullAssigned_TreatedAsEmpty()
    {
        var vm = Make();
        vm.SearchText = null!;
        Assert.Equal(string.Empty, vm.SearchText);
    }

    // ── SelectedTag ───────────────────────────────────────────────────────────

    [Fact]
    public void SelectedTag_Change_TriggersRefreshAndDirty()
    {
        int refresh = 0, dirty = 0;
        var vm = Make(onRefresh: () => refresh++, onDirty: () => dirty++);

        vm.SelectedTag = "design";

        Assert.Equal("design", vm.SelectedTag);
        Assert.Equal(1, refresh);
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void SelectedTag_SameValue_DoesNotTriggerCallbacks()
    {
        int refresh = 0;
        var vm = Make(onRefresh: () => refresh++);
        vm.SelectedTag = "ui";
        refresh = 0;

        vm.SelectedTag = "ui";

        Assert.Equal(0, refresh);
    }

    [Fact]
    public void SelectedTag_NullAssigned_TreatedAsEmpty()
    {
        var vm = Make();
        vm.SelectedTag = null!;
        Assert.Equal(string.Empty, vm.SelectedTag);
    }

    // ── SelectedColor ─────────────────────────────────────────────────────────

    [Fact]
    public void SelectedColor_Change_TriggersRefreshAndDirty()
    {
        int refresh = 0, dirty = 0;
        var vm = Make(onRefresh: () => refresh++, onDirty: () => dirty++);

        vm.SelectedColor = "yellow";

        Assert.Equal("yellow", vm.SelectedColor);
        Assert.Equal(1, refresh);
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void SelectedColor_NullAssigned_TreatedAsEmpty()
    {
        var vm = Make();
        vm.SelectedColor = null!;
        Assert.Equal(string.Empty, vm.SelectedColor);
    }

    // ── ShowArchived ──────────────────────────────────────────────────────────

    [Fact]
    public void ShowArchived_Change_TriggersRefreshAndDirty()
    {
        int refresh = 0, dirty = 0;
        var vm = Make(onRefresh: () => refresh++, onDirty: () => dirty++);

        vm.ShowArchived = true;

        Assert.True(vm.ShowArchived);
        Assert.Equal(1, refresh);
        Assert.Equal(1, dirty);
    }

    [Fact]
    public void ShowArchived_SameValue_DoesNotTriggerCallbacks()
    {
        int refresh = 0;
        var vm = Make(onRefresh: () => refresh++);
        // default is false; setting to false again → no callback

        vm.ShowArchived = false;

        Assert.Equal(0, refresh);
    }

    // ── HasActiveFilter ───────────────────────────────────────────────────────

    [Fact]
    public void HasActiveFilter_False_WhenAllEmpty()
    {
        Assert.False(Make().HasActiveFilter);
    }

    [Theory]
    [InlineData("keyword", "", "")]
    [InlineData("", "ui", "")]
    [InlineData("", "", "yellow")]
    public void HasActiveFilter_True_WhenAnyFieldSet(
        string search, string tag, string color)
    {
        var vm = Make();
        vm.SearchText   = search;
        vm.SelectedTag  = tag;
        vm.SelectedColor = color;

        Assert.True(vm.HasActiveFilter);
    }

    [Fact]
    public void HasActiveFilter_WhitespaceOnly_IsFalse()
    {
        var vm = Make();
        vm.SearchText = "   ";
        Assert.False(vm.HasActiveFilter);
    }

    [Fact]
    public void HasActiveFilter_ShowArchived_DoesNotAffectIt()
    {
        // ShowArchived is a visibility toggle, not a search filter
        var vm = Make();
        vm.ShowArchived = true;
        Assert.False(vm.HasActiveFilter);
    }

    [Fact]
    public void HasActiveFilter_PropertyChanged_FiredOnSearchTextChange()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.SearchText = "test";

        Assert.Contains(nameof(FilterViewModel.HasActiveFilter), fired);
    }

    [Fact]
    public void HasActiveFilter_PropertyChanged_FiredOnSelectedTagChange()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.SelectedTag = "mytag";

        Assert.Contains(nameof(FilterViewModel.HasActiveFilter), fired);
    }

    [Fact]
    public void HasActiveFilter_PropertyChanged_FiredOnSelectedColorChange()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.SelectedColor = "blue";

        Assert.Contains(nameof(FilterViewModel.HasActiveFilter), fired);
    }

    // ── ClearFilter ───────────────────────────────────────────────────────────

    [Fact]
    public void ClearFilter_ClearsAllThreeFilterValues()
    {
        var vm = Make();
        vm.SearchText   = "q";
        vm.SelectedTag  = "t";
        vm.SelectedColor = "c";

        vm.ClearFilter();

        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal(string.Empty, vm.SelectedTag);
        Assert.Equal(string.Empty, vm.SelectedColor);
    }

    [Fact]
    public void ClearFilter_SetsHasActiveFilter_ToFalse()
    {
        var vm = Make();
        vm.SearchText = "anything";
        Assert.True(vm.HasActiveFilter);

        vm.ClearFilter();

        Assert.False(vm.HasActiveFilter);
    }

    [Fact]
    public void ClearFilter_WhenAlreadyClear_DoesNotFireCallbacks()
    {
        int refresh = 0;
        var vm = Make(onRefresh: () => refresh++);
        // All fields are already empty — ClearFilter should be a no-op.

        vm.ClearFilter();

        Assert.Equal(0, refresh);
    }

    // ── PropertyChanged ───────────────────────────────────────────────────────

    [Fact]
    public void SearchText_Change_RaisesPropertyChanged()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.SearchText = "abc";

        Assert.Contains(nameof(FilterViewModel.SearchText), fired);
    }

    [Fact]
    public void ShowArchived_Change_RaisesPropertyChanged()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.ShowArchived = true;

        Assert.Contains(nameof(FilterViewModel.ShowArchived), fired);
    }

    // ── SyncToSettings ────────────────────────────────────────────────────────

    [Fact]
    public void SyncToSettings_WritesAllFourFields()
    {
        var vm = Make();
        vm.SearchText   = "my query";
        vm.SelectedTag  = "design";
        vm.SelectedColor = "pink";
        vm.ShowArchived = true;

        var settings = new WorkspaceSettings();
        vm.SyncToSettings(settings);

        Assert.Equal("my query", settings.SearchText);
        Assert.Equal("design",   settings.SelectedTag);
        Assert.Equal("pink",     settings.SelectedColor);
        Assert.True(settings.ShowArchived);
    }

    [Fact]
    public void SyncToSettings_DefaultValues_WritesEmptyStringsAndFalse()
    {
        var settings = new WorkspaceSettings
        {
            SearchText = "stale",
            SelectedTag = "stale",
            SelectedColor = "stale",
            ShowArchived = true,
        };

        Make().SyncToSettings(settings);

        Assert.Equal(string.Empty, settings.SearchText);
        Assert.Equal(string.Empty, settings.SelectedTag);
        Assert.Equal(string.Empty, settings.SelectedColor);
        Assert.False(settings.ShowArchived);
    }

    // ── LoadFromSettings ──────────────────────────────────────────────────────

    [Fact]
    public void LoadFromSettings_RestoresAllFourFields()
    {
        var vm = Make();
        var settings = new WorkspaceSettings
        {
            SearchText   = "restored query",
            SelectedTag  = "restored-tag",
            SelectedColor = "green",
            ShowArchived = true,
        };

        vm.LoadFromSettings(settings);

        Assert.Equal("restored query", vm.SearchText);
        Assert.Equal("restored-tag",   vm.SelectedTag);
        Assert.Equal("green",          vm.SelectedColor);
        Assert.True(vm.ShowArchived);
        Assert.True(vm.HasActiveFilter);
    }

    [Fact]
    public void LoadFromSettings_NullValues_FallBackToEmpty()
    {
        var vm = Make();
        var settings = new WorkspaceSettings
        {
            SearchText   = null!,
            SelectedTag  = null!,
            SelectedColor = null!,
        };

        vm.LoadFromSettings(settings);

        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal(string.Empty, vm.SelectedTag);
        Assert.Equal(string.Empty, vm.SelectedColor);
    }

    [Fact]
    public void LoadFromSettings_FiresPropertyChangedForAllFields()
    {
        var vm = Make();
        var fired = new List<string>();
        vm.PropertyChanged += (_, e) => fired.Add(e.PropertyName ?? "");

        vm.LoadFromSettings(new WorkspaceSettings
        {
            SearchText = "x", SelectedTag = "t", SelectedColor = "c", ShowArchived = true,
        });

        Assert.Contains(nameof(FilterViewModel.SearchText),   fired);
        Assert.Contains(nameof(FilterViewModel.SelectedTag),  fired);
        Assert.Contains(nameof(FilterViewModel.SelectedColor), fired);
        Assert.Contains(nameof(FilterViewModel.ShowArchived),  fired);
        Assert.Contains(nameof(FilterViewModel.HasActiveFilter), fired);
    }

    [Fact]
    public void LoadFromSettings_DoesNotFireRefreshOrDirtyCallbacks()
    {
        int refresh = 0, dirty = 0;
        var vm = Make(onRefresh: () => refresh++, onDirty: () => dirty++);

        vm.LoadFromSettings(new WorkspaceSettings { SearchText = "q" });

        Assert.Equal(0, refresh);
        Assert.Equal(0, dirty);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public void SyncAndLoad_RoundTrips_AllFourFields()
    {
        var vm = Make();
        vm.SearchText   = "round-trip";
        vm.SelectedTag  = "rt-tag";
        vm.SelectedColor = "orange";
        vm.ShowArchived = true;

        var settings = new WorkspaceSettings();
        vm.SyncToSettings(settings);

        var vm2 = Make();
        vm2.LoadFromSettings(settings);

        Assert.Equal(vm.SearchText,   vm2.SearchText);
        Assert.Equal(vm.SelectedTag,  vm2.SelectedTag);
        Assert.Equal(vm.SelectedColor, vm2.SelectedColor);
        Assert.Equal(vm.ShowArchived,  vm2.ShowArchived);
    }
}
