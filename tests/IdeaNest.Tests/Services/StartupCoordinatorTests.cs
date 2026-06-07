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
    public void Resolve_OnlyEmptyOrWhitespaceArgs_ReturnsShowDialog()
    {
        var action = StartupCoordinator.Resolve(new[] { "", "   ", null! }, AlwaysExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    // ── DirectOpen: first arg exists ─────────────────────────────────────────

    [Fact]
    public void Resolve_FirstArgExistsAsIdeaNestFile_ReturnsDirectOpen()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\notes\ideas.ideanest" }, AlwaysExists);

        Assert.Equal(StartupActionKind.DirectOpen, action.Kind);
        Assert.Equal(@"C:\notes\ideas.ideanest", action.Path);
    }

    [Fact]
    public void Resolve_FirstArgExistsAsNonIdeaNestFile_ReturnsDirectOpen()
    {
        // No extension filter — any existing first arg triggers DirectOpen.
        // This mirrors the old App.OnStartup behavior where File.Exists(args[0])
        // was the only check, allowing e.g. workspace.json opened by the in-app
        // "All files (*.*)" option to also be passed on the command line.
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\notes\workspace.json" }, AlwaysExists);

        Assert.Equal(StartupActionKind.DirectOpen, action.Kind);
        Assert.Equal(@"C:\notes\workspace.json", action.Path);
    }

    [Fact]
    public void Resolve_FirstArgDoesNotExist_ReturnsShowDialog()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\nope.ideanest" }, NeverExists);

        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    // ── Multiple args: only args[0] is examined ──────────────────────────────

    [Fact]
    public void Resolve_MultipleArgs_OnlyFirstArgIsExamined()
    {
        // Second arg is an existing .ideanest file, but it should be ignored.
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\notes\second.ideanest",
        };

        var action = StartupCoordinator.Resolve(
            new[] { @"C:\notes\missing.ideanest", @"C:\notes\second.ideanest" },
            present.Contains);

        // First arg does not exist → ShowDialog; second arg is not consulted.
        Assert.Equal(StartupActionKind.ShowDialog, action.Kind);
    }

    [Fact]
    public void Resolve_MultipleArgs_FirstArgExists_ReturnsDirectOpenForFirstArg()
    {
        var action = StartupCoordinator.Resolve(
            new[] { @"C:\notes\first.ideanest", @"C:\notes\second.ideanest" },
            AlwaysExists);

        Assert.Equal(StartupActionKind.DirectOpen, action.Kind);
        Assert.Equal(@"C:\notes\first.ideanest", action.Path);
    }

    [Fact]
    public void Resolve_FlagStyleFirstArg_ReturnsShowDialog()
    {
        // A flag like "--some-option" is not a file that exists on disk;
        // the existence check fails and the dialog is shown.
        var action = StartupCoordinator.Resolve(
            new[] { "--some-option", @"C:\notes\ideas.ideanest" },
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
