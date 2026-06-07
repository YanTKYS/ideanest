using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IdeaNest.Services;

public enum StartupActionKind
{
    /// <summary>Open the given file directly, skipping the start dialog.</summary>
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
    /// <summary>
    /// If the first argument is a path that exists on disk, return
    /// <see cref="StartupActionKind.DirectOpen"/> for that path.
    /// Any subsequent arguments are ignored, and non-existent first arguments
    /// fall through to <see cref="StartupActionKind.ShowDialog"/>.
    ///
    /// This mirrors the original <c>App.OnStartup</c> behavior: only
    /// <c>args[0]</c> is examined, and no extension filtering is applied —
    /// because the "open with" / file-association path always supplies a
    /// single argument, and the in-app Open dialog already validates content.
    /// </summary>
    public static StartupAction Resolve(
        IEnumerable<string> args,
        Func<string, bool>? fileExists = null)
    {
        if (args is null) throw new ArgumentNullException(nameof(args));
        var exists = fileExists ?? File.Exists;

        var first = args.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(first) && exists(first))
        {
            return StartupAction.Open(first);
        }

        return StartupAction.Dialog();
    }
}
