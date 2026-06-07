using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdeaNest.Models;
using IdeaNest.Services;
using IdeaNest.ViewModels;
using Xunit;

namespace IdeaNest.Tests.ViewModels;

public class ExportViewModelTests
{
    // ── Test fakes ────────────────────────────────────────────────────────────

    private sealed class FakePlatform : IExportPlatform
    {
        public string? SavePathToReturn { get; set; }
        public NoteNestExportOptions? OptionsToReturn { get; set; }
        public bool ClipboardShouldThrow { get; set; }

        public List<string> InfoMessages { get; } = new();
        public List<string> ErrorMessages { get; } = new();
        public List<string> SavePathPrompts { get; } = new();
        public int OptionsPromptCount { get; private set; }
        public string? ClipboardText { get; private set; }
        public int ClipboardCalls { get; private set; }

        public string? PromptSaveFilePath(string defaultFileName)
        {
            SavePathPrompts.Add(defaultFileName);
            return SavePathToReturn;
        }

        public NoteNestExportOptions? PromptNoteNestOptions()
        {
            OptionsPromptCount++;
            return OptionsToReturn;
        }

        public void SetClipboard(string text)
        {
            ClipboardCalls++;
            if (ClipboardShouldThrow) throw new InvalidOperationException("clipboard locked");
            ClipboardText = text;
        }

        public void ShowInformation(string message) => InfoMessages.Add(message);
        public void ShowError(string message) => ErrorMessages.Add(message);
    }

    private static IdeaCardViewModel Card(string title, string body = "", params string[] tags)
        => new(new Idea
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Body = body,
            Tags = tags.ToList(),
            Color = "white",
            CreatedAt = new DateTime(2026, 6, 1, 10, 0, 0),
            UpdatedAt = new DateTime(2026, 6, 2, 11, 0, 0),
        });

    private static ExportViewModel Make(
        List<IdeaCardViewModel>? visible = null,
        ExportFilterContext? filter = null,
        IExportPlatform? platform = null,
        Action<string>? showStatus = null)
    {
        var cards = visible ?? new List<IdeaCardViewModel>();
        var ctx = filter ?? new ExportFilterContext("", "", "", false);
        return new ExportViewModel(
            getVisibleCards: () => cards,
            getFilterContext: () => ctx,
            platform: platform ?? new FakePlatform(),
            showStatus: showStatus ?? (_ => { }));
    }

    // ── ExportMarkdown ────────────────────────────────────────────────────────

    [Fact]
    public void ExportMarkdown_ZeroCards_ShowsInfo_AndDoesNotPromptSavePath()
    {
        var platform = new FakePlatform();
        var vm = Make(visible: new List<IdeaCardViewModel>(), platform: platform);

        vm.ExportMarkdown();

        Assert.Single(platform.InfoMessages);
        Assert.Contains("エクスポート対象", platform.InfoMessages[0]);
        Assert.Empty(platform.SavePathPrompts);
    }

    [Fact]
    public void ExportMarkdown_SaveCancelled_DoesNotWriteFile()
    {
        var platform = new FakePlatform { SavePathToReturn = null };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.ExportMarkdown();

        Assert.Single(platform.SavePathPrompts);
        Assert.Empty(platform.ErrorMessages);
    }

    [Fact]
    public void ExportMarkdown_WritesFileWithVisibleCards_UsingMarkdownService()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ideanest-export-{Guid.NewGuid()}.md");
        try
        {
            var platform = new FakePlatform { SavePathToReturn = path };
            var cards = new List<IdeaCardViewModel> { Card("hello", "body") };
            var ctx = new ExportFilterContext("kw", "UI", "blue", true);
            var vm = Make(visible: cards, filter: ctx, platform: platform);

            vm.ExportMarkdown();

            var expected = MarkdownExportService.FormatAll(
                cards, ctx.SearchText, ctx.SelectedTag, ctx.SelectedColor, ctx.ShowArchived);
            Assert.Equal(expected, File.ReadAllText(path));
            Assert.Empty(platform.ErrorMessages);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ExportMarkdown_DefaultFileName_HasMarkdownExtension()
    {
        var platform = new FakePlatform { SavePathToReturn = null };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.ExportMarkdown();

        Assert.Single(platform.SavePathPrompts);
        Assert.StartsWith("ideanest_export_", platform.SavePathPrompts[0]);
        Assert.EndsWith(".md", platform.SavePathPrompts[0]);
    }

    [Fact]
    public void ExportMarkdown_WriteFailure_ShowsErrorDialog()
    {
        // Use a path that can't be written (invalid characters).
        var invalid = Path.Combine(Path.GetTempPath(), "no-such-dir-" + Guid.NewGuid(), "x", "file.md");
        var platform = new FakePlatform { SavePathToReturn = invalid };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.ExportMarkdown();

        Assert.Single(platform.ErrorMessages);
        Assert.Contains("エクスポートに失敗", platform.ErrorMessages[0]);
    }

    // ── CopyCardMarkdown ──────────────────────────────────────────────────────

    [Fact]
    public void CopyCardMarkdown_NullCard_DoesNothing()
    {
        var platform = new FakePlatform();
        string? status = null;
        var vm = Make(platform: platform, showStatus: s => status = s);

        vm.CopyCardMarkdown(null);

        Assert.Equal(0, platform.ClipboardCalls);
        Assert.Null(status);
        Assert.Empty(platform.ErrorMessages);
    }

    [Fact]
    public void CopyCardMarkdown_SetsClipboardWithFormattedCard_AndShowsStatus()
    {
        var platform = new FakePlatform();
        string? status = null;
        var card = Card("title-x", "body-y");
        var vm = Make(platform: platform, showStatus: s => status = s);

        vm.CopyCardMarkdown(card);

        Assert.Equal(1, platform.ClipboardCalls);
        Assert.Equal(MarkdownExportService.FormatCard(card), platform.ClipboardText);
        Assert.Equal("カードをコピーしました。", status);
    }

    [Fact]
    public void CopyCardMarkdown_ClipboardThrows_ShowsErrorAndDoesNotSetStatus()
    {
        var platform = new FakePlatform { ClipboardShouldThrow = true };
        string? status = null;
        var vm = Make(platform: platform, showStatus: s => status = s);

        vm.CopyCardMarkdown(Card("a"));

        Assert.Single(platform.ErrorMessages);
        Assert.Null(status);
    }

    // ── CopyAllMarkdown ───────────────────────────────────────────────────────

    [Fact]
    public void CopyAllMarkdown_ZeroCards_ShowsInfo_AndDoesNotTouchClipboard()
    {
        var platform = new FakePlatform();
        var vm = Make(visible: new List<IdeaCardViewModel>(), platform: platform);

        vm.CopyAllMarkdown();

        Assert.Single(platform.InfoMessages);
        Assert.Contains("コピー対象", platform.InfoMessages[0]);
        Assert.Equal(0, platform.ClipboardCalls);
    }

    [Fact]
    public void CopyAllMarkdown_UsesVisibleCardsAndFilterContext()
    {
        var platform = new FakePlatform();
        string? status = null;
        var cards = new List<IdeaCardViewModel> { Card("a"), Card("b") };
        var ctx = new ExportFilterContext("kw", "UI", "blue", false);
        var vm = Make(visible: cards, filter: ctx, platform: platform, showStatus: s => status = s);

        vm.CopyAllMarkdown();

        Assert.Equal(MarkdownExportService.FormatAll(cards, "kw", "UI", "blue", false), platform.ClipboardText);
        Assert.Equal("表示中の2件をコピーしました。", status);
    }

    [Fact]
    public void CopyAllMarkdown_ClipboardThrows_ShowsError()
    {
        var platform = new FakePlatform { ClipboardShouldThrow = true };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.CopyAllMarkdown();

        Assert.Single(platform.ErrorMessages);
    }

    // ── ExportNoteNest ────────────────────────────────────────────────────────

    [Fact]
    public void ExportNoteNest_ZeroCards_ShowsInfo_AndDoesNotPromptOptions()
    {
        var platform = new FakePlatform();
        var vm = Make(visible: new List<IdeaCardViewModel>(), platform: platform);

        vm.ExportNoteNest();

        Assert.Single(platform.InfoMessages);
        Assert.Contains("NoteNest向け", platform.InfoMessages[0]);
        Assert.Equal(0, platform.OptionsPromptCount);
        Assert.Empty(platform.SavePathPrompts);
    }

    [Fact]
    public void ExportNoteNest_OptionsCancelled_DoesNotPromptSavePath()
    {
        var platform = new FakePlatform { OptionsToReturn = null };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.ExportNoteNest();

        Assert.Equal(1, platform.OptionsPromptCount);
        Assert.Empty(platform.SavePathPrompts);
    }

    [Fact]
    public void ExportNoteNest_SaveCancelled_DoesNotWrite()
    {
        var platform = new FakePlatform
        {
            OptionsToReturn = new NoteNestExportOptions(),
            SavePathToReturn = null,
        };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.ExportNoteNest();

        Assert.Single(platform.SavePathPrompts);
        Assert.Empty(platform.ErrorMessages);
    }

    [Fact]
    public void ExportNoteNest_WritesFileWithVisibleCards()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ideanest-nn-{Guid.NewGuid()}.md");
        try
        {
            var opts = new NoteNestExportOptions();
            var platform = new FakePlatform { OptionsToReturn = opts, SavePathToReturn = path };
            var cards = new List<IdeaCardViewModel> { Card("hello") };
            var ctx = new ExportFilterContext("", "", "", false);
            var vm = Make(visible: cards, filter: ctx, platform: platform);

            vm.ExportNoteNest();

            var expected = NoteNestExportService.FormatAll(
                cards, ctx.SearchText, ctx.SelectedTag, ctx.SelectedColor, ctx.ShowArchived, opts);
            Assert.Equal(expected, File.ReadAllText(path));
            Assert.Empty(platform.ErrorMessages);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ExportNoteNest_DefaultFileName_HasNoteNestPrefix()
    {
        var platform = new FakePlatform
        {
            OptionsToReturn = new NoteNestExportOptions(),
            SavePathToReturn = null,
        };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.ExportNoteNest();

        Assert.Single(platform.SavePathPrompts);
        Assert.StartsWith("ideanest_notenest_", platform.SavePathPrompts[0]);
        Assert.EndsWith(".md", platform.SavePathPrompts[0]);
    }

    [Fact]
    public void ExportNoteNest_WriteFailure_ShowsError()
    {
        var invalid = Path.Combine(Path.GetTempPath(), "no-such-dir-" + Guid.NewGuid(), "x", "file.md");
        var platform = new FakePlatform
        {
            OptionsToReturn = new NoteNestExportOptions(),
            SavePathToReturn = invalid,
        };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.ExportNoteNest();

        Assert.Single(platform.ErrorMessages);
        Assert.Contains("エクスポートに失敗", platform.ErrorMessages[0]);
    }

    // ── CopyNoteNest ──────────────────────────────────────────────────────────

    [Fact]
    public void CopyNoteNest_ZeroCards_ShowsInfo_AndDoesNotPromptOptions()
    {
        var platform = new FakePlatform();
        var vm = Make(visible: new List<IdeaCardViewModel>(), platform: platform);

        vm.CopyNoteNest();

        Assert.Single(platform.InfoMessages);
        Assert.Equal(0, platform.OptionsPromptCount);
        Assert.Equal(0, platform.ClipboardCalls);
    }

    [Fact]
    public void CopyNoteNest_OptionsCancelled_DoesNotTouchClipboard()
    {
        var platform = new FakePlatform { OptionsToReturn = null };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.CopyNoteNest();

        Assert.Equal(1, platform.OptionsPromptCount);
        Assert.Equal(0, platform.ClipboardCalls);
    }

    [Fact]
    public void CopyNoteNest_UsesVisibleCardsAndFilterContext()
    {
        var platform = new FakePlatform { OptionsToReturn = new NoteNestExportOptions() };
        string? status = null;
        var cards = new List<IdeaCardViewModel> { Card("a"), Card("b"), Card("c") };
        var ctx = new ExportFilterContext("kw", "UI", "blue", false);
        var vm = Make(visible: cards, filter: ctx, platform: platform, showStatus: s => status = s);

        vm.CopyNoteNest();

        Assert.Equal(
            NoteNestExportService.FormatAll(cards, "kw", "UI", "blue", false, platform.OptionsToReturn!),
            platform.ClipboardText);
        Assert.Equal("表示中の3件をNoteNest向け形式でコピーしました。", status);
    }

    [Fact]
    public void CopyNoteNest_ClipboardThrows_ShowsError()
    {
        var platform = new FakePlatform
        {
            OptionsToReturn = new NoteNestExportOptions(),
            ClipboardShouldThrow = true,
        };
        var vm = Make(visible: new List<IdeaCardViewModel> { Card("a") }, platform: platform);

        vm.CopyNoteNest();

        Assert.Single(platform.ErrorMessages);
    }

    // ── Snapshot semantics ────────────────────────────────────────────────────

    [Fact]
    public void GetVisibleCards_ReadAtInvocationTime_NotConstructionTime()
    {
        var cards = new List<IdeaCardViewModel> { Card("a") };
        var platform = new FakePlatform();
        var vm = new ExportViewModel(
            getVisibleCards: () => cards,
            getFilterContext: () => new ExportFilterContext("", "", "", false),
            platform: platform,
            showStatus: _ => { });

        cards.Clear(); // simulate filter change emptying the visible set
        vm.ExportMarkdown();

        Assert.Single(platform.InfoMessages); // zero-card branch took effect
    }

    [Fact]
    public void GetFilterContext_ReadAtInvocationTime_NotConstructionTime()
    {
        var cards = new List<IdeaCardViewModel> { Card("a"), Card("b") };
        var ctx = new ExportFilterContext("", "", "", false);
        var platform = new FakePlatform();
        var vm = new ExportViewModel(
            getVisibleCards: () => cards,
            getFilterContext: () => ctx,
            platform: platform,
            showStatus: _ => { });

        ctx = new ExportFilterContext("kw", "Tag1", "red", true);
        vm.CopyAllMarkdown();

        var expected = MarkdownExportService.FormatAll(cards, "kw", "Tag1", "red", true);
        Assert.Equal(expected, platform.ClipboardText);
    }
}
