using System;
using System.Collections.Generic;
using System.ComponentModel;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class SaveStateViewModelTests
{
    private static SaveStateViewModel Make(Func<DateTime>? clock = null)
        => new(clock);

    private static List<string?> CapturePropertyChanges(INotifyPropertyChanged source)
    {
        var changes = new List<string?>();
        source.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        return changes;
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void InitialState_IsDirty_IsFalse()
    {
        Assert.False(Make().IsDirty);
    }

    [Fact]
    public void InitialState_CurrentFilePath_IsNull()
    {
        Assert.Null(Make().CurrentFilePath);
    }

    [Fact]
    public void InitialState_SaveStatusText_IsNewFile()
    {
        Assert.Equal("新規ファイル", Make().SaveStatusText);
    }

    [Fact]
    public void InitialState_CanScheduleAutoSave_IsFalse()
    {
        // No path → cannot schedule auto-save.
        Assert.False(Make().CanScheduleAutoSave);
    }

    // ── MarkDirty ─────────────────────────────────────────────────────────────

    [Fact]
    public void MarkDirty_Sets_IsDirty_True()
    {
        var vm = Make();
        vm.MarkDirty();
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void MarkDirty_WithNoPath_SaveStatusText_IsUnsavedNewFile()
    {
        var vm = Make();
        vm.MarkDirty();
        Assert.Equal("未保存 (新規ファイル)", vm.SaveStatusText);
    }

    [Fact]
    public void MarkDirty_WithPath_SaveStatusText_IsUnsavedChanges()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        Assert.Equal("未保存の変更あり", vm.SaveStatusText);
    }

    [Fact]
    public void MarkDirty_WithNoPath_CanScheduleAutoSave_IsFalse()
    {
        var vm = Make();
        vm.MarkDirty();
        // Dirty but no path → still cannot auto-save.
        Assert.False(vm.CanScheduleAutoSave);
    }

    [Fact]
    public void MarkDirty_WithPath_CanScheduleAutoSave_IsTrue()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        Assert.True(vm.CanScheduleAutoSave);
    }

    // ── OnFileLoaded ──────────────────────────────────────────────────────────

    [Fact]
    public void OnFileLoaded_SetsPath_And_ClearsDirty()
    {
        var vm = Make();
        vm.MarkDirty();

        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");

        Assert.Equal(@"C:\notes\ideas.ideanest", vm.CurrentFilePath);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void OnFileLoaded_SaveStatusText_IsSaved()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        Assert.Equal("保存済み", vm.SaveStatusText);
    }

    [Fact]
    public void OnFileLoaded_CanScheduleAutoSave_IsTrue()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        Assert.True(vm.CanScheduleAutoSave);
    }

    // ── OnManualSaveSuccess ───────────────────────────────────────────────────

    [Fact]
    public void OnManualSaveSuccess_ClearsDirty_And_UpdatesPath()
    {
        var vm = Make();
        vm.MarkDirty();

        vm.OnManualSaveSuccess(@"C:\notes\ideas.ideanest");

        Assert.False(vm.IsDirty);
        Assert.Equal(@"C:\notes\ideas.ideanest", vm.CurrentFilePath);
    }

    [Fact]
    public void OnManualSaveSuccess_SaveStatusText_IsSaved()
    {
        var vm = Make();
        vm.MarkDirty();
        vm.OnManualSaveSuccess(@"C:\notes\ideas.ideanest");
        Assert.Equal("保存済み", vm.SaveStatusText);
    }

    [Fact]
    public void OnManualSaveSuccess_ClearsAutoSaveTimestamp_StatusDoesNotShowAutoSaveTime()
    {
        // Simulate: auto-save ran first, then user does manual save.
        // Status should revert to "保存済み", not "自動保存しました HH:mm".
        var fixedTime = new DateTime(2026, 6, 7, 10, 30, 0);
        var vm = Make(() => fixedTime);
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();
        vm.OnAutoSaveSuccess(); // sets _lastAutoSaveTime

        vm.MarkDirty();
        vm.OnManualSaveSuccess(@"C:\notes\ideas.ideanest"); // should clear it

        Assert.Equal("保存済み", vm.SaveStatusText);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsDirty_And_Path()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();

        vm.Reset();

        Assert.False(vm.IsDirty);
        Assert.Null(vm.CurrentFilePath);
    }

    [Fact]
    public void Reset_SaveStatusText_IsNewFile()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.Reset();
        Assert.Equal("新規ファイル", vm.SaveStatusText);
    }

    [Fact]
    public void Reset_CanScheduleAutoSave_IsFalse()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.Reset();
        Assert.False(vm.CanScheduleAutoSave);
    }

    // ── Auto-save lifecycle ───────────────────────────────────────────────────

    [Fact]
    public void OnAutoSaveBegin_SaveStatusText_IsAutoSaving()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();

        vm.OnAutoSaveBegin();

        Assert.Equal("自動保存中...", vm.SaveStatusText);
    }

    [Fact]
    public void OnAutoSaveBegin_CanScheduleAutoSave_IsFalse()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();
        // _isAutoSaving is true → cannot start another auto-save.
        Assert.False(vm.CanScheduleAutoSave);
    }

    [Fact]
    public void OnAutoSaveSuccess_ClearsDirty()
    {
        var vm = Make(() => DateTime.Now);
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();

        vm.OnAutoSaveSuccess();

        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void OnAutoSaveSuccess_SaveStatusText_ContainsAutoSaveTime()
    {
        var fixedTime = new DateTime(2026, 6, 7, 14, 55, 0);
        var vm = Make(() => fixedTime);
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();
        vm.OnAutoSaveSuccess();

        Assert.Equal("自動保存しました 14:55", vm.SaveStatusText);
    }

    [Fact]
    public void OnAutoSaveSuccess_CanScheduleAutoSave_IsTrue_AfterAutoSave()
    {
        var vm = Make(() => DateTime.Now);
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();
        vm.OnAutoSaveSuccess();
        // After successful auto-save, _isAutoSaving resets → can schedule again.
        Assert.True(vm.CanScheduleAutoSave);
    }

    [Fact]
    public void OnAutoSaveFail_KeepsDirty_True()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();

        vm.OnAutoSaveFail();

        // Stay dirty so the user can retry via Ctrl+S.
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void OnAutoSaveFail_SaveStatusText_IsFailure()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();
        vm.OnAutoSaveFail();

        Assert.Equal("自動保存に失敗しました", vm.SaveStatusText);
    }

    // ── Confirm-discard guard (IsDirty as termination signal) ─────────────────

    [Fact]
    public void NeedsConfirmDiscard_Initially_False()
    {
        Assert.False(Make().IsDirty);
    }

    [Fact]
    public void NeedsConfirmDiscard_True_AfterMarkDirty()
    {
        var vm = Make();
        vm.MarkDirty();
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void NeedsConfirmDiscard_False_AfterManualSave()
    {
        var vm = Make();
        vm.MarkDirty();
        vm.OnManualSaveSuccess(@"C:\notes\ideas.ideanest");
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void NeedsConfirmDiscard_False_AfterReset()
    {
        var vm = Make();
        vm.MarkDirty();
        vm.Reset();
        Assert.False(vm.IsDirty);
    }

    // ── CanScheduleAutoSave change notifications ─────────────────────────────

    [Fact]
    public void OnFileLoaded_RaisesPropertyChanged_For_CanScheduleAutoSave()
    {
        var vm = Make();
        var changes = CapturePropertyChanges(vm);

        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");

        Assert.Contains(nameof(SaveStateViewModel.CanScheduleAutoSave), changes);
    }

    [Fact]
    public void OnManualSaveSuccess_FromNoPath_RaisesPropertyChanged_For_CanScheduleAutoSave()
    {
        var vm = Make();
        var changes = CapturePropertyChanges(vm);

        vm.OnManualSaveSuccess(@"C:\notes\ideas.ideanest");

        // Path went from null → set → CanScheduleAutoSave flipped false → true.
        Assert.Contains(nameof(SaveStateViewModel.CanScheduleAutoSave), changes);
    }

    [Fact]
    public void Reset_FromLoadedFile_RaisesPropertyChanged_For_CanScheduleAutoSave()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        var changes = CapturePropertyChanges(vm);

        vm.Reset();

        // Path went set → null → CanScheduleAutoSave flipped true → false.
        Assert.Contains(nameof(SaveStateViewModel.CanScheduleAutoSave), changes);
    }

    [Fact]
    public void OnAutoSaveBegin_RaisesPropertyChanged_For_CanScheduleAutoSave()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        var changes = CapturePropertyChanges(vm);

        vm.OnAutoSaveBegin();

        // _isAutoSaving flipped false → true → CanScheduleAutoSave flipped true → false.
        Assert.Contains(nameof(SaveStateViewModel.CanScheduleAutoSave), changes);
    }

    [Fact]
    public void OnAutoSaveSuccess_RaisesPropertyChanged_For_CanScheduleAutoSave()
    {
        var vm = Make(() => DateTime.Now);
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();
        var changes = CapturePropertyChanges(vm);

        vm.OnAutoSaveSuccess();

        // _isAutoSaving flipped true → false → CanScheduleAutoSave flipped false → true.
        Assert.Contains(nameof(SaveStateViewModel.CanScheduleAutoSave), changes);
    }

    [Fact]
    public void OnAutoSaveFail_RaisesPropertyChanged_For_CanScheduleAutoSave()
    {
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        vm.MarkDirty();
        vm.OnAutoSaveBegin();
        var changes = CapturePropertyChanges(vm);

        vm.OnAutoSaveFail();

        // _isAutoSaving flipped true → false → CanScheduleAutoSave flipped false → true.
        Assert.Contains(nameof(SaveStateViewModel.CanScheduleAutoSave), changes);
    }

    [Fact]
    public void MarkDirty_DoesNotRaisePropertyChanged_For_CanScheduleAutoSave()
    {
        // MarkDirty changes IsDirty, but IsDirty is not part of CanScheduleAutoSave's
        // formula — so the dependency-tracking notification should not fire.
        var vm = Make();
        vm.OnFileLoaded(@"C:\notes\ideas.ideanest");
        var changes = CapturePropertyChanges(vm);

        vm.MarkDirty();

        Assert.DoesNotContain(nameof(SaveStateViewModel.CanScheduleAutoSave), changes);
    }
}
