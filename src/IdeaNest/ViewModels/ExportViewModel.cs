using System;
using System.Collections.Generic;
using IdeaNest.Services;

namespace IdeaNest.ViewModels;

/// <summary>
/// Snapshot of the filter state at the moment an export/copy was triggered.
/// Bundled into a record so ExportViewModel can pull it through a single Func
/// without taking a direct dependency on FilterViewModel.
/// </summary>
public sealed record ExportFilterContext(
    string SearchText,
    string SelectedTag,
    string SelectedColor,
    bool ShowArchived);

/// <summary>
/// Owns Markdown / NoteNest export and clipboard-copy workflows.
/// Pure logic: WPF dependencies (file dialog, MessageBox, Clipboard,
/// options dialog) are routed through IExportPlatform so this class is
/// fully testable without a UI.
/// </summary>
public class ExportViewModel
{
    private readonly Func<IReadOnlyList<IdeaCardViewModel>> _getVisibleCards;
    private readonly Func<ExportFilterContext> _getFilterContext;
    private readonly IExportPlatform _platform;
    private readonly Action<string> _showStatus;

    public ExportViewModel(
        Func<IReadOnlyList<IdeaCardViewModel>> getVisibleCards,
        Func<ExportFilterContext> getFilterContext,
        IExportPlatform platform,
        Action<string> showStatus)
    {
        _getVisibleCards = getVisibleCards;
        _getFilterContext = getFilterContext;
        _platform = platform;
        _showStatus = showStatus;
    }

    // ── Markdown ─────────────────────────────────────────────────────────────

    public void ExportMarkdown()
    {
        var cards = _getVisibleCards();
        if (cards.Count == 0)
        {
            _platform.ShowInformation("エクスポート対象のカードがありません。");
            return;
        }

        var path = _platform.PromptSaveFilePath(
            $"ideanest_export_{DateTime.Now:yyyyMMdd_HHmm}.md");
        if (path == null) return;

        var ctx = _getFilterContext();
        try
        {
            MarkdownExportService.Export(
                path, cards, ctx.SearchText, ctx.SelectedTag, ctx.SelectedColor, ctx.ShowArchived);
        }
        catch (Exception ex)
        {
            _platform.ShowError($"エクスポートに失敗しました:\n{ex.Message}");
        }
    }

    public void CopyCardMarkdown(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var text = MarkdownExportService.FormatCard(card);
        try
        {
            _platform.SetClipboard(text);
            _showStatus("カードをコピーしました。");
        }
        catch (Exception ex)
        {
            _platform.ShowError($"クリップボードへのコピーに失敗しました:\n{ex.Message}");
        }
    }

    public void CopyAllMarkdown()
    {
        var cards = _getVisibleCards();
        if (cards.Count == 0)
        {
            _platform.ShowInformation("コピー対象のカードがありません。");
            return;
        }
        var ctx = _getFilterContext();
        var text = MarkdownExportService.FormatAll(
            cards, ctx.SearchText, ctx.SelectedTag, ctx.SelectedColor, ctx.ShowArchived);
        try
        {
            _platform.SetClipboard(text);
            _showStatus($"表示中の{cards.Count}件をコピーしました。");
        }
        catch (Exception ex)
        {
            _platform.ShowError($"クリップボードへのコピーに失敗しました:\n{ex.Message}");
        }
    }

    // ── NoteNest ─────────────────────────────────────────────────────────────

    public void ExportNoteNest()
    {
        var cards = _getVisibleCards();
        if (cards.Count == 0)
        {
            _platform.ShowInformation("NoteNest向けに出力するカードがありません。");
            return;
        }

        var options = _platform.PromptNoteNestOptions();
        if (options == null) return;

        var path = _platform.PromptSaveFilePath(
            $"ideanest_notenest_{DateTime.Now:yyyyMMdd_HHmm}.md");
        if (path == null) return;

        var ctx = _getFilterContext();
        try
        {
            NoteNestExportService.Export(
                path, cards, ctx.SearchText, ctx.SelectedTag, ctx.SelectedColor, ctx.ShowArchived, options);
        }
        catch (Exception ex)
        {
            _platform.ShowError($"エクスポートに失敗しました:\n{ex.Message}");
        }
    }

    public void CopyNoteNest()
    {
        var cards = _getVisibleCards();
        if (cards.Count == 0)
        {
            _platform.ShowInformation("NoteNest向けに出力するカードがありません。");
            return;
        }

        var options = _platform.PromptNoteNestOptions();
        if (options == null) return;

        var ctx = _getFilterContext();
        var text = NoteNestExportService.FormatAll(
            cards, ctx.SearchText, ctx.SelectedTag, ctx.SelectedColor, ctx.ShowArchived, options);
        try
        {
            _platform.SetClipboard(text);
            _showStatus($"表示中の{cards.Count}件をNoteNest向け形式でコピーしました。");
        }
        catch (Exception ex)
        {
            _platform.ShowError($"クリップボードへのコピーに失敗しました:\n{ex.Message}");
        }
    }
}
