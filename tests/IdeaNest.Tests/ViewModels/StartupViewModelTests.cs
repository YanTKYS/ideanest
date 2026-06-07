using System;
using System.Collections.Generic;
using System.Linq;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class StartupViewModelTests
{
    private static bool AlwaysExists(string _) => true;
    private static bool NeverExists(string _) => false;

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_LoadsExistingFilesAsItems()
    {
        // Use Path.Combine so DisplayName via Path.GetFileName resolves correctly
        // on whichever platform the test runner is hosted on.
        var pathA = System.IO.Path.Combine("notes", "a.ideanest");
        var pathB = System.IO.Path.Combine("notes", "b.ideanest");

        var vm = new StartupViewModel(new[] { pathA, pathB }, AlwaysExists);

        Assert.Equal(2, vm.Items.Count);
        Assert.Equal(pathA, vm.Items[0].FullPath);
        Assert.Equal("a.ideanest", vm.Items[0].DisplayName);
    }

    [Fact]
    public void Constructor_FiltersOutMissingFiles()
    {
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\a.ideanest" };

        var vm = new StartupViewModel(
            new[] { @"C:\a.ideanest", @"C:\missing.ideanest" },
            present.Contains);

        Assert.Single(vm.Items);
        Assert.Equal(@"C:\a.ideanest", vm.Items[0].FullPath);
    }

    [Fact]
    public void Constructor_NoRecentFiles_HasItemsIsFalse()
    {
        var vm = new StartupViewModel(Array.Empty<string>(), AlwaysExists);

        Assert.False(vm.HasItems);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public void Constructor_DefaultChoiceIsCancel()
    {
        var vm = new StartupViewModel(Array.Empty<string>(), AlwaysExists);

        Assert.Equal(StartupChoice.Cancel, vm.Choice);
        Assert.Null(vm.SelectedPath);
    }

    [Fact]
    public void Constructor_NullList_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new StartupViewModel(null!, AlwaysExists));
    }

    // ── ChooseNew / Cancel ───────────────────────────────────────────────────

    [Fact]
    public void ChooseNew_SetsChoiceAndClearsPath()
    {
        var vm = new StartupViewModel(new[] { @"C:\a.ideanest" }, AlwaysExists);

        vm.ChooseNew();

        Assert.Equal(StartupChoice.New, vm.Choice);
        Assert.Null(vm.SelectedPath);
    }

    [Fact]
    public void Cancel_SetsChoiceToCancel()
    {
        var vm = new StartupViewModel(new[] { @"C:\a.ideanest" }, AlwaysExists);
        vm.ChooseNew();

        vm.Cancel();

        Assert.Equal(StartupChoice.Cancel, vm.Choice);
        Assert.Null(vm.SelectedPath);
    }

    // ── TryChooseOpen ────────────────────────────────────────────────────────

    [Fact]
    public void TryChooseOpen_ExistingPath_SetsChoiceAndPath()
    {
        var vm = new StartupViewModel(new[] { @"C:\a.ideanest" }, AlwaysExists);

        var ok = vm.TryChooseOpen(@"C:\a.ideanest");

        Assert.True(ok);
        Assert.Equal(StartupChoice.Open, vm.Choice);
        Assert.Equal(@"C:\a.ideanest", vm.SelectedPath);
    }

    [Fact]
    public void TryChooseOpen_MissingPath_ReturnsFalse_LeavesChoiceUnchanged()
    {
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\a.ideanest" };
        var vm = new StartupViewModel(new[] { @"C:\a.ideanest" }, present.Contains);

        var ok = vm.TryChooseOpen(@"C:\gone.ideanest");

        Assert.False(ok);
        Assert.Equal(StartupChoice.Cancel, vm.Choice);
        Assert.Null(vm.SelectedPath);
    }

    [Fact]
    public void TryChooseOpen_EmptyPath_ReturnsFalse()
    {
        var vm = new StartupViewModel(Array.Empty<string>(), AlwaysExists);

        Assert.False(vm.TryChooseOpen(""));
        Assert.False(vm.TryChooseOpen("   "));
        Assert.Equal(StartupChoice.Cancel, vm.Choice);
    }

    // ── Items mutation ───────────────────────────────────────────────────────

    [Fact]
    public void RemoveItem_RemovesFromCollection()
    {
        var vm = new StartupViewModel(
            new[] { @"C:\a.ideanest", @"C:\b.ideanest" },
            AlwaysExists);
        var first = vm.Items[0];

        vm.RemoveItem(first);

        Assert.Single(vm.Items);
        Assert.DoesNotContain(first, vm.Items);
    }

    [Fact]
    public void ClearItems_EmptiesCollection()
    {
        var vm = new StartupViewModel(
            new[] { @"C:\a.ideanest", @"C:\b.ideanest" },
            AlwaysExists);

        vm.ClearItems();

        Assert.Empty(vm.Items);
        Assert.False(vm.HasItems);
    }
}
