using System;
using System.Collections.Generic;
using IdeaNest.Services;
using Xunit;

namespace IdeaNest.Tests.Services;

public class StartupCoordinatorTests
{
    private static bool AlwaysExists(string _) => true;
    private static bool NeverExists(string _) => false;

    // ── No args / dialog path ────────────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyArgs_ReturnsShowDialog()
    {
        var action = StartupCoordinator.Resolve(Array.Empty<string>(), AlwaysExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
        Assert.Null(action.Path);
    }

    [Fact]
    public void Resolve_OnlyEmptyArgs_ReturnsShowDialog()
    {
        var action = StartupCoordinator.Resolve(new[] { "", "   ", null! }, AlwaysExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    // ── DirectOpen path ──────────────────────────────────────────────────────

    [Fact]
    public void Resolve_SingleIdeaNestArg_ReturnsDirectOpen()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\notes\ideas.ideanest" }, AlwaysExists);

        Assert.Equal(StartupActionKind.DirectOpen, action.Kind);
        Assert.Equal(@"C:\notes\ideas.ideanest", action.Path);
    }

    [Fact]
    public void Resolve_IdeaNestExtensionIsCaseInsensitive()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\notes\IDEAS.IDEANEST" }, AlwaysExists);

        Assert.Equal(StartupActionKind.DirectOpen, action.Kind);
    }

    [Fact]
    public void Resolve_MissingIdeaNestArg_ReturnsShowDialog()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\nope.ideanest" }, NeverExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    // ── Non-.ideanest args ───────────────────────────────────────────────────

    [Fact]
    public void Resolve_NonIdeaNestArg_ReturnsShowDialog()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\notes\readme.txt" }, AlwaysExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    [Fact]
    public void Resolve_NonIdeaNestExistingFile_IsNotOpened()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\binary.exe", @"C:\image.png" }, AlwaysExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    // ── Multiple args ────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_MultipleArgs_PicksFirstIdeaNest()
    {
        var action = StartupCoordinator.Resolve(
            new[]
            {
                @"C:\notes\skip.txt",
                @"C:\notes\first.ideanest",
                @"C:\notes\second.ideanest",
            },
            AlwaysExists);

        Assert.Equal(StartupActionKind.DirectOpen, action.Kind);
        Assert.Equal(@"C:\notes\first.ideanest", action.Path);
    }

    [Fact]
    public void Resolve_MultipleArgs_SkipsMissingIdeaNest_PicksNextExisting()
    {
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\b.ideanest",
        };

        var action = StartupCoordinator.Resolve(
            new[] { @"C:\a.ideanest", @"C:\b.ideanest" },
            present.Contains);

        Assert.Equal(StartupActionKind.DirectOpen, action.Kind);
        Assert.Equal(@"C:\b.ideanest", action.Path);
    }

    [Fact]
    public void Resolve_AllMissing_ReturnsShowDialog()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\a.ideanest", @"C:\b.ideanest" },
            NeverExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    // ── Argument validation ──────────────────────────────────────────────────

    [Fact]
    public void Resolve_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StartupCoordinator.Resolve(null!, AlwaysExists));
    }
}
