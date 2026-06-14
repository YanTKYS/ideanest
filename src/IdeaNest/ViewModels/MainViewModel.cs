using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using IdeaNest.Commands;
using IdeaNest.Models;
using IdeaNest.Services;
using IdeaNest.Views;
using Microsoft.Win32;

namespace IdeaNest.ViewModels;

/// <summary>Standalone IdeaNest AppShell: owns files, save state, lifecycle and auto-save.</summary>
public class MainViewModel : ViewModelBase
{
    private static readonly TimeSpan AutoSaveDelay = TimeSpan.FromSeconds(2);
    private static readonly string AppVersion = FormatAppVersion();
    private DispatcherTimer? _autoSaveTimer;

    public IdeaNestWorkspaceViewModel Workspace { get; }
    public SaveStateViewModel SaveState { get; }
    public ICommand NewWorkspaceCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand ShowTutorialCommand { get; }
    public ICommand ExitCommand { get; }

    public string? CurrentFilePath => SaveState.CurrentFilePath;
    public bool IsDirty => SaveState.IsDirty;
    public string SaveStatusText => SaveState.SaveStatusText;
    public string Title
    {
        get
        {
            var file = string.IsNullOrEmpty(CurrentFilePath) ? "(未保存)" : Path.GetFileName(CurrentFilePath);
            return $"IdeaNest - {file}{(IsDirty ? "*" : string.Empty)} - {AppVersion}";
        }
    }

    public MainViewModel()
    {
        Workspace = new IdeaNestWorkspaceViewModel();
        Workspace.DirtyRequested += (_, _) => MarkDirty();
        SaveState = new SaveStateViewModel();
        SaveState.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            if (e.PropertyName is nameof(SaveStateViewModel.CurrentFilePath) or nameof(SaveStateViewModel.IsDirty))
                OnPropertyChanged(nameof(Title));
        };
        NewWorkspaceCommand = new RelayCommand(_ => NewWorkspace());
        OpenCommand = new RelayCommand(_ => Open());
        SaveCommand = new RelayCommand(_ => Save());
        SaveAsCommand = new RelayCommand(_ => SaveAs());
        ShowTutorialCommand = new RelayCommand(_ => ShowTutorial());
        ExitCommand = new RelayCommand(_ => Application.Current?.MainWindow?.Close());
        Workspace.SetHostCommands(new IdeaNestWorkspaceHostCommands
        {
            NewWorkspace = NewWorkspaceCommand,
            Open = OpenCommand,
            Save = SaveCommand,
            SaveAs = SaveAsCommand,
            ShowTutorial = ShowTutorialCommand,
            Exit = ExitCommand,
        });
    }

    private static string FormatAppVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        return v != null ? $"ver{v.Major}.{v.Minor}.{v.Build}" : string.Empty;
    }

    private static void ShowTutorial()
    {
        var window = new TutorialWindow { Owner = Application.Current?.MainWindow };
        window.ShowDialog();
    }

    private void NewWorkspace()
    {
        if (!ConfirmDiscardChanges()) return;
        _autoSaveTimer?.Stop();
        SaveState.Reset();
        Workspace.LoadFromWorkspace(new Workspace());
    }

    private void Open()
    {
        if (!ConfirmDiscardChanges()) return;
        var dlg = new OpenFileDialog { Filter = "IdeaNest files (*.ideanest)|*.ideanest|All files (*.*)|*.*", DefaultExt = ".ideanest" };
        if (dlg.ShowDialog() == true) LoadFromFile(dlg.FileName, addToRecent: true);
    }

    public bool LoadFromFile(string path, bool addToRecent = false)
    {
        try
        {
            var loaded = WorkspaceService.Load(path);
            _autoSaveTimer?.Stop();
            Workspace.LoadFromWorkspace(loaded);
            SaveState.OnFileLoaded(path);
            if (addToRecent) AppSettingsService.AddRecentFile(path);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ファイルを開けませんでした:\n{ex.Message}", "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    public bool Save() => string.IsNullOrEmpty(CurrentFilePath) ? SaveAs() : SaveToFile(CurrentFilePath);

    public bool SaveAs()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "IdeaNest files (*.ideanest)|*.ideanest", DefaultExt = ".ideanest",
            FileName = string.IsNullOrEmpty(CurrentFilePath) ? "ideas.ideanest" : Path.GetFileName(CurrentFilePath),
        };
        if (dlg.ShowDialog() != true) return false;
        var saved = SaveToFile(dlg.FileName);
        if (saved) AppSettingsService.AddRecentFile(dlg.FileName);
        return saved;
    }

    public bool SaveToFile(string path)
    {
        try
        {
            SyncWindowSizeBeforeSave();
            WorkspaceService.Save(path, Workspace.BuildWorkspaceForSave());
            _autoSaveTimer?.Stop();
            SaveState.OnManualSaveSuccess(path);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存に失敗しました:\n{ex.Message}", "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    public void MarkDirty()
    {
        SaveState.MarkDirty();
        ScheduleAutoSave();
    }

    private void ScheduleAutoSave()
    {
        if (!SaveState.CanScheduleAutoSave) return;
        _autoSaveTimer ??= CreateAutoSaveTimer();
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private DispatcherTimer CreateAutoSaveTimer()
    {
        var timer = new DispatcherTimer { Interval = AutoSaveDelay };
        timer.Tick += (_, _) => { timer.Stop(); PerformAutoSave(); };
        return timer;
    }

    private void PerformAutoSave()
    {
        if (string.IsNullOrEmpty(CurrentFilePath) || !IsDirty || !SaveState.CanScheduleAutoSave) return;
        SaveState.OnAutoSaveBegin();
        try
        {
            SyncWindowSizeBeforeSave();
            WorkspaceService.Save(CurrentFilePath, Workspace.BuildWorkspaceForSave());
            SaveState.OnAutoSaveSuccess();
        }
        catch { SaveState.OnAutoSaveFail(); }
    }

    private void SyncWindowSizeBeforeSave()
    {
        var window = Application.Current?.MainWindow;
        var settings = Workspace.BuildWorkspaceForSave().Settings;
        if (window != null) { settings.WindowWidth = window.ActualWidth; settings.WindowHeight = window.ActualHeight; }
    }

    public bool ConfirmDiscardChanges()
    {
        if (!IsDirty) return true;
        var result = ConfirmWindow.ShowYesNoCancel(Application.Current?.MainWindow, "未保存の変更があります", "保存していない変更があります。保存しますか？", "保存して続行", "保存しない", "キャンセル");
        return result switch { ConfirmResult.Primary => Save(), ConfirmResult.Secondary => true, _ => false };
    }

    public void LoadStartup(string? path = null)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path) && LoadFromFile(path)) return;
        Workspace.LoadFromWorkspace(new Workspace());
    }

    public void ApplyInitialWindowSize(Window window)
    {
        var settings = Workspace.BuildWorkspaceForSave().Settings;
        if (settings.WindowWidth > 200) window.Width = settings.WindowWidth;
        if (settings.WindowHeight > 200) window.Height = settings.WindowHeight;
    }
}
