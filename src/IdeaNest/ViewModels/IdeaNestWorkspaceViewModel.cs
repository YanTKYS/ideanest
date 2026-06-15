using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using IdeaNest.Commands;
using IdeaNest.Models;
using IdeaNest.Services;
using IdeaNest.Views;

namespace IdeaNest.ViewModels;

public class IdeaNestWorkspaceViewModel : ViewModelBase
{
    private Workspace _workspace = new();
    private CardOperationsService _cardOps = null!; // assigned in constructor, re-created by ReloadFromWorkspace
    private TagManagementService _tagMgmt = null!; // assigned in constructor
    private DispatcherTimer? _statusClearTimer;
    private string _statusMessage = string.Empty;
    private readonly WorkspaceUiService _ui;

    public CardDisplayViewModel CardDisplay { get; }
    public FilterViewModel Filter { get; }
    public TagPanelViewModel TagPanel { get; }
    public ExportViewModel Export { get; }

    public ObservableCollection<IdeaCardViewModel> AllCards { get; } = new();
    public ObservableCollection<IdeaCardViewModel> VisibleCards { get; } = new();
    public ObservableCollection<string> AvailableTags { get; } = new();

    /// <summary>
    /// Full, unfiltered tag list. Used by the tag management window so that
    /// rename/delete operations always cover every tag regardless of the side
    /// panel's TagSearch filter.
    /// </summary>
    public ObservableCollection<TagItemViewModel> TagItems => TagPanel.AllItems;

    /// <summary>
    /// TagSearch-filtered tag list. Used by the side panel ListBox.
    /// </summary>
    public ObservableCollection<TagItemViewModel> VisibleTagPanelItems => TagPanel.VisibleItems;

    public ObservableCollection<SortOptionViewModel> SortOptions { get; } = new()
    {
        new SortOptionViewModel("UpdatedDesc", "更新日時順"),
        new SortOptionViewModel("CreatedDesc", "作成日時順"),
        new SortOptionViewModel("TitleAsc",    "タイトル順"),
        new SortOptionViewModel("Shuffle",     "シャッフル"),
    };

    public ObservableCollection<ColorFilterItemViewModel> ColorItems { get; } = new()
    {
        new ColorFilterItemViewModel("white",  "白"),
        new ColorFilterItemViewModel("yellow", "黄"),
        new ColorFilterItemViewModel("green",  "緑"),
        new ColorFilterItemViewModel("blue",   "青"),
        new ColorFilterItemViewModel("pink",   "ピンク"),
        new ColorFilterItemViewModel("purple", "紫"),
        new ColorFilterItemViewModel("orange", "オレンジ"),
        new ColorFilterItemViewModel("gray",   "グレー"),
    };


    // ── Filter state: forward to Filter sub-ViewModel ────────────────────────
    // Logic (callback invocation, HasActiveFilter) lives in FilterViewModel.
    // These thin forwards keep existing XAML bindings working without change.

    public string SearchText      { get => Filter.SearchText;    set => Filter.SearchText = value; }
    public string SelectedTag     { get => Filter.SelectedTag;   set => Filter.SelectedTag = value; }
    public string SelectedColor   { get => Filter.SelectedColor; set => Filter.SelectedColor = value; }
    public bool   ShowArchived    { get => Filter.ShowArchived;  set => Filter.ShowArchived = value; }
    public bool   HasActiveFilter => Filter.HasActiveFilter;

    // ── Tag panel: forward to TagPanel sub-ViewModel ─────────────────────────
    // Logic (open state, button labels, tag search + filtering) lives in TagPanelViewModel.
    // Thin forwards keep existing XAML bindings working without change.

    public bool IsTagPanelOpen
    {
        get => TagPanel.IsTagPanelOpen;
        set => TagPanel.IsTagPanelOpen = value;
    }

    public string TagPanelButtonLabel => TagPanel.TagPanelButtonLabel;
    public string TagPanelButtonTip   => TagPanel.TagPanelButtonTip;

    // ── Card display: forward to CardDisplay sub-ViewModel ────────────────────
    // Logic (dimension calculations, validation, shuffle) lives in CardDisplayViewModel.
    // These thin forwards keep existing XAML bindings working without change.

    public string CardSize       { get => CardDisplay.CardSize;       set => CardDisplay.CardSize = value; }
    public string CardHeightMode { get => CardDisplay.CardHeightMode; set => CardDisplay.CardHeightMode = value; }
    public string SortMode       { get => CardDisplay.SortMode;       set => CardDisplay.SortMode = value; }

    public double CardWidth     => CardDisplay.CardWidth;
    public double CardHeight    => CardDisplay.CardHeight;
    public double CardMinHeight => CardDisplay.CardMinHeight;
    public double CardMaxHeight => CardDisplay.CardMaxHeight;

    public bool IsCardSizeSmall  => CardDisplay.IsCardSizeSmall;
    public bool IsCardSizeMedium => CardDisplay.IsCardSizeMedium;
    public bool IsCardSizeLarge  => CardDisplay.IsCardSizeLarge;
    public bool IsCardHeightFixed => CardDisplay.IsCardHeightFixed;
    public bool IsCardHeightAuto  => CardDisplay.IsCardHeightAuto;
    public bool IsShuffleMode     => CardDisplay.IsShuffleMode;

    public WorkspaceSettings Settings => _workspace.Settings;

    public ICommand AddIdeaCommand { get; }
    public ICommand EditIdeaCommand { get; }
    public ICommand PreviewIdeaCommand { get; }
    public RelayCommand RandomPreviewCommand { get; }
    public ICommand DeleteIdeaCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand ToggleArchiveCommand { get; }
    public ICommand SelectTagCommand { get; }
    public ICommand ClearTagCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ClearColorCommand { get; }
    public ICommand ManageTagsCommand { get; }
    public ICommand ExportMarkdownCommand { get; }
    public ICommand CopyCardMarkdownCommand { get; }
    public ICommand CopyAllMarkdownCommand { get; }
    public ICommand ExportNoteNestCommand { get; }
    public ICommand CopyNoteNestCommand { get; }
    public ICommand ToggleTagPanelCommand { get; }
    public ICommand SetCardSizeCommand { get; }
    public ICommand SetCardHeightModeCommand { get; }
    public ICommand ReshuffleCommand { get; }
    public IdeaNestWorkspaceHostCommands HostCommands { get; private set; } = new();
    public ICommand? NewWorkspaceCommand => HostCommands.NewWorkspace;
    public ICommand? OpenCommand => HostCommands.Open;
    public ICommand? SaveCommand => HostCommands.Save;
    public ICommand? SaveAsCommand => HostCommands.SaveAs;
    public string DisplayName => _workspace.WorkspaceName;
    public ExportFilterContext CurrentFilterContext => new(
        SearchText, SelectedTag, SelectedColor, ShowArchived);

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public int TotalCount => AllCards.Count;
    public int VisibleCount => VisibleCards.Count;
    public int VisibleCardCount => VisibleCount;

    public string CountText
    {
        get
        {
            // Show fraction whenever the visible set differs from total,
            // including the implicit "archived hidden" case.
            if (VisibleCount == TotalCount)
            {
                return $"{TotalCount}件";
            }
            return $"{VisibleCount}件 / 全{TotalCount}件";
        }
    }

    public bool ShowEmptyState => VisibleCount == 0;

    public string EmptyStateTitle
    {
        get
        {
            if (TotalCount == 0) return "まだアイデアがありません";
            if (HasActiveFilter) return "条件に一致するカードがありません";
            // No filter, total > 0, but visible == 0 means everything is archived
            // and ShowArchived is OFF.
            return "表示できるカードがありません";
        }
    }

    public string EmptyStateMessage
    {
        get
        {
            if (TotalCount == 0)
                return "右下の「＋」ボタン (または Ctrl+Shift+N) から最初のアイデアを追加できます。";
            if (HasActiveFilter)
                return "検索語やタグを変更してください。";
            return "「アーカイブを表示」を有効にすると、アーカイブ済みカードが見られます。";
        }
    }

    public IdeaNestWorkspaceViewModel() : this(new WorkspaceUiService()) { }

    public IdeaNestWorkspaceViewModel(WorkspaceUiService ui)
    {
        _ui = ui;
        CardDisplay = new CardDisplayViewModel(RefreshVisible, OnCardDisplayChanged);
        CardDisplay.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);

        Filter = new FilterViewModel(RefreshVisible, OnFilterChanged);
        Filter.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);

        TagPanel = new TagPanelViewModel(OnTagPanelChanged, tag => SelectedTag = tag);
        // Relay IsTagPanelOpen / TagPanelButtonLabel / TagPanelButtonTip so existing
        // XAML bindings continue to work via the forwarding properties above.
        TagPanel.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);

        Export = new ExportViewModel(
            getVisibleCards: () => VisibleCards,
            getFilterContext: () => new ExportFilterContext(
                SearchText, SelectedTag, SelectedColor, ShowArchived),
            platform: new WpfExportPlatform(_ui),
            showStatus: ShowStatus);

        AddIdeaCommand         = new RelayCommand(_ => AddIdea());
        EditIdeaCommand        = new RelayCommand(p => EditIdea(p as IdeaCardViewModel));
        PreviewIdeaCommand     = new RelayCommand(p => PreviewIdea(p as IdeaCardViewModel));
        RandomPreviewCommand   = new RelayCommand(_ => RandomPreview(), _ => VisibleCards.Count > 0);
        DeleteIdeaCommand      = new RelayCommand(p => DeleteIdea(p as IdeaCardViewModel));
        TogglePinCommand       = new RelayCommand(p => TogglePin(p as IdeaCardViewModel));
        ToggleArchiveCommand   = new RelayCommand(p => ToggleArchive(p as IdeaCardViewModel));
        SelectTagCommand       = new RelayCommand(p => TagPanel.SelectTag(p as string ?? string.Empty));
        ClearTagCommand        = new RelayCommand(_ => SelectedTag = string.Empty);
        ClearSearchCommand     = new RelayCommand(_ => SearchText = string.Empty);
        ClearColorCommand         = new RelayCommand(_ => SelectedColor = string.Empty);
        ManageTagsCommand         = new RelayCommand(_ => OpenTagManagement());
        ExportMarkdownCommand     = new RelayCommand(_ => Export.ExportMarkdown());
        CopyCardMarkdownCommand   = new RelayCommand(p => Export.CopyCardMarkdown(p as IdeaCardViewModel));
        CopyAllMarkdownCommand    = new RelayCommand(_ => Export.CopyAllMarkdown());
        ExportNoteNestCommand     = new RelayCommand(_ => Export.ExportNoteNest());
        CopyNoteNestCommand       = new RelayCommand(_ => Export.CopyNoteNest());
        ToggleTagPanelCommand     = new RelayCommand(_ => TagPanel.Toggle());
        SetCardSizeCommand        = new RelayCommand(p => CardDisplay.CardSize = p as string ?? "medium");
        SetCardHeightModeCommand  = new RelayCommand(p => CardDisplay.CardHeightMode = p as string ?? "fixed");
        ReshuffleCommand          = new RelayCommand(_ =>
            CardDisplay.Reshuffle(AllCards.Where(c => !c.IsPinned).Select(c => c.Id)));

        _cardOps = CreateCardOps();
        _tagMgmt = new TagManagementService(
            AllCards,
            getSelectedTag: () => SelectedTag,
            setSelectedTag: t => SelectedTag = t,
            onDirty: MarkDirty,
            onRefreshTags: RefreshTags,
            onRefreshVisible: RefreshVisible);
    }

    private CardOperationsService CreateCardOps() => new(
        _workspace.Ideas,
        AllCards,
        MarkDirty,
        RefreshTags,
        RefreshVisible);

    private void RaiseCountAndEmptyStateChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(VisibleCardCount));
        OnPropertyChanged(nameof(CurrentFilterContext));
        OnPropertyChanged(nameof(HasActiveFilter));
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
    }

    private void AddIdea()
    {
        var draft = new Idea();
        var vm = new EditIdeaViewModel(draft);
        var dlg = new EditIdeaWindow
        {
            Title = "新規アイデア",
            DataContext = vm,
            Owner = _ui.Owner,
        };
        if (dlg.ShowDialog() != true) return;

        vm.ApplyTo(draft);
        _cardOps.CommitAdd(draft);
    }

    private void PreviewIdea(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var cards = VisibleCards.ToList();
        var index = cards.IndexOf(card);
        if (index < 0) index = 0;

        // Preview owns any nested dialog (Edit) so the nested ShowDialog stacks
        // on top of the preview itself rather than behind it.
        PreviewIdeaWindow? dlg = null;
        dlg = new PreviewIdeaWindow(
            cards,
            index,
            onEdit: c => EditIdea(c, dlg),
            onTogglePin: c => TogglePin(c),
            onToggleArchive: c => ToggleArchive(c),
            onCopyMarkdown: c => Export.CopyCardMarkdown(c))
        {
            Owner = _ui.Owner,
        };
        // Preview itself does not mutate state — IsDirty is only set by the
        // delegated actions (EditIdea / TogglePin / ToggleArchive) when invoked.
        dlg.ShowDialog();
    }

    private void RandomPreview()
    {
        if (VisibleCards.Count == 0) return;
        var card = VisibleCards[new Random().Next(VisibleCards.Count)];
        PreviewIdea(card);
    }

    private void EditIdea(IdeaCardViewModel? card, Window? owner = null)
    {
        if (card == null) return;
        var vm = new EditIdeaViewModel(card.Model);
        var dlg = new EditIdeaWindow
        {
            Title = "アイデア編集",
            DataContext = vm,
            Owner = owner ?? _ui.Owner,
        };
        if (dlg.ShowDialog() != true) return;
        vm.ApplyTo(card.Model);
        _cardOps.CommitEdit(card);
    }

    private void DeleteIdea(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var ok = ConfirmWindow.ShowOkCancel(
            _ui.Owner,
            "このカードを削除しますか？",
            $"「{card.DisplayTitle}」を削除します。削除すると元に戻せません。\n\n" +
            "不要な場合は、削除ではなくアーカイブ (📥) も検討してください。",
            primaryText: "削除",
            cancelText: "キャンセル");
        if (ok != ConfirmResult.Primary) return;
        _cardOps.CommitDelete(card);
    }

    private void TogglePin(IdeaCardViewModel? card)
    {
        if (card == null) return;
        _cardOps.TogglePin(card);
    }

    private void ToggleArchive(IdeaCardViewModel? card)
    {
        if (card == null) return;
        _cardOps.ToggleArchive(card);
    }

    public event EventHandler? DirtyRequested;

    public void MarkDirty() => DirtyRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Supplies standalone AppShell commands used by the menu hosted in this
    /// first-stage workspace view. The workspace never performs file I/O itself.
    /// </summary>
    public void SetHostCommands(IdeaNestWorkspaceHostCommands commands)
    {
        HostCommands = commands ?? new IdeaNestWorkspaceHostCommands();
        OnPropertyChanged(nameof(HostCommands));
        OnPropertyChanged(nameof(NewWorkspaceCommand));
        OnPropertyChanged(nameof(OpenCommand));
        OnPropertyChanged(nameof(SaveCommand));
        OnPropertyChanged(nameof(SaveAsCommand));
    }

    public void SetOwnerResolver(Func<Window?> resolver) => _ui.SetOwnerResolver(resolver);

    private void OnFilterChanged()
    {
        Filter.SyncToSettings(_workspace.Settings);
        MarkDirty();
    }

    private void OnTagPanelChanged()
    {
        TagPanel.SyncToSettings(_workspace.Settings);
        MarkDirty();
    }

    private void OnCardDisplayChanged()
    {
        CardDisplay.SyncToSettings(_workspace.Settings);
        MarkDirty();
    }

    public void LoadFromWorkspace(Workspace workspace)
    {
        _workspace = workspace ?? new Workspace();
        ReloadFromWorkspace();
        OnPropertyChanged(nameof(DisplayName));
    }

    public Workspace BuildWorkspaceForSave()
    {
        SyncSettings();
        return _workspace;
    }

    public void SyncSettings()
    {
        Filter.SyncToSettings(_workspace.Settings);
        TagPanel.SyncToSettings(_workspace.Settings);
        CardDisplay.SyncToSettings(_workspace.Settings);
    }

    private void ReloadFromWorkspace()
    {
        _cardOps = CreateCardOps();
        AllCards.Clear();
        foreach (var idea in _workspace.Ideas)
        {
            AllCards.Add(new IdeaCardViewModel(idea));
        }
        // Filter, TagPanel, and CardDisplay state are owned by their sub-ViewModels.
        // LoadFromSettings fires all derived PropertyChanged notifications,
        // which are relayed to MainViewModel via the subscribed handlers.
        Filter.LoadFromSettings(_workspace.Settings);
        TagPanel.LoadFromSettings(_workspace.Settings);
        CardDisplay.LoadFromSettings(_workspace.Settings);
        RefreshTags();
        RefreshVisible();
    }

    private void RefreshTags()
    {
        var tagItems = TagSyncService.ComputeTagItems(AllCards);
        AvailableTags.Clear();
        foreach (var item in tagItems) AvailableTags.Add(item.Name);
        TagPanel.SetAllItems(tagItems);
    }

    private void OpenTagManagement()
    {
        var dlg = new Views.TagManagementWindow(this)
        {
            Owner = _ui.Owner,
        };
        dlg.ShowDialog();
    }

    private void ShowStatus(string message)
    {
        StatusMessage = message;
        _statusClearTimer?.Stop();
        _statusClearTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };
        _statusClearTimer.Tick += (_, _) =>
        {
            StatusMessage = string.Empty;
            _statusClearTimer?.Stop();
        };
        _statusClearTimer.Start();
    }

    public void RenameTag(string oldName, string newName) => _tagMgmt.RenameTag(oldName, newName);

    public void DeleteTag(string tagName) => _tagMgmt.DeleteTag(tagName);

    /// <summary>
    /// Creates a new card from the current clipboard text. No-op when the
    /// clipboard has no text. Used by Ctrl+V on the main card area.
    /// </summary>
    public bool PasteAsNewCard()
    {
        string text;
        try
        {
            text = _ui.GetClipboardText() ?? string.Empty;
        }
        catch
        {
            // Clipboard access can occasionally fail with COMException; treat as no-op.
            return false;
        }
        if (string.IsNullOrWhiteSpace(text)) return false;
        var ok = _cardOps.CommitAddFromText(text);
        if (ok) ShowStatus("クリップボードのテキストからカードを作成しました");
        return ok;
    }

    /// <summary>
    /// Creates one card per dropped text file. Reads each file as UTF-8;
    /// failures are collected and surfaced via a single MessageBox at the end
    /// so a bad file does not abort the rest.
    /// </summary>
    public int CreateCardsFromFiles(IEnumerable<string> filePaths)
    {
        var created = 0;
        var errors = new List<string>();
        foreach (var path in filePaths)
        {
            try
            {
                var body = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var title = Path.GetFileNameWithoutExtension(path);
                if (_cardOps.CommitAddFromFileContent(title, body)) created++;
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            _ui.ShowWarning("次のファイルを読み込めませんでした:\n\n" + string.Join("\n", errors));
        }
        if (created > 0) ShowStatus($"{created}件のテキストファイルからカードを作成しました");
        return created;
    }

    private void RefreshVisible()
    {
        var query = (SearchText ?? string.Empty).Trim();
        var tag = (SelectedTag ?? string.Empty).Trim();
        var color = (SelectedColor ?? string.Empty).Trim();

        IEnumerable<IdeaCardViewModel> items = AllCards;

        if (!ShowArchived)
        {
            items = items.Where(c => !c.IsArchived);
        }

        if (!string.IsNullOrEmpty(tag))
        {
            items = items.Where(c => c.Tags.Any(t => string.Equals(t, tag, StringComparison.Ordinal)));
        }

        if (!string.IsNullOrEmpty(color))
        {
            items = items.Where(c => string.Equals(
                string.IsNullOrWhiteSpace(c.Color) ? "yellow" : c.Color,
                color, StringComparison.Ordinal));
        }

        if (!string.IsNullOrEmpty(query))
        {
            items = items.Where(c =>
                (c.Title ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase)
                || (c.Body ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase)
                || c.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var pinned = items.Where(c => c.IsPinned)
                          .OrderByDescending(c => c.UpdatedAt);

        var rest = items.Where(c => !c.IsPinned);
        rest = CardDisplay.SortMode switch
        {
            "CreatedDesc" => rest.OrderByDescending(c => c.CreatedAt),
            "TitleAsc"    => rest.OrderBy(c => c.DisplayTitle, StringComparer.CurrentCulture),
            "Shuffle"     => CardDisplay.OrderByShuffle(rest, AllCards),
            _             => rest.OrderByDescending(c => c.UpdatedAt),
        };

        var ordered = pinned.Concat(rest).ToList();

        VisibleCards.Clear();
        foreach (var c in ordered) VisibleCards.Add(c);

        RaiseCountAndEmptyStateChanged();
        RandomPreviewCommand.RaiseCanExecuteChanged();
    }

}
