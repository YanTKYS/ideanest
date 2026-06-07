using System;
using System.Collections.Generic;
using System.Linq;
using IdeaNest.Models;

namespace IdeaNest.ViewModels;

/// <summary>
/// Owns card-list display settings: card size, card height mode, sort/shuffle order.
/// Has no WPF dependencies so it can be unit-tested cross-platform.
/// MainViewModel holds an instance and forwards property changes to the UI layer.
/// </summary>
public class CardDisplayViewModel : ViewModelBase
{
    private string _cardSize = "medium";
    private string _cardHeightMode = "fixed";
    private string _sortMode = "UpdatedDesc";
    private List<string> _shuffleOrder = new();

    private readonly Action _onRefreshVisible;
    private readonly Action _onMarkDirty;

    public CardDisplayViewModel(Action onRefreshVisible, Action onMarkDirty)
    {
        _onRefreshVisible = onRefreshVisible;
        _onMarkDirty = onMarkDirty;
    }

    // ── Card size ─────────────────────────────────────────────────────────────

    public string CardSize
    {
        get => _cardSize;
        set
        {
            var v = value switch { "small" => "small", "large" => "large", _ => "medium" };
            if (SetField(ref _cardSize, v))
            {
                OnPropertyChanged(nameof(CardWidth));
                OnPropertyChanged(nameof(CardHeight));
                OnPropertyChanged(nameof(CardMinHeight));
                OnPropertyChanged(nameof(CardMaxHeight));
                _onMarkDirty();
            }
            // Notify flags unconditionally so WPF menu/button checks stay correct after
            // re-clicking the already-active size (WPF momentarily un-checks it).
            OnPropertyChanged(nameof(IsCardSizeSmall));
            OnPropertyChanged(nameof(IsCardSizeMedium));
            OnPropertyChanged(nameof(IsCardSizeLarge));
        }
    }

    public double CardWidth => _cardSize switch { "small" => 184, "large" => 340, _ => 252 };

    // Auto height returns double.NaN so the Border sizes to its content
    // (capped by CardMinHeight / CardMaxHeight).
    public double CardHeight => _cardHeightMode == "auto"
        ? double.NaN
        : _cardSize switch { "small" => 148, "large" => 280, _ => 212 };

    public double CardMinHeight => _cardHeightMode == "auto"
        ? (_cardSize switch { "small" => 110, "large" => 180, _ => 140 })
        : 0;

    public double CardMaxHeight => _cardHeightMode == "auto"
        ? (_cardSize switch { "small" => 200, "large" => 380, _ => 280 })
        : double.PositiveInfinity;

    public bool IsCardSizeSmall  => _cardSize == "small";
    public bool IsCardSizeMedium => _cardSize == "medium";
    public bool IsCardSizeLarge  => _cardSize == "large";

    // ── Card height mode ──────────────────────────────────────────────────────

    public string CardHeightMode
    {
        get => _cardHeightMode;
        set
        {
            var v = value switch { "auto" => "auto", _ => "fixed" };
            if (SetField(ref _cardHeightMode, v))
            {
                OnPropertyChanged(nameof(CardHeight));
                OnPropertyChanged(nameof(CardMinHeight));
                OnPropertyChanged(nameof(CardMaxHeight));
                _onMarkDirty();
            }
            OnPropertyChanged(nameof(IsCardHeightFixed));
            OnPropertyChanged(nameof(IsCardHeightAuto));
        }
    }

    public bool IsCardHeightFixed => _cardHeightMode == "fixed";
    public bool IsCardHeightAuto  => _cardHeightMode == "auto";

    // ── Sort / shuffle ────────────────────────────────────────────────────────

    public string SortMode
    {
        get => _sortMode;
        set
        {
            var v = value switch
            {
                "CreatedDesc" => "CreatedDesc",
                "TitleAsc"    => "TitleAsc",
                "Shuffle"     => "Shuffle",
                _             => "UpdatedDesc",
            };
            if (SetField(ref _sortMode, v))
            {
                OnPropertyChanged(nameof(IsShuffleMode));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public bool IsShuffleMode => _sortMode == "Shuffle";

    /// <summary>
    /// Re-randomise the shuffle order and trigger a visible-card refresh.
    /// nonPinnedIds should be AllCards.Where(c => !c.IsPinned).Select(c => c.Id).
    /// </summary>
    public void Reshuffle(IEnumerable<string> nonPinnedIds)
    {
        GenerateShuffleOrder(nonPinnedIds);
        _onRefreshVisible();
    }

    public void ClearShuffleOrder() => _shuffleOrder.Clear();

    /// <summary>
    /// Read-only view of the current shuffle order. Exposed for inspection
    /// (e.g. by tests verifying that LoadFromSettings cleared the previous order).
    /// </summary>
    public IReadOnlyList<string> ShuffleOrderSnapshot => _shuffleOrder.AsReadOnly();

    /// <summary>
    /// Orders <paramref name="source"/> by the current shuffle order.
    /// allCards is used to lazily seed the order on the first call and to insert
    /// newly-added cards at the front so they are immediately visible.
    /// </summary>
    public IEnumerable<IdeaCardViewModel> OrderByShuffle(
        IEnumerable<IdeaCardViewModel> source,
        IEnumerable<IdeaCardViewModel> allCards)
    {
        if (_shuffleOrder.Count == 0)
        {
            GenerateShuffleOrder(allCards.Where(c => !c.IsPinned).Select(c => c.Id));
        }
        else
        {
            foreach (var c in allCards)
            {
                // Newly added cards surface at the top so the user sees their just-added
                // idea rather than having it buried somewhere in the existing order.
                if (!c.IsPinned && !_shuffleOrder.Contains(c.Id))
                    _shuffleOrder.Insert(0, c.Id);
            }
        }
        return source.OrderBy(c =>
        {
            var idx = _shuffleOrder.IndexOf(c.Id);
            return idx >= 0 ? idx : int.MaxValue;
        });
    }

    public void GenerateShuffleOrder(IEnumerable<string> nonPinnedIds)
    {
        var ids = nonPinnedIds.ToList();
        var rng = new Random();
        for (int i = ids.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (ids[i], ids[j]) = (ids[j], ids[i]);
        }
        _shuffleOrder = ids;
    }

    // ── Settings sync ─────────────────────────────────────────────────────────

    public void SyncToSettings(WorkspaceSettings settings)
    {
        settings.CardSize       = _cardSize;
        settings.CardHeightMode = _cardHeightMode;
        settings.SortMode       = _sortMode;
    }

    public void LoadFromSettings(WorkspaceSettings settings)
    {
        _cardSize = settings.CardSize switch { "small" => "small", "large" => "large", _ => "medium" };
        _cardHeightMode = settings.CardHeightMode switch { "auto" => "auto", _ => "fixed" };
        _sortMode = settings.SortMode switch
        {
            "CreatedDesc" => "CreatedDesc",
            "TitleAsc"    => "TitleAsc",
            "Shuffle"     => "Shuffle",
            _             => "UpdatedDesc",
        };
        ClearShuffleOrder();

        OnPropertyChanged(nameof(CardSize));
        OnPropertyChanged(nameof(CardWidth));
        OnPropertyChanged(nameof(CardHeight));
        OnPropertyChanged(nameof(CardMinHeight));
        OnPropertyChanged(nameof(CardMaxHeight));
        OnPropertyChanged(nameof(IsCardSizeSmall));
        OnPropertyChanged(nameof(IsCardSizeMedium));
        OnPropertyChanged(nameof(IsCardSizeLarge));
        OnPropertyChanged(nameof(CardHeightMode));
        OnPropertyChanged(nameof(IsCardHeightFixed));
        OnPropertyChanged(nameof(IsCardHeightAuto));
        OnPropertyChanged(nameof(SortMode));
        OnPropertyChanged(nameof(IsShuffleMode));
    }
}
