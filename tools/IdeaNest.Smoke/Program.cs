using System;
using System.IO;
using System.Linq;
using IdeaNest.Models;
using IdeaNest.Services;

namespace IdeaNest.Smoke;

internal static class Program
{
    private static int _failures;

    private static void Check(bool condition, string label)
    {
        if (condition)
        {
            Console.WriteLine($"  OK   {label}");
        }
        else
        {
            Console.WriteLine($"  FAIL {label}");
            _failures++;
        }
    }

    private static int Main()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "ideanest-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        var path = Path.Combine(tmpDir, "test.ideanest");
        var bakPath = path + ".bak";

        Console.WriteLine($"smoke directory: {tmpDir}");

        // 1. Build a workspace and save it.
        var ws = new Workspace
        {
            WorkspaceName = "Smoke",
            Settings = new WorkspaceSettings
            {
                SearchText = "alpha",
                SelectedTag = "UI",
                SelectedColor = "blue",
                ShowArchived = true,
                WindowWidth = 1234,
                WindowHeight = 789,
            },
            Ideas =
            {
                new Idea { Title = "T1", Body = "body 1", Tags = { "UI", "開発" }, Color = "yellow",  IsPinned = true },
                new Idea { Title = "T2", Body = "body 2", Tags = { "記事" },        Color = "blue",    IsArchived = true },
            },
        };
        WorkspaceService.Save(path, ws);
        Check(File.Exists(path), "save creates .ideanest file");
        Check(!File.Exists(bakPath), "first save does NOT create .bak (no prior file)");

        // 2. Modify and save again — .bak should now appear.
        ws.Ideas.Add(new Idea { Title = "T3", Body = "body 3", Tags = { "新規" } });
        WorkspaceService.Save(path, ws);
        Check(File.Exists(bakPath), "second save creates .bak");

        // 3. Reload and verify roundtrip.
        var loaded = WorkspaceService.Load(path);
        Check(loaded.Version == "0.1.0", "version preserved");
        Check(loaded.WorkspaceName == "Smoke", "workspaceName preserved");
        Check(loaded.Ideas.Count == 3, "all ideas preserved");
        Check(loaded.Ideas[0].Title == "T1" && loaded.Ideas[0].IsPinned, "pin preserved");
        Check(loaded.Ideas[1].IsArchived, "archive flag preserved");
        Check(loaded.Ideas[0].Tags.SequenceEqual(new[] { "UI", "開発" }), "tags preserved (UTF-8)");
        Check(loaded.Settings.SearchText == "alpha", "settings.searchText preserved");
        Check(loaded.Settings.SelectedTag == "UI", "settings.selectedTag preserved");
        Check(loaded.Settings.SelectedColor == "blue", "settings.selectedColor preserved");
        Check(loaded.Settings.ShowArchived, "settings.showArchived preserved");
        Check(Math.Abs(loaded.Settings.WindowWidth - 1234) < 0.01, "settings.windowWidth preserved");
        Check(Math.Abs(loaded.Settings.WindowHeight - 789) < 0.01, "settings.windowHeight preserved");

        // 4. JSON file content sanity check.
        var json = File.ReadAllText(path);
        Check(json.Contains("\"version\""), "json has version field");
        Check(json.Contains("\"workspaceName\""), "json has workspaceName field");
        Check(json.Contains("\"ideas\""), "json has ideas array");
        Check(json.Contains("\"settings\""), "json has settings object");
        Check(json.Contains("\"isPinned\""), "json has isPinned");
        Check(json.Contains("\"isArchived\""), "json has isArchived");
        Check(json.Contains("\"createdAt\""), "json has createdAt");
        Check(json.Contains("\"updatedAt\""), "json has updatedAt");
        Check(json.Contains("開発"), "non-ASCII characters preserved without escaping");

        // 5. Malformed / partial JSON should be normalized by Load.
        var brokenPath = Path.Combine(tmpDir, "broken.ideanest");
        File.WriteAllText(brokenPath, """
            {
              "version": "0.1.0",
              "workspaceName": "Broken",
              "ideas": [
                null,
                { "id": "", "title": null, "body": null, "tags": null, "color": "" },
                { "id": "x", "title": "ok", "body": "ok", "tags": ["a", "", "  "], "color": "blue" }
              ]
            }
            """);
        var broken = WorkspaceService.Load(brokenPath);
        Check(broken.Ideas.Count == 2, "broken: null element in array is dropped");
        Check(!string.IsNullOrEmpty(broken.Ideas[0].Id), "broken: empty id is regenerated");
        Check(broken.Ideas[0].Title == string.Empty, "broken: null title becomes empty");
        Check(broken.Ideas[0].Body == string.Empty, "broken: null body becomes empty");
        Check(broken.Ideas[0].Tags != null && broken.Ideas[0].Tags.Count == 0, "broken: null tags becomes empty list");
        Check(broken.Ideas[0].Color == "yellow", "broken: empty color falls back to yellow");
        Check(broken.Ideas[1].Tags.Count == 1 && broken.Ideas[1].Tags[0] == "a", "broken: blank tag entries are dropped");

        try { Directory.Delete(tmpDir, recursive: true); } catch { /* ignore */ }

        Console.WriteLine();
        Console.WriteLine(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} CHECKS FAILED");
        return _failures == 0 ? 0 : 1;
    }
}
