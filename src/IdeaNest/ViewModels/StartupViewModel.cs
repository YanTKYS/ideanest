using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using IdeaNest.Services;

namespace IdeaNest.ViewModels;

public enum StartupChoice
{
    /// <summary>The dialog was dismissed without selecting an action.</summary>
    Cancel,

    /// <summary>The user chose to start a new (unsaved) workspace.</summary>
    New,

    /// <summary>The user chose to open a recent file (see SelectedPath).</summary>
    Open,
}

public sealed class RecentFileItem
{
    public string FullPath { get; }
    public string DisplayName { get; }

    public RecentFileItem(string fullPath)
    {
        FullPath = fullPath;
        DisplayName = Path.GetFileName(fullPath);
    }
}

/// <summary>
/// Holds state for the startup dialog: which recent files to show, the user's
/// selection, and the outcome (New/Open/Cancel). The dialog code-behind drives
/// it; tests exercise it directly without touching WPF.
/// </summary>
public class StartupViewModel
{
    private readonly Func<string, bool> _fileExists;

    public ObservableCollection<RecentFileItem> Items { get; } = new();
    public StartupChoice Choice { get; private set; } = StartupChoice.Cancel;
    public string? SelectedPath { get; private set; }

    public StartupViewModel(IEnumerable<string> recentFiles, Func<string, bool>? fileExists = null)
    {
        if (recentFiles is null) throw new ArgumentNullException(nameof(recentFiles));
        _fileExists = fileExists ?? File.Exists;
        foreach (var path in RecentFilesService.FilterExisting(recentFiles, _fileExists))
        {
            Items.Add(new RecentFileItem(path));
        }
    }

    public bool HasItems => Items.Count > 0;

    public void ChooseNew()
    {
        Choice = StartupChoice.New;
        SelectedPath = null;
    }

    public void Cancel()
    {
        Choice = StartupChoice.Cancel;
        SelectedPath = null;
    }

    /// <summary>
    /// Mark <paramref name="path"/> as the chosen file. Returns false (and
    /// leaves <see cref="Choice"/> unchanged) when the file is no longer on
    /// disk — the caller should drop the entry and re-prompt.
    /// </summary>
    public bool TryChooseOpen(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!_fileExists(path)) return false;
        Choice = StartupChoice.Open;
        SelectedPath = path;
        return true;
    }

    public void RemoveItem(RecentFileItem item) => Items.Remove(item);

    public void ClearItems() => Items.Clear();
}
