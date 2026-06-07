using System;
using System.IO;
using System.Linq;
using IdeaNest.Models;
using IdeaNest.Services;
using Xunit;

namespace IdeaNest.Tests.Services;

public class WorkspaceServiceTests
{
    [Theory]
    [InlineData("UI", "UI")]
    [InlineData(" UI ", "UI")]
    [InlineData("#UI", "UI")]
    [InlineData("##UI", "UI")]
    [InlineData("# # UI", "UI")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizeTag_StripsLeadingHashAndWhitespace(string? input, string expected)
    {
        Assert.Equal(expected, WorkspaceService.NormalizeTag(input!));
    }

    [Fact]
    public void NormalizeTags_RemovesEmptyAndDuplicates_PreservesFirstOccurrence()
    {
        var input = new[] { "UI", "#UI", "  ", "design", "UI", "#design" };

        var result = WorkspaceService.NormalizeTags(input);

        Assert.Equal(new[] { "UI", "design" }, result);
    }

    [Fact]
    public void NormalizeTags_TreatsTagsAsCaseSensitive()
    {
        var input = new[] { "UI", "ui", "Ui" };

        var result = WorkspaceService.NormalizeTags(input);

        Assert.Equal(new[] { "UI", "ui", "Ui" }, result);
    }

    [Fact]
    public void NormalizeTags_AcceptsNull()
    {
        Assert.Empty(WorkspaceService.NormalizeTags(null!));
    }

    [Fact]
    public void SaveThenLoad_RoundTrips_AllFields()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, "ws.ideanest");

        var ws = new Workspace
        {
            WorkspaceName = "Round Trip",
            Settings = new WorkspaceSettings
            {
                SearchText = "alpha",
                SelectedTag = "UI",
                SelectedColor = "blue",
                ShowArchived = true,
                TagPanelOpen = true,
                CardSize = "large",
                CardHeightMode = "auto",
                SortMode = "CreatedDesc",
                WindowWidth = 1234,
                WindowHeight = 789,
            },
            Ideas =
            {
                new Idea
                {
                    Title = "First",
                    Body = "body 1",
                    Tags = { "UI", "design" },
                    Color = "yellow",
                    IsPinned = true,
                },
                new Idea
                {
                    Title = "Second",
                    Body = "body 2",
                    Tags = { "ops" },
                    Color = "blue",
                    IsArchived = true,
                },
            },
        };

        WorkspaceService.Save(path, ws);
        var loaded = WorkspaceService.Load(path);

        Assert.Equal("Round Trip", loaded.WorkspaceName);
        Assert.Equal("alpha", loaded.Settings.SearchText);
        Assert.Equal("UI", loaded.Settings.SelectedTag);
        Assert.True(loaded.Settings.ShowArchived);
        Assert.True(loaded.Settings.TagPanelOpen);
        Assert.Equal("large", loaded.Settings.CardSize);
        Assert.Equal("auto", loaded.Settings.CardHeightMode);
        Assert.Equal("CreatedDesc", loaded.Settings.SortMode);
        Assert.Equal(1234, loaded.Settings.WindowWidth);
        Assert.Equal(789, loaded.Settings.WindowHeight);

        Assert.Equal(2, loaded.Ideas.Count);
        Assert.Equal("First", loaded.Ideas[0].Title);
        Assert.Equal(new[] { "UI", "design" }, loaded.Ideas[0].Tags);
        Assert.True(loaded.Ideas[0].IsPinned);
        Assert.True(loaded.Ideas[1].IsArchived);
    }

    [Fact]
    public void Save_CreatesBackupCopy_OnSecondSave()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, "ws.ideanest");

        WorkspaceService.Save(path, new Workspace { WorkspaceName = "v1" });
        Assert.False(File.Exists(path + ".bak"));

        WorkspaceService.Save(path, new Workspace { WorkspaceName = "v2" });

        Assert.True(File.Exists(path + ".bak"));
        Assert.Contains("\"v1\"", File.ReadAllText(path + ".bak"));
        Assert.Contains("\"v2\"", File.ReadAllText(path));
    }

    [Fact]
    public void Load_NormalizesIdeas_AssignsIdAndDefaultsAndDeduplicatesTags()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, "ws.ideanest");
        File.WriteAllText(path, """
        {
          "workspaceName": "Legacy",
          "ideas": [
            {
              "title": "Legacy idea",
              "body": "hi",
              "tags": ["#UI", "UI", "  ", "design"],
              "color": ""
            }
          ]
        }
        """);

        var loaded = WorkspaceService.Load(path);

        Assert.Single(loaded.Ideas);
        var idea = loaded.Ideas[0];
        Assert.False(string.IsNullOrEmpty(idea.Id));
        Assert.Equal(new[] { "UI", "design" }, idea.Tags);
        Assert.Equal("yellow", idea.Color);
        Assert.NotEqual(default, idea.CreatedAt);
        Assert.NotEqual(default, idea.UpdatedAt);
    }

    [Fact]
    public void Load_AcceptsFileMissingSettingsBlock_AndUsesDefaults()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, "ws.ideanest");
        File.WriteAllText(path, """
        {
          "workspaceName": "No Settings",
          "ideas": []
        }
        """);

        var loaded = WorkspaceService.Load(path);

        Assert.NotNull(loaded.Settings);
        Assert.Equal("fixed", loaded.Settings.CardHeightMode);
        Assert.Equal("medium", loaded.Settings.CardSize);
    }

    [Fact]
    public void Load_AcceptsFileMissingCardHeightMode_AndDefaultsToFixed()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, "ws.ideanest");
        File.WriteAllText(path, """
        {
          "workspaceName": "Pre 0.7.2",
          "ideas": [],
          "settings": {
            "cardSize": "large",
            "sortMode": "UpdatedDesc"
          }
        }
        """);

        var loaded = WorkspaceService.Load(path);

        Assert.Equal("large", loaded.Settings.CardSize);
        Assert.Equal("fixed", loaded.Settings.CardHeightMode);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ideanest-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
