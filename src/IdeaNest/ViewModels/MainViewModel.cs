using System;
using System.Collections.Generic;
using System.Reflection;
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
using Microsoft.Win32;

namespace IdeaNest.ViewModels;

public class MainViewModel : ViewModelBase
{
    private Workspace _workspace = new();
    private CardOperationsService _cardOps = null!; // assigned in constructor, re-created by ReloadFromWorkspace
    private DispatcherTimer? _autoSaveTimer;
    private DispatcherTimer? _statusClearTimer;
    private string _statusMessage = string.Empty;
    private static readonly TimeSpan AutoSaveDelay = TimeSpan.FromSeconds(2);

    public SaveStateViewModel SaveState { get; }
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

    private static readonly string AppVersion = FormatAppVersion();

    private static string FormatAppVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        return v != null ? $"ver{v.Major}.{v.Minor}.{v.Build}" : string.Empty;
    }

    public string Title
    {
        get
        {
            var fileLabel = string.IsNullOrEmpty(SaveState.CurrentFilePath)
                ? "(未保存)"
                : Path.GetFileName(SaveState.CurrentFilePath);
            var dirtyMark = SaveState.IsDirty ? "*" : string.Empty;
            return $"IdeaNest - {fileLabel}{dirtyMark} - {AppVersion}";
        }
    }

    // ── Save state: forward to SaveState sub-ViewModel ────────────────────────
    // Logic (dirty tracking, auto-save state, status text) lives in SaveStateViewModel.
    // These thin forwards keep existing XAML bindings working without change.

    public string? CurrentFilePath => SaveState.CurrentFilePath;
    public bool IsDirty => SaveState.IsDirty;
    public string SaveStatusText => SaveState.SaveStatusText;

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

    public ICommand NewWorkspaceCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
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

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public int TotalCount => AllCards.Count;
    public int VisibleCount => VisibleCards.Count;

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

    public MainViewModel()
    {
        SaveState = new SaveStateViewModel();
        SaveState.PropertyChanged += (_, e) =>
        {
            // Relay all SaveState property changes to MainViewModel's bindings.
            OnPropertyChanged(e.PropertyName);
            // Title depends on CurrentFilePath and IsDirty; re-raise it when either changes.
            if (e.PropertyName is nameof(SaveStateViewModel.CurrentFilePath)
                               or nameof(SaveStateViewModel.IsDirty))
            {
                OnPropertyChanged(nameof(Title));
            }
        };

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
            platform: new WpfExportPlatform(),
            showStatus: ShowStatus);

        NewWorkspaceCommand    = new RelayCommand(_ => NewWorkspace());
        OpenCommand            = new RelayCommand(_ => Open());
        SaveCommand            = new RelayCommand(_ => Save());
        SaveAsCommand          = new RelayCommand(_ => SaveAs());
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
        OnPropertyChanged(nameof(HasActiveFilter));
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
    }

    private void NewWorkspace()
    {
        if (!ConfirmDiscardChanges()) return;
        _workspace = new Workspace();
        _autoSaveTimer?.Stop();
        SaveState.Reset();
        ReloadFromWorkspace();
    }

    private void Open()
    {
        if (!ConfirmDiscardChanges()) return;
        var dlg = new OpenFileDialog
        {
            Filter = "IdeaNest files (*.ideanest)|*.ideanest|All files (*.*)|*.*",
            DefaultExt = ".ideanest",
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _workspace = WorkspaceService.Load(dlg.FileName);
            _autoSaveTimer?.Stop();
            SaveState.OnFileLoaded(dlg.FileName);
            ReloadFromWorkspace();
            AppSettingsService.AddRecentFile(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ファイルを開けませんでした:\n{ex.Message}", "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool Save()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            return SaveAs();
        }
        return SaveTo(CurrentFilePath);
    }

    public bool SaveAs()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "IdeaNest files (*.ideanest)|*.ideanest",
            DefaultExt = ".ideanest",
            FileName = string.IsNullOrEmpty(CurrentFilePath) ? "ideas.ideanest" : Path.GetFileName(CurrentFilePath),
        };
        if (dlg.ShowDialog() != true) return false;
        var ok = SaveTo(dlg.FileName);
        if (ok) AppSettingsService.AddRecentFile(dlg.FileName);
        return ok;
    }

    private bool SaveTo(string path)
    {
        try
        {
            SyncWindowSizeBeforeSave();
            WorkspaceService.Save(path, _workspace);
            _autoSaveTimer?.Stop();
            SaveState.OnManualSaveSuccess(path);
            return true;
        }
        catch (Exception ex)
        {
            // Manual save failure is surfaced via MessageBox only.
            // SaveStatusText is unchanged because SaveState is left unmodified.
            MessageBox.Show($"保存に失敗しました:\n{ex.Message}", "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void SyncWindowSizeBeforeSave()
    {
        var win = Application.Current?.MainWindow;
        if (win != null)
        {
            _workspace.Settings.WindowWidth = win.ActualWidth;
            _workspace.Settings.WindowHeight = win.ActualHeight;
        }
        Filter.SyncToSettings(_workspace.Settings);
        TagPanel.SyncToSettings(_workspace.Settings);
        CardDisplay.SyncToSettings(_workspace.Settings);
    }

    public bool ConfirmDiscardChanges()
    {
        if (!IsDirty) return true;
        var result = ConfirmWindow.ShowYesNoCancel(
            Application.Current?.MainWindow,
            "未保存の変更があります",
            "保存していない変更があります。保存しますか？",
            primaryText: "保存して続行",
            secondaryText: "保存しない",
            cancelText: "キャンセル");
        return result switch
        {
            ConfirmResult.Primary    => Save(),
            ConfirmResult.Secondary  => true,
            _                        => false,
        };
    }

    private void AddIdea()
    {
        var draft = new Idea();
        var vm = new EditIdeaViewModel(draft);
        var dlg = new EditIdeaWindow
        {
            Title = "新規アイデア",
            DataContext = vm,
            Owner = Application.Current?.MainWindow,
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
            Owner = Application.Current?.MainWindow,
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
            Owner = owner ?? Application.Current?.MainWindow,
        };
        if (dlg.ShowDialog() != true) return;
        vm.ApplyTo(card.Model);
        _cardOps.CommitEdit(card);
    }

    private void DeleteIdea(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var ok = ConfirmWindow.ShowOkCancel(
            Application.Current?.MainWindow,
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

    public void MarkDirty()
    {
        SaveState.MarkDirty();
        ScheduleAutoSave();
    }

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

    private void ScheduleAutoSave()
    {
        // Auto-save only fires when we already have a path; new/unsaved files
        // require explicit Save-As so we never pick a path on the user's behalf.
        if (!SaveState.CanScheduleAutoSave) return;

        if (_autoSaveTimer == null)
        {
            _autoSaveTimer = new DispatcherTimer { Interval = AutoSaveDelay };
            _autoSaveTimer.Tick += OnAutoSaveTick;
        }
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private void OnAutoSaveTick(object? sender, EventArgs e)
    {
        _autoSaveTimer?.Stop();
        PerformAutoSave();
    }

    private void PerformAutoSave()
    {
        if (string.IsNullOrEmpty(SaveState.CurrentFilePath)) return;
        if (!SaveState.IsDirty) return;
        if (!SaveState.CanScheduleAutoSave) return;

        SaveState.OnAutoSaveBegin();
        try
        {
            SyncWindowSizeBeforeSave();
            WorkspaceService.Save(SaveState.CurrentFilePath!, _workspace);
            SaveState.OnAutoSaveSuccess();
        }
        catch
        {
            // Stay dirty so the user can retry via Ctrl+S; surface the failure
            // through SaveStatusText rather than a modal dialog.
            SaveState.OnAutoSaveFail();
        }
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
            Owner = Application.Current?.MainWindow,
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

    public void RenameTag(string oldName, string newName)
    {
        newName = WorkspaceService.NormalizeTag(newName);
        if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;

        foreach (var card in AllCards)
        {
            if (card.Tags.Any(t => string.Equals(t, oldName, StringComparison.Ordinal)))
            {
                card.Tags = WorkspaceService.NormalizeTags(
                    card.Tags.Select(t => string.Equals(t, oldName, StringComparison.Ordinal) ? newName : t));
                card.Touch();
                card.OnExternalUpdate();
            }
        }

        if (string.Equals(SelectedTag, oldName, StringComparison.Ordinal))
        {
            SelectedTag = newName;
        }

        MarkDirty();
        RefreshTags();
        RefreshVisible();
    }

    public void DeleteTag(string tagName)
    {
        foreach (var card in AllCards)
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

        if (string.Equals(SelectedTag, tagName, StringComparison.Ordinal))
        {
            SelectedTag = string.Empty;
        }

        MarkDirty();
        RefreshTags();
        RefreshVisible();
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

    public void LoadStartup(string? filePath = null)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                _workspace = WorkspaceService.Load(filePath);
                _autoSaveTimer?.Stop();
                SaveState.OnFileLoaded(filePath);
                ReloadFromWorkspace();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ファイルを開けませんでした:\n{ex.Message}",
                    "IdeaNest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                // fall through to new workspace
            }
        }
        ReloadFromWorkspace();
    }

    public void ApplyInitialWindowSize(Window window)
    {
        if (_workspace.Settings.WindowWidth  > 200) window.Width  = _workspace.Settings.WindowWidth;
        if (_workspace.Settings.WindowHeight > 200) window.Height = _workspace.Settings.WindowHeight;
    }
}
