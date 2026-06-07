using System;
using System.Collections.Generic;
using System.IO;

namespace IdeaNest.Services;

public enum StartupActionKind
{
    /// <summary>Open the given .ideanest file directly, skipping the start dialog.</summary>
    DirectOpen,

    /// <summary>Show the start dialog so the user can pick from recent files or start new.</summary>
    ShowDialog,
}

public sealed record StartupAction(StartupActionKind Kind, string? Path)
{
    public static StartupAction Dialog() => new(StartupActionKind.ShowDialog, null);
    public static StartupAction Open(string path) => new(StartupActionKind.DirectOpen, path);
}

/// <summary>
/// Decides what to do at app launch based on command-line arguments. Pure
/// logic — no UI and no filesystem I/O beyond an injectable existence check.
/// </summary>
public static class StartupCoordinator
{
    private const string IdeaNestExtension = ".ideanest";

    /// <summary>
    /// Pick the first existing <c>.ideanest</c> argument. Non-<c>.ideanest</c>
    /// arguments are ignored — Windows file association is registered for
    /// <c>.ideanest</c> only, so any other input is treated as noise and the
    /// start dialog is shown instead.
    /// </summary>
    public static StartupAction Resolve(
        IEnumerable<string> args,
        Func<string, bool>? fileExists = null)
    {
        if (args is null) throw new ArgumentNullException(nameof(args));
        var exists = fileExists ?? File.Exists;

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg)) continue;
            if (!arg.EndsWith(IdeaNestExtension, StringComparison.OrdinalIgnoreCase)) continue;
            if (!exists(arg)) continue;
            return StartupAction.Open(arg);
        }

        return StartupAction.Dialog();
    }
}
