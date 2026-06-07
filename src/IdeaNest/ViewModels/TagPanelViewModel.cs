using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IdeaNest.Models;

namespace IdeaNest.ViewModels;

/// <summary>
/// Owns the tag panel's display state: open/closed, tag search text,
/// and the filtered list of tags shown in the panel.
/// Has no WPF dependencies; decoupled via callbacks.
/// MainViewModel holds an instance and forwards property changes to the UI layer.
/// </summary>
public class TagPanelViewModel : ViewModelBase
{
    private bool _isTagPanelOpen;
    private string _tagSearch = string.Empty;
    private List<TagItemViewModel> _allItems = new();

    private readonly Action _onMarkDirty;
    private readonly Action<string> _onTagSelected;

    public TagPanelViewModel(Action onMarkDirty, Action<string> onTagSelected)
    {
        _onMarkDirty = onMarkDirty;
        _onTagSelected = onTagSelected;
    }

    // ── Panel open state ──────────────────────────────────────────────────────

    /// <summary>
    /// Property name matches the XAML binding so the relay
    /// (TagPanel.PropertyChanged → MainViewModel.PropertyChanged) keeps existing
    /// {Binding IsTagPanelOpen} / {Binding IsChecked=IsTagPanelOpen} working.
    /// </summary>
    public bool IsTagPanelOpen
    {
        get => _isTagPanelOpen;
        set
        {
            if (SetField(ref _isTagPanelOpen, value))
            {
                OnPropertyChanged(nameof(TagPanelButtonLabel));
                OnPropertyChanged(nameof(TagPanelButtonTip));
                _onMarkDirty();
            }
        }
    }

    public string TagPanelButtonLabel => _isTagPanelOpen ? "タグ ◀" : "タグ ▶";
    public string TagPanelButtonTip   => _isTagPanelOpen ? "タグパネルを閉じる" : "タグパネルを表示";

    public void Toggle() => IsTagPanelOpen = !IsTagPanelOpen;

    // ── Tag search (filters VisibleItems to tags whose Name contains the query) ─

    public string TagSearch
    {
        get => _tagSearch;
        set
        {
            if (SetField(ref _tagSearch, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasTagSearch));
                RefreshVisible();
            }
        }
    }

    /// <summary>True when TagSearch is non-empty after trimming.</summary>
    public bool HasTagSearch => !string.IsNullOrEmpty(_tagSearch.Trim());

    public void ClearTagSearch() => TagSearch = string.Empty;

    // ── Tag list ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The full, unfiltered tag list. Stable reference; updated in place by SetAllItems.
    /// Consumers that must see every tag regardless of TagSearch (e.g. the tag
    /// management window) bind here.
    /// </summary>
    public ObservableCollection<TagItemViewModel> AllItems { get; } = new();

    /// <summary>
    /// The TagSearch-filtered view used by the side panel ListBox.
    /// Stable reference; Clear + re-Add drives CollectionChanged.
    /// </summary>
    public ObservableCollection<TagItemViewModel> VisibleItems { get; } = new();

    /// <summary>
    /// Replaces the full tag list and re-filters VisibleItems.
    /// Called by MainViewModel.RefreshTags() after every card-list mutation.
    /// </summary>
    public void SetAllItems(IEnumerable<TagItemViewModel> items)
    {
        _allItems = items.ToList();
        AllItems.Clear();
        foreach (var item in _allItems) AllItems.Add(item);
        RefreshVisible();
    }

    private void RefreshVisible()
    {
        var query = _tagSearch.Trim();
        var filtered = string.IsNullOrEmpty(query)
            ? _allItems
            : _allItems
                .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        VisibleItems.Clear();
        foreach (var item in filtered) VisibleItems.Add(item);
    }

    // ── Tag selection ─────────────────────────────────────────────────────────

    /// <summary>
    /// Notifies the host (MainViewModel) that the user selected a tag.
    /// The host routes this to FilterViewModel.SelectedTag via the callback.
    /// </summary>
    public void SelectTag(string tag) => _onTagSelected(tag ?? string.Empty);

    // ── Settings sync ─────────────────────────────────────────────────────────

    public void SyncToSettings(WorkspaceSettings settings)
    {
        settings.TagPanelOpen = _isTagPanelOpen;
    }

    /// <summary>
    /// Restores open state from settings without invoking callbacks.
    /// Also resets TagSearch — it's a session-local UI filter scoped to a single
    /// workspace, so a previous workspace's filter must not hide tags in the new one.
    /// The caller (ReloadFromWorkspace) is responsible for calling RefreshTags()
    /// afterwards so AllItems / VisibleItems are rebuilt with the latest card data.
    /// </summary>
    public void LoadFromSettings(WorkspaceSettings settings)
    {
        _isTagPanelOpen = settings.TagPanelOpen;

        bool tagSearchChanged = _tagSearch.Length > 0;
        _tagSearch = string.Empty;

        OnPropertyChanged(nameof(IsTagPanelOpen));
        OnPropertyChanged(nameof(TagPanelButtonLabel));
        OnPropertyChanged(nameof(TagPanelButtonTip));
        if (tagSearchChanged)
        {
            OnPropertyChanged(nameof(TagSearch));
            OnPropertyChanged(nameof(HasTagSearch));
        }
    }
}
