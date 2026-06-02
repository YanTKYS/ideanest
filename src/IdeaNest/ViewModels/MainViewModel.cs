using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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
    private string _searchText = string.Empty;
    private string _selectedTag = string.Empty;
    private bool _showArchived;

    public ObservableCollection<IdeaCardViewModel> AllCards { get; } = new();
    public ObservableCollection<IdeaCardViewModel> VisibleCards { get; } = new();
    public ObservableCollection<string> AvailableTags { get; } = new();

    public string Title
    {
        get
        {
            var fileLabel = string.IsNullOrEmpty(_currentFilePath) ? "(未保存)" : Path.GetFileName(_currentFilePath);
            var dirtyMark = _isDirty ? "*" : string.Empty;
            return $"IdeaNest - {fileLabel}{dirtyMark}";
        }
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set { if (SetField(ref _currentFilePath, value)) { OnPropertyChanged(nameof(Title)); } }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set { if (SetField(ref _isDirty, value)) { OnPropertyChanged(nameof(Title)); } }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                _workspace.Settings.SearchText = value;
                RefreshVisible();
                MarkDirty();
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
                _workspace.Settings.SelectedTag = _selectedTag;
                RefreshVisible();
                MarkDirty();
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
                _workspace.Settings.ShowArchived = value;
                RefreshVisible();
                MarkDirty();
            }
        }
    }

    public WorkspaceSettings Settings => _workspace.Settings;

    public ICommand NewWorkspaceCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand AddIdeaCommand { get; }
    public ICommand EditIdeaCommand { get; }
    public ICommand DeleteIdeaCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand ToggleArchiveCommand { get; }
    public ICommand SelectTagCommand { get; }
    public ICommand ClearTagCommand { get; }

    public MainViewModel()
    {
        NewWorkspaceCommand    = new RelayCommand(_ => NewWorkspace());
        OpenCommand            = new RelayCommand(_ => Open());
        SaveCommand            = new RelayCommand(_ => Save());
        SaveAsCommand          = new RelayCommand(_ => SaveAs());
        AddIdeaCommand         = new RelayCommand(_ => AddIdea());
        EditIdeaCommand        = new RelayCommand(p => EditIdea(p as IdeaCardViewModel));
        DeleteIdeaCommand      = new RelayCommand(p => DeleteIdea(p as IdeaCardViewModel));
        TogglePinCommand       = new RelayCommand(p => TogglePin(p as IdeaCardViewModel));
        ToggleArchiveCommand   = new RelayCommand(p => ToggleArchive(p as IdeaCardViewModel));
        SelectTagCommand       = new RelayCommand(p => SelectedTag = p as string ?? string.Empty);
        ClearTagCommand        = new RelayCommand(_ => SelectedTag = string.Empty);
    }

    private void NewWorkspace()
    {
        if (!ConfirmDiscardChanges()) return;
        _workspace = new Workspace();
        CurrentFilePath = null;
        ReloadFromWorkspace();
        IsDirty = false;
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
        return SaveTo(dlg.FileName);
    }

    private bool SaveTo(string path)
    {
        try
        {
            SyncWindowSizeBeforeSave();
            WorkspaceService.Save(path, _workspace);
            CurrentFilePath = path;
            IsDirty = false;
            return true;
        }
        catch (Exception ex)
        {
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
        _workspace.Settings.SearchText = SearchText;
        _workspace.Settings.SelectedTag = SelectedTag;
        _workspace.Settings.ShowArchived = ShowArchived;
    }

    public bool ConfirmDiscardChanges()
    {
        if (!IsDirty) return true;
        var result = MessageBox.Show(
            "未保存の変更があります。保存しますか？",
            "IdeaNest",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        return result switch
        {
            MessageBoxResult.Yes    => Save(),
            MessageBoxResult.No     => true,
            _                       => false,
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

    private void EditIdea(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var vm = new EditIdeaViewModel(card.Model);
        var dlg = new EditIdeaWindow
        {
            Title = "アイデア編集",
            DataContext = vm,
            Owner = Application.Current?.MainWindow,
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
        var ok = MessageBox.Show("このカードを削除しますか？", "IdeaNest", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (ok != MessageBoxResult.OK) return;
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

    public void MarkDirty() => IsDirty = true;

    private void ReloadFromWorkspace()
    {
        AllCards.Clear();
        foreach (var idea in _workspace.Ideas)
        {
            AllCards.Add(new IdeaCardViewModel(idea));
        }
        _searchText = _workspace.Settings.SearchText ?? string.Empty;
        _selectedTag = _workspace.Settings.SelectedTag ?? string.Empty;
        _showArchived = _workspace.Settings.ShowArchived;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedTag));
        OnPropertyChanged(nameof(ShowArchived));
        RefreshTags();
        RefreshVisible();
    }

    private void RefreshTags()
    {
        var tags = AllCards
            .SelectMany(c => c.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();
        AvailableTags.Clear();
        foreach (var t in tags) AvailableTags.Add(t);
    }

    private void RefreshVisible()
    {
        var query = (SearchText ?? string.Empty).Trim();
        var tag = (SelectedTag ?? string.Empty).Trim();

        IEnumerable<IdeaCardViewModel> items = AllCards;

        if (!ShowArchived)
        {
            items = items.Where(c => !c.IsArchived);
        }

        if (!string.IsNullOrEmpty(tag))
        {
            items = items.Where(c => c.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrEmpty(query))
        {
            items = items.Where(c =>
                (c.Title ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase)
                || (c.Body ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase)
                || c.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var ordered = items
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .ToList();

        VisibleCards.Clear();
        foreach (var c in ordered) VisibleCards.Add(c);
    }

    public void LoadStartup()
    {
        ReloadFromWorkspace();
    }

    public void ApplyInitialWindowSize(Window window)
    {
        if (_workspace.Settings.WindowWidth  > 200) window.Width  = _workspace.Settings.WindowWidth;
        if (_workspace.Settings.WindowHeight > 200) window.Height = _workspace.Settings.WindowHeight;
    }
}
