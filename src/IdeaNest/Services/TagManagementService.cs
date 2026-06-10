using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IdeaNest.ViewModels;

namespace IdeaNest.Services;

/// <summary>
/// WPF-free tag rename / delete / merge logic extracted from MainViewModel.
/// Mutates the supplied cards in place, drives the selected-tag adjustment via
/// callbacks, and fans out the standard dirty / refresh-tags / refresh-visible
/// notifications. Tag rename collapses into a merge automatically when the
/// destination tag already exists on the same card (via NormalizeTags).
/// </summary>
public class TagManagementService
{
    private readonly ObservableCollection<IdeaCardViewModel> _allCards;
    private readonly Func<string> _getSelectedTag;
    private readonly Action<string> _setSelectedTag;
    private readonly Action _onDirty;
    private readonly Action _onRefreshTags;
    private readonly Action _onRefreshVisible;

    public TagManagementService(
        ObservableCollection<IdeaCardViewModel> allCards,
        Func<string> getSelectedTag,
        Action<string> setSelectedTag,
        Action onDirty,
        Action onRefreshTags,
        Action onRefreshVisible)
    {
        _allCards = allCards;
        _getSelectedTag = getSelectedTag;
        _setSelectedTag = setSelectedTag;
        _onDirty = onDirty;
        _onRefreshTags = onRefreshTags;
        _onRefreshVisible = onRefreshVisible;
    }

    /// <summary>
    /// Renames every occurrence of <paramref name="oldName"/> to <paramref name="newName"/>.
    /// When a card already carries <paramref name="newName"/>, NormalizeTags collapses the
    /// duplicate — this is the merge path.
    /// Returns false without firing any callbacks when the new name normalises to empty or
    /// to the same value as <paramref name="oldName"/>, or when no card and no selected-tag
    /// filter actually carry <paramref name="oldName"/>.
    /// Returns true when at least one card or the selected-tag filter was mutated.
    /// </summary>
    public bool RenameTag(string oldName, string newName)
    {
        newName = WorkspaceService.NormalizeTag(newName);
        if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return false;

        bool mutated = false;

        foreach (var card in _allCards)
        {
            if (card.Tags.Any(t => string.Equals(t, oldName, StringComparison.Ordinal)))
            {
                card.Tags = WorkspaceService.NormalizeTags(
                    card.Tags.Select(t => string.Equals(t, oldName, StringComparison.Ordinal) ? newName : t));
                card.Touch();
                card.OnExternalUpdate();
                mutated = true;
            }
        }

        if (string.Equals(_getSelectedTag(), oldName, StringComparison.Ordinal))
        {
            _setSelectedTag(newName);
            mutated = true;
        }

        if (!mutated) return false;

        _onDirty();
        _onRefreshTags();
        _onRefreshVisible();
        return true;
    }

    /// <summary>
    /// Removes <paramref name="tagName"/> from every card that carries it.
    /// Clears the selected tag filter when it matches.
    /// </summary>
    public void DeleteTag(string tagName)
    {
        foreach (var card in _allCards)
        {
            if (card.Tags.Any(t => string.Equals(t, tagName, StringComparison.Ordinal)))
            {
                card.Tags = card.Tags
                    .Where(t => !string.Equals(t, tagName, StringComparison.Ordinal))
                    .ToList();
                card.Touch();
                card.OnExternalUpdate();
            }
        }

        if (string.Equals(_getSelectedTag(), tagName, StringComparison.Ordinal))
        {
            _setSelectedTag(string.Empty);
        }

        _onDirty();
        _onRefreshTags();
        _onRefreshVisible();
    }
}
