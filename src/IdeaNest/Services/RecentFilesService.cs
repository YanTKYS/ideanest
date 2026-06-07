using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IdeaNest.Services;

/// <summary>
/// Pure list-manipulation logic for the recent-files history. Persistence
/// (settings.json) is handled by <see cref="AppSettingsService"/>; this class
/// only operates on in-memory lists so it can be unit-tested without disk I/O.
/// </summary>
public static class RecentFilesService
{
    public const int MaxRecentFiles = 5;

    /// <summary>
    /// Push <paramref name="path"/> to the front of the list, dropping any
    /// existing entry pointing at the same file (case-insensitive). The result
    /// is capped at <see cref="MaxRecentFiles"/> entries. Empty/whitespace
    /// paths return the original list unchanged.
    /// </summary>
    public static IReadOnlyList<string> Add(IEnumerable<string> current, string path)
    {
        if (current is null) throw new ArgumentNullException(nameof(current));
        if (string.IsNullOrWhiteSpace(path)) return current.ToList();

        var result = new List<string> { path };
        result.AddRange(current.Where(p =>
            !string.IsNullOrWhiteSpace(p) &&
            !string.Equals(p, path, StringComparison.OrdinalIgnoreCase)));

        if (result.Count > MaxRecentFiles)
            result = result.Take(MaxRecentFiles).ToList();

        return result;
    }

    /// <summary>
    /// Drop any entry equal (case-insensitive) to <paramref name="path"/>.
    /// </summary>
    public static IReadOnlyList<string> Remove(IEnumerable<string> current, string path)
    {
        if (current is null) throw new ArgumentNullException(nameof(current));
        if (string.IsNullOrWhiteSpace(path)) return current.ToList();
        return current.Where(p =>
            !string.IsNullOrWhiteSpace(p) &&
            !string.Equals(p, path, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Drop entries whose files no longer exist on disk. The
    /// <paramref name="fileExists"/> delegate defaults to <see cref="File.Exists"/>;
    /// tests pass a fake to avoid touching the real filesystem.
    /// </summary>
    public static IReadOnlyList<string> FilterExisting(
        IEnumerable<string> current,
        Func<string, bool>? fileExists = null)
    {
        if (current is null) throw new ArgumentNullException(nameof(current));
        var exists = fileExists ?? File.Exists;
        return current
            .Where(p => !string.IsNullOrWhiteSpace(p) && exists(p))
            .ToList();
    }
}
