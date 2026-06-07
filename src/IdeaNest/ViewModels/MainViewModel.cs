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
    private string? _currentFilePath;
    private bool _isDirty;
    private string _statusMessage = string.Empty;
    private DispatcherTimer? _statusClearTimer;
    private DispatcherTimer? _autoSaveTimer;
    private bool _isAutoSaving;
    private bool _autoSaveFailed;
    private DateTime? _lastAutoSaveTime;
    private static readonly TimeSpan AutoSaveDelay = TimeSpan.FromSeconds(2);
    public CardDisplayViewModel CardDisplay { get; }
    public FilterViewModel Filter { get; }
    public TagPanelViewModel TagPanel { get; }

    public ObservableCollection<IdeaCardViewModel> AllCards { get; } = new();
    public ObservableCollection<IdeaCardViewModel> VisibleCards { get; } = new();
    public ObservableCollection<string> AvailableTags { get; } = new();

    /// <summary>Forwards to TagPanel.VisibleItems (filtered by TagPanel.TagSearch).</summary>
    public ObservableCollection<TagItemViewModel> TagItems => TagPanel.VisibleItems;

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
            var fileLabel = string.IsNullOrEmpty(_currentFilePath) ? "(未保存)" : Path.GetFileName(_currentFilePath);
            var dirtyMark = _isDirty ? "*" : string.Empty;
            return $"IdeaNest - {fileLabel}{dirtyMark} - {AppVersion}";
        }
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set
        {
            if (SetField(ref _currentFilePath, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(SaveStatusText));
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetField(ref _isDirty, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(SaveStatusText));
            }
        }
    }

    public string SaveStatusText
    {
        get
        {
            if (_isAutoSaving) return "自動保存中...";
            if (_autoSaveFailed) return "自動保存に失敗しました";
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                return IsDirty ? "未保存 (新規ファイル)" : "新規ファイル";
            }
            if (IsDirty) return "未保存の変更あり";
            if (_lastAutoSaveTime.HasValue)
                return $"自動保存しました {_lastAutoSaveTime:HH:mm}";
            return "保存済み";
        }
    }

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
        CardDisplay = new CardDisplayViewModel(RefreshVisible, MarkDirty);
        CardDisplay.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);

        Filter = new FilterViewModel(RefreshVisible, OnFilterChanged);
        Filter.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);

        TagPanel = new TagPanelViewModel(OnTagPanelChanged, tag => SelectedTag = tag);
        // Relay IsTagPanelOpen / TagPanelButtonLabel / TagPanelButtonTip so existing
        // XAML bindings continue to work via the forwarding properties above.
        TagPanel.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);

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
        ExportMarkdownCommand     = new RelayCommand(_ => ExportMarkdown());
        CopyCardMarkdownCommand   = new RelayCommand(p => CopyCardMarkdown(p as IdeaCardViewModel));
        CopyAllMarkdownCommand    = new RelayCommand(_ => CopyAllMarkdown());
        ExportNoteNestCommand     = new RelayCommand(_ => ExportNoteNest());
        CopyNoteNestCommand       = new RelayCommand(_ => CopyNoteNest());
        ToggleTagPanelCommand     = new RelayCommand(_ => TagPanel.Toggle());
        SetCardSizeCommand        = new RelayCommand(p => CardDisplay.CardSize = p as string ?? "medium");
        SetCardHeightModeCommand  = new RelayCommand(p => CardDisplay.CardHeightMode = p as string ?? "fixed");
        ReshuffleCommand          = new RelayCommand(_ =>
            CardDisplay.Reshuffle(AllCards.Where(c => !c.IsPinned).Select(c => c.Id)));
    }

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
        CurrentFilePath = null;
        ReloadFromWorkspace();
        IsDirty = false;
        ResetAutoSaveState();
    }

    private void ResetAutoSaveState()
    {
        _autoSaveTimer?.Stop();
        _lastAutoSaveTime = null;
        _autoSaveFailed = false;
        OnPropertyChanged(nameof(SaveStatusText));
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
            CurrentFilePath = dlg.FileName;
            ReloadFromWorkspace();
            IsDirty = false;
            ResetAutoSaveState();
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
            CurrentFilePath = path;
            IsDirty = false;
            _autoSaveTimer?.Stop();
            // _lastAutoSaveTime は自動保存専用。手動保存後は過去の自動保存時刻を
            // クリアして SaveStatusText が「保存済み」を返すようにする。
            _lastAutoSaveTime = null;
            _autoSaveFailed = false;
            OnPropertyChanged(nameof(SaveStatusText));
            return true;
        }
        catch (Exception ex)
        {
            // _autoSaveFailed は自動保存専用フラグ。手動保存失敗は MessageBox のみで伝える。
            OnPropertyChanged(nameof(SaveStatusText));
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

        var title = draft.Title?.Trim() ?? string.Empty;
        var body  = draft.Body?.Trim()  ?? string.Empty;
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
        {
            // タイトル・本文が両方空のカードは保存しない (空入力 → キャンセル相当)
            return;
        }

        if (string.IsNullOrEmpty(title))
        {
            var firstLine = body.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
            draft.Title = firstLine.Length > 40 ? firstLine[..40] : firstLine;
        }

        var now = DateTime.Now;
        draft.CreatedAt = now;
        draft.UpdatedAt = now;

        _workspace.Ideas.Add(draft);
        AllCards.Add(new IdeaCardViewModel(draft));
        MarkDirty();
        RefreshTags();
        RefreshVisible();
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
            onCopyMarkdown: c => CopyCardMarkdown(c))
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
        var result = dlg.ShowDialog();
        if (result == true)
        {
            vm.ApplyTo(card.Model);
            card.Touch();
            card.OnExternalUpdate();
            MarkDirty();
            RefreshTags();
            RefreshVisible();
        }
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
        _workspace.Ideas.Remove(card.Model);
        AllCards.Remove(card);
        MarkDirty();
        RefreshTags();
        RefreshVisible();
    }

    private void TogglePin(IdeaCardViewModel? card)
    {
        if (card == null) return;
        card.IsPinned = !card.IsPinned;
        card.Touch();
        MarkDirty();
        RefreshVisible();
    }

    private void ToggleArchive(IdeaCardViewModel? card)
    {
        if (card == null) return;
        card.IsArchived = !card.IsArchived;
        card.Touch();
        MarkDirty();
        RefreshVisible();
    }

    public void MarkDirty()
    {
        IsDirty = true;
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

    private void ScheduleAutoSave()
    {
        // Auto-save only fires when we already have a path; new/unsaved files
        // require explicit Save-As so we never pick a path on the user's behalf.
        if (string.IsNullOrEmpty(CurrentFilePath)) return;
        if (_isAutoSaving) return;

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
        if (string.IsNullOrEmpty(CurrentFilePath)) return;
        if (!IsDirty) return;
        if (_isAutoSaving) return;

        _isAutoSaving = true;
        OnPropertyChanged(nameof(SaveStatusText));

        try
        {
            SyncWindowSizeBeforeSave();
            WorkspaceService.Save(CurrentFilePath, _workspace);
            IsDirty = false;
            _lastAutoSaveTime = DateTime.Now;
            _autoSaveFailed = false;
        }
        catch
        {
            // Stay dirty so the user can retry via Ctrl+S; surface the failure
            // through SaveStatusText rather than a modal dialog.
            _autoSaveFailed = true;
        }
        finally
        {
            _isAutoSaving = false;
            OnPropertyChanged(nameof(SaveStatusText));
        }
    }

    private void ReloadFromWorkspace()
    {
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
        var tagCounts = AllCards
            .SelectMany(c => c.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .GroupBy(t => t, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (Name: g.Key, Count: g.Count()))
            .ToList();

        AvailableTags.Clear();
        var tagItems = new List<TagItemViewModel>(tagCounts.Count);
        foreach (var (name, count) in tagCounts)
        {
            AvailableTags.Add(name);
            tagItems.Add(new TagItemViewModel(name, count));
        }
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

    private void ExportMarkdown()
    {
        if (VisibleCards.Count == 0)
        {
            MessageBox.Show(
                "エクスポート対象のカードがありません。",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var defaultName = $"ideanest_export_{DateTime.Now:yyyyMMdd_HHmm}.md";
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt",
            DefaultExt = ".md",
            FileName = defaultName,
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            Services.MarkdownExportService.Export(
                dlg.FileName,
                VisibleCards,
                SearchText,
                SelectedTag,
                SelectedColor,
                ShowArchived);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"エクスポートに失敗しました:\n{ex.Message}",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CopyCardMarkdown(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var text = MarkdownExportService.FormatCard(card);
        try
        {
            Clipboard.SetText(text);
            ShowStatus("カードをコピーしました。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"クリップボードへのコピーに失敗しました:\n{ex.Message}",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CopyAllMarkdown()
    {
        if (VisibleCards.Count == 0)
        {
            MessageBox.Show(
                "コピー対象のカードがありません。",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        var text = MarkdownExportService.FormatAll(
            VisibleCards, SearchText, SelectedTag, SelectedColor, ShowArchived);
        try
        {
            Clipboard.SetText(text);
            ShowStatus($"表示中の{VisibleCards.Count}件をコピーしました。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"クリップボードへのコピーに失敗しました:\n{ex.Message}",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ExportNoteNest()
    {
        if (VisibleCards.Count == 0)
        {
            MessageBox.Show(
                "NoteNest向けに出力するカードがありません。",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var optsDlg = new Views.NoteNestExportOptionsWindow
        {
            Owner = Application.Current?.MainWindow,
        };
        if (optsDlg.ShowDialog() != true) return;
        var options = optsDlg.Options!;

        var defaultName = $"ideanest_notenest_{DateTime.Now:yyyyMMdd_HHmm}.md";
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt",
            DefaultExt = ".md",
            FileName = defaultName,
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            NoteNestExportService.Export(
                dlg.FileName,
                VisibleCards,
                SearchText,
                SelectedTag,
                SelectedColor,
                ShowArchived,
                options);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"エクスポートに失敗しました:\n{ex.Message}",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CopyNoteNest()
    {
        if (VisibleCards.Count == 0)
        {
            MessageBox.Show(
                "NoteNest向けに出力するカードがありません。",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var optsDlg = new Views.NoteNestExportOptionsWindow
        {
            Owner = Application.Current?.MainWindow,
        };
        if (optsDlg.ShowDialog() != true) return;
        var options = optsDlg.Options!;

        var text = NoteNestExportService.FormatAll(
            VisibleCards, SearchText, SelectedTag, SelectedColor, ShowArchived, options);
        try
        {
            Clipboard.SetText(text);
            ShowStatus($"表示中の{VisibleCards.Count}件をNoteNest向け形式でコピーしました。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"クリップボードへのコピーに失敗しました:\n{ex.Message}",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
                CurrentFilePath = filePath;
                ReloadFromWorkspace();
                IsDirty = false;
                ResetAutoSaveState();
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
