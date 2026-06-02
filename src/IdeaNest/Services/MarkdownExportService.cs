using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IdeaNest.ViewModels;

namespace IdeaNest.Services;

public static class MarkdownExportService
{
    private static readonly Dictionary<string, string> ColorDisplayNames = new(StringComparer.Ordinal)
    {
        ["yellow"] = "黄",
        ["pink"]   = "ピンク",
        ["blue"]   = "青",
        ["green"]  = "緑",
        ["purple"] = "紫",
        ["orange"] = "オレンジ",
        ["gray"]   = "グレー",
        ["white"]  = "白",
    };

    // ── Public entry points ────────────────────────────────────────────────

    /// <summary>Single-card Markdown block (no leading "---" separator).</summary>
    public static string FormatCard(IdeaCardViewModel card)
    {
        var sb = new StringBuilder();
        AppendCardBlock(sb, card);
        return sb.ToString();
    }

    /// <summary>Full export text: header + all card blocks with "---" dividers.</summary>
    public static string FormatAll(
        IReadOnlyList<IdeaCardViewModel> cards,
        string searchText,
        string selectedTag,
        string selectedColor,
        bool showArchived)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# IdeaNest エクスポート");
        sb.AppendLine();
        sb.AppendLine($"- 出力日時: {DateTime.Now:yyyy/MM/dd HH:mm}");
        sb.AppendLine($"- 出力件数: {cards.Count}");

        if (!string.IsNullOrWhiteSpace(searchText))
            sb.AppendLine($"- 検索条件: {searchText}");
        if (!string.IsNullOrWhiteSpace(selectedTag))
            sb.AppendLine($"- タグ: #{selectedTag}");
        if (!string.IsNullOrWhiteSpace(selectedColor))
            sb.AppendLine($"- 色: {ColorDisplayName(selectedColor)}");
        sb.AppendLine($"- アーカイブ表示: {(showArchived ? "する" : "しない")}");

        sb.AppendLine();

        foreach (var card in cards)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            AppendCardBlock(sb, card);
        }

        return sb.ToString();
    }

    /// <summary>Write the full export to a UTF-8 file (no BOM).</summary>
    public static void Export(
        string path,
        IReadOnlyList<IdeaCardViewModel> cards,
        string searchText,
        string selectedTag,
        string selectedColor,
        bool showArchived)
    {
        var text = FormatAll(cards, searchText, selectedTag, selectedColor, showArchived);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static void AppendCardBlock(StringBuilder sb, IdeaCardViewModel card)
    {
        var title = string.IsNullOrWhiteSpace(card.Title) ? card.DisplayTitle : card.Title;
        sb.AppendLine($"## {title.Replace("\n", " ").Trim()}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(card.Body))
        {
            sb.AppendLine(card.Body.TrimEnd());
            sb.AppendLine();
        }

        var tagLine = card.Tags.Count > 0
            ? string.Join(" ", card.Tags.Select(t => $"#{t}"))
            : string.Empty;

        if (!string.IsNullOrEmpty(tagLine))
            sb.AppendLine($"Tags: {tagLine}");
        sb.AppendLine($"Color: {ColorDisplayName(card.Color)}");
        sb.AppendLine($"Pinned: {(card.IsPinned ? "true" : "false")}");
        sb.AppendLine($"Archived: {(card.IsArchived ? "true" : "false")}");
        sb.AppendLine($"CreatedAt: {card.CreatedAt:yyyy/MM/dd HH:mm}");
        sb.AppendLine($"UpdatedAt: {card.UpdatedAt:yyyy/MM/dd HH:mm}");
        sb.AppendLine();
    }

    public static string ColorDisplayName(string color) =>
        ColorDisplayNames.TryGetValue(color ?? string.Empty, out var name) ? name : (color ?? string.Empty);
}
