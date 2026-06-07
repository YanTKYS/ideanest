using System;
using IdeaNest.ViewModels;

namespace IdeaNest.ViewModels;

/// <summary>
/// Tracks file-path, dirty state, auto-save progress, and save-status text.
/// WPF-independent — the DispatcherTimer that triggers auto-save stays in
/// MainViewModel, which calls the transition methods here at the right moments.
/// Accepts an injectable clock so tests do not depend on wall time.
/// </summary>
public class SaveStateViewModel : ViewModelBase
{
    private string? _currentFilePath;
    private bool _isDirty;
    private bool _isAutoSaving;
    private bool _autoSaveFailed;
    private DateTime? _lastAutoSaveTime;

    private readonly Func<DateTime> _now;

    public SaveStateViewModel(Func<DateTime>? now = null)
    {
        _now = now ?? (() => DateTime.Now);
    }

    // ── Public state ──────────────────────────────────────────────────────────

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set
        {
            if (SetField(ref _currentFilePath, value))
            {
                OnPropertyChanged(nameof(SaveStatusText));
                OnPropertyChanged(nameof(CanScheduleAutoSave));
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetField(ref _isDirty, value))
                OnPropertyChanged(nameof(SaveStatusText));
        }
    }

    /// <summary>
    /// True when starting the auto-save timer makes sense: a file path is
    /// known and an auto-save is not already in progress.
    /// Does NOT check IsDirty — the caller checks that independently so it can
    /// guard both the timer-start path and the timer-tick path.
    /// </summary>
    public bool CanScheduleAutoSave =>
        !string.IsNullOrEmpty(CurrentFilePath) && !_isAutoSaving;

    public string SaveStatusText
    {
        get
        {
            if (_isAutoSaving) return "自動保存中...";
            if (_autoSaveFailed) return "自動保存に失敗しました";
            if (string.IsNullOrEmpty(CurrentFilePath))
                return IsDirty ? "未保存 (新規ファイル)" : "新規ファイル";
            if (IsDirty) return "未保存の変更あり";
            if (_lastAutoSaveTime.HasValue)
                return $"自動保存しました {_lastAutoSaveTime:HH:mm}";
            return "保存済み";
        }
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    /// <summary>Marks the workspace as having unsaved changes.</summary>
    public void MarkDirty() => IsDirty = true;

    /// <summary>
    /// Called after a file is successfully loaded (Open dialog or startup).
    /// Clears dirty state and resets auto-save history.
    /// </summary>
    public void OnFileLoaded(string path)
    {
        CurrentFilePath = path;
        IsDirty = false;
        ResetAutoSaveState();
    }

    /// <summary>
    /// Called after a manual Save or Save-As succeeds.
    /// Clears dirty state, updates path, and clears auto-save timestamp so that
    /// SaveStatusText shows "保存済み" rather than "自動保存しました HH:mm".
    /// </summary>
    public void OnManualSaveSuccess(string path)
    {
        CurrentFilePath = path;
        IsDirty = false;
        _lastAutoSaveTime = null;
        _autoSaveFailed = false;
        OnPropertyChanged(nameof(SaveStatusText));
    }

    /// <summary>
    /// Resets to a new/unsaved workspace state (New Workspace command).
    /// </summary>
    public void Reset()
    {
        CurrentFilePath = null;
        IsDirty = false;
        ResetAutoSaveState();
    }

    // ── Auto-save lifecycle ───────────────────────────────────────────────────

    /// <summary>Called by MainViewModel just before the auto-save write starts.</summary>
    public void OnAutoSaveBegin()
    {
        _isAutoSaving = true;
        OnPropertyChanged(nameof(SaveStatusText));
        OnPropertyChanged(nameof(CanScheduleAutoSave));
    }

    /// <summary>Called by MainViewModel after the auto-save write succeeds.</summary>
    public void OnAutoSaveSuccess()
    {
        IsDirty = false;
        _lastAutoSaveTime = _now();
        _autoSaveFailed = false;
        _isAutoSaving = false;
        OnPropertyChanged(nameof(SaveStatusText));
        OnPropertyChanged(nameof(CanScheduleAutoSave));
    }

    /// <summary>
    /// Called by MainViewModel when the auto-save write throws.
    /// Preserves IsDirty so the user can retry via Ctrl+S.
    /// </summary>
    public void OnAutoSaveFail()
    {
        _autoSaveFailed = true;
        _isAutoSaving = false;
        OnPropertyChanged(nameof(SaveStatusText));
        OnPropertyChanged(nameof(CanScheduleAutoSave));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void ResetAutoSaveState()
    {
        _lastAutoSaveTime = null;
        _autoSaveFailed = false;
        _isAutoSaving = false;
        OnPropertyChanged(nameof(SaveStatusText));
        OnPropertyChanged(nameof(CanScheduleAutoSave));
    }
}
