using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IdeaNest.Models;
using IdeaNest.ViewModels;

namespace IdeaNest.Services;

public static class NoteNestExportService
{
    // NoteNest marker lines — kept here so future changes only require editing this one place.
    private const string MarkerNote = "[NOTE] IdeaNestから移行したアイデア";
    private const string MarkerTodo = "[TODO] 採用判断";

    // ── Public entry points ────────────────────────────────────────────────

    public static string FormatAll(
        IReadOnlyList<IdeaCardViewModel> cards,
        string searchText,
        string selectedTag,
        string selectedColor,
        bool showArchived,
        NoteNestExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# IdeaNestから取り込んだアイデア");
        sb.AppendLine();
        sb.AppendLine($"出力日時: {DateTime.Now:yyyy/MM/dd HH:mm}");
        sb.AppendLine($"出力件数: {cards.Count}");

        if (!string.IsNullOrWhiteSpace(searchText))
            sb.AppendLine($"検索条件: {searchText}");
        if (!string.IsNullOrWhiteSpace(selectedTag))
            sb.AppendLine($"タグ: #{selectedTag}");
        if (!string.IsNullOrWhiteSpace(selectedColor))
            sb.AppendLine($"色: {MarkdownExportService.ColorDisplayName(selectedColor)}");
        sb.AppendLine($"アーカイブ表示: {(showArchived ? "する" : "しない")}");

        sb.AppendLine();

        for (int i = 0; i < cards.Count; i++)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            AppendCardBlock(sb, cards[i], i + 1, options);
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static void Export(
        string path,
        IReadOnlyList<IdeaCardViewModel> cards,
        string searchText,
        string selectedTag,
        string selectedColor,
        bool showArchived,
        NoteNestExportOptions options)
    {
        var text = FormatAll(cards, searchText, selectedTag, selectedColor, showArchived, options);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static void AppendCardBlock(
        StringBuilder sb,
        IdeaCardViewModel card,
        int number,
        NoteNestExportOptions options)
    {
        var title = string.IsNullOrWhiteSpace(card.Title) ? card.DisplayTitle : card.Title;
        sb.AppendLine($"## {number}. {title.Replace("\n", " ").Trim()}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(card.Body))
        {
            sb.AppendLine(card.Body.TrimEnd());
            sb.AppendLine();
        }

        if (options.IncludeMeta)
        {
            var tagLine = card.Tags.Count > 0
                ? string.Join(" ", card.Tags.Select(t => $"#{t}"))
                : string.Empty;

            if (!string.IsNullOrEmpty(tagLine))
                sb.AppendLine($"タグ: {tagLine}");
            sb.AppendLine($"色: {MarkdownExportService.ColorDisplayName(card.Color)}");
            sb.AppendLine($"ピン留め: {(card.IsPinned ? "あり" : "なし")}");
            sb.AppendLine($"アーカイブ: {(card.IsArchived ? "あり" : "なし")}");
            sb.AppendLine($"作成日: {card.CreatedAt:yyyy/MM/dd HH:mm}");
            sb.AppendLine($"更新日: {card.UpdatedAt:yyyy/MM/dd HH:mm}");
            sb.AppendLine();
        }

        if (options.IncludeNoteMarker || options.IncludeTodoMarker)
        {
            if (options.IncludeNoteMarker) sb.AppendLine(MarkerNote);
            if (options.IncludeTodoMarker) sb.AppendLine(MarkerTodo);
            sb.AppendLine();
        }
    }
}
