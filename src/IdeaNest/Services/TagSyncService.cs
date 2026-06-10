using System.Collections.Generic;
using System.Linq;
using IdeaNest.ViewModels;

namespace IdeaNest.Services;

/// <summary>
/// WPF-free tag aggregation logic extracted from MainViewModel.RefreshTags().
/// </summary>
public static class TagSyncService
{
    /// <summary>
    /// Computes tag names and their usage counts from the full card collection.
    /// Returns items sorted alphabetically by tag name (Ordinal comparison).
    /// Blank or whitespace-only tag strings are excluded.
    /// </summary>
    public static IReadOnlyList<TagItemViewModel> ComputeTagItems(
        IEnumerable<IdeaCardViewModel> allCards)
    {
        return allCards
            .SelectMany(c => c.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .GroupBy(t => t, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new TagItemViewModel(g.Key, g.Count()))
            .ToList()
            .AsReadOnly();
    }
}
