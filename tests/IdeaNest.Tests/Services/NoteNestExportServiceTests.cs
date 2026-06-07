using System.Collections.Generic;
using IdeaNest.Models;
using IdeaNest.Services;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.Services;

public class NoteNestExportServiceTests
{
    [Fact]
    public void FormatAll_NumbersTitlesAndWrapsEachCardWithDividers()
    {
        var cards = new List<IdeaCardViewModel>
        {
            MakeCard("Alpha", "a"),
            MakeCard("Beta",  "b"),
        };

        var text = NoteNestExportService.FormatAll(
            cards, "", "", "", showArchived: false, new NoteNestExportOptions());

        Assert.Contains("# IdeaNestから取り込んだアイデア", text);
        Assert.Contains("## 1. Alpha", text);
        Assert.Contains("## 2. Beta", text);
    }

    [Fact]
    public void FormatAll_IncludeMetaFalse_OmitsTagsColorPinArchive()
    {
        var cards = new List<IdeaCardViewModel>
        {
            MakeCard("T", "body", new[] { "UI" }, color: "blue", pinned: true),
        };

        var text = NoteNestExportService.FormatAll(
            cards, "", "", "", showArchived: false,
            new NoteNestExportOptions { IncludeMeta = false, IncludeNoteMarker = false, IncludeTodoMarker = false });

        Assert.DoesNotContain("タグ:", text);
        Assert.DoesNotContain("色:", text);
        Assert.DoesNotContain("ピン留め:", text);
        Assert.DoesNotContain("アーカイブ:", text);
    }

    [Fact]
    public void FormatAll_IncludeMetaTrue_EmitsTagsAndMetaLines()
    {
        var cards = new List<IdeaCardViewModel>
        {
            MakeCard("T", "body", new[] { "UI", "design" }, color: "blue", pinned: true, archived: true),
        };

        var text = NoteNestExportService.FormatAll(
            cards, "", "", "", showArchived: true,
            new NoteNestExportOptions { IncludeMeta = true, IncludeNoteMarker = false, IncludeTodoMarker = false });

        Assert.Contains("タグ: #UI #design", text);
        Assert.Contains("色: 青", text);
        Assert.Contains("ピン留め: あり", text);
        Assert.Contains("アーカイブ: あり", text);
    }

    [Fact]
    public void FormatAll_IncludeNoteMarker_AppendsNoteMarker()
    {
        var text = NoteNestExportService.FormatAll(
            new List<IdeaCardViewModel> { MakeCard("T", "b") },
            "", "", "", showArchived: false,
            new NoteNestExportOptions { IncludeNoteMarker = true, IncludeTodoMarker = false, IncludeMeta = false });

        Assert.Contains("[NOTE] IdeaNestから移行したアイデア", text);
        Assert.DoesNotContain("[TODO]", text);
    }

    [Fact]
    public void FormatAll_IncludeTodoMarker_AppendsTodoMarker()
    {
        var text = NoteNestExportService.FormatAll(
            new List<IdeaCardViewModel> { MakeCard("T", "b") },
            "", "", "", showArchived: false,
            new NoteNestExportOptions { IncludeNoteMarker = false, IncludeTodoMarker = true, IncludeMeta = false });

        Assert.Contains("[TODO] 採用判断", text);
        Assert.DoesNotContain("[NOTE]", text);
    }

    [Fact]
    public void FormatAll_BothMarkersOff_OmitsMarkerBlock()
    {
        var text = NoteNestExportService.FormatAll(
            new List<IdeaCardViewModel> { MakeCard("T", "b") },
            "", "", "", showArchived: false,
            new NoteNestExportOptions { IncludeNoteMarker = false, IncludeTodoMarker = false, IncludeMeta = false });

        Assert.DoesNotContain("[NOTE]", text);
        Assert.DoesNotContain("[TODO]", text);
    }

    private static IdeaCardViewModel MakeCard(
        string title,
        string body,
        IEnumerable<string>? tags = null,
        string color = "yellow",
        bool pinned = false,
        bool archived = false)
    {
        return new IdeaCardViewModel(new Idea
        {
            Title = title,
            Body = body,
            Tags = tags is null ? new List<string>() : new List<string>(tags),
            Color = color,
            IsPinned = pinned,
            IsArchived = archived,
        });
    }
}
