using System;
using IdeaNest.Models;

namespace IdeaNest.ViewModels;

/// <summary>
/// Owns filter state for the card list: search text, selected tag, selected color,
/// and archive visibility. Has no WPF dependencies; decoupled via callbacks.
/// MainViewModel holds an instance and forwards property changes to the UI layer.
/// </summary>
public class FilterViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private string _selectedTag = string.Empty;
    private string _selectedColor = string.Empty;
    private bool _showArchived;

    private readonly Action _onRefreshVisible;
    private readonly Action _onMarkDirty;

    public FilterViewModel(Action onRefreshVisible, Action onMarkDirty)
    {
        _onRefreshVisible = onRefreshVisible;
        _onMarkDirty = onMarkDirty;
    }

    // ── Filter state ──────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasActiveFilter));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public string SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (SetField(ref _selectedTag, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasActiveFilter));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public string SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (SetField(ref _selectedColor, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasActiveFilter));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public bool ShowArchived
    {
        get => _showArchived;
        set
        {
            if (SetField(ref _showArchived, value))
            {
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    /// <summary>
    /// True when at least one of SearchText / SelectedTag / SelectedColor is non-empty
    /// (after trimming). ShowArchived is intentionally excluded — it is a display option,
    /// not a search filter, and is handled separately in the empty-state messages.
    /// </summary>
    public bool HasActiveFilter =>
        !string.IsNullOrEmpty(_searchText.Trim()) ||
        !string.IsNullOrEmpty(_selectedTag.Trim()) ||
        !string.IsNullOrEmpty(_selectedColor.Trim());

    // ── Bulk operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Clears SearchText, SelectedTag, and SelectedColor.
    /// Each setter fires its callbacks only if the value actually changed,
    /// so calling this when already clear is a no-op.
    /// </summary>
    public void ClearFilter()
    {
        SearchText   = string.Empty;
        SelectedTag  = string.Empty;
        SelectedColor = string.Empty;
    }

    // ── Settings sync ─────────────────────────────────────────────────────────

    public void SyncToSettings(WorkspaceSettings settings)
    {
        settings.SearchText   = _searchText;
        settings.SelectedTag  = _selectedTag;
        settings.SelectedColor = _selectedColor;
        settings.ShowArchived = _showArchived;
    }

    /// <summary>
    /// Restores state from settings without invoking callbacks.
    /// The caller (ReloadFromWorkspace) is responsible for calling RefreshVisible()
    /// after all sub-ViewModels have been loaded.
    /// </summary>
    public void LoadFromSettings(WorkspaceSettings settings)
    {
        _searchText   = settings.SearchText   ?? string.Empty;
        _selectedTag  = settings.SelectedTag  ?? string.Empty;
        _selectedColor = settings.SelectedColor ?? string.Empty;
        _showArchived = settings.ShowArchived;

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedTag));
        OnPropertyChanged(nameof(SelectedColor));
        OnPropertyChanged(nameof(ShowArchived));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
