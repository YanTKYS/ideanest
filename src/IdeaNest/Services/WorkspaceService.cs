using System;
using System.IO;
using System.Text;
using System.Text.Json;
using IdeaNest.Models;

namespace IdeaNest.Services;

public static class WorkspaceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static Workspace Load(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var workspace = JsonSerializer.Deserialize<Workspace>(json, JsonOptions)
            ?? throw new InvalidDataException("Invalid .ideanest file");
        workspace.Ideas ??= new();
        workspace.Ideas.RemoveAll(i => i is null);
        workspace.Settings ??= new();
        foreach (var idea in workspace.Ideas)
        {
            Normalize(idea);
        }
        return workspace;
    }

    private static void Normalize(Idea idea)
    {
        if (string.IsNullOrEmpty(idea.Id))
        {
            idea.Id = Guid.NewGuid().ToString();
        }
        idea.Title ??= string.Empty;
        idea.Body ??= string.Empty;
        idea.Tags ??= new();
        idea.Tags.RemoveAll(string.IsNullOrWhiteSpace);
        if (string.IsNullOrWhiteSpace(idea.Color))
        {
            idea.Color = "yellow";
        }
        if (idea.CreatedAt == default)
        {
            idea.CreatedAt = DateTime.Now;
        }
        if (idea.UpdatedAt == default)
        {
            idea.UpdatedAt = idea.CreatedAt;
        }
    }

    public static void Save(string path, Workspace workspace)
    {
        if (File.Exists(path))
        {
            var bakPath = path + ".bak";
            try
            {
                File.Copy(path, bakPath, overwrite: true);
            }
            catch
            {
                // Best-effort backup; do not block save on .bak failure.
            }
        }

        var json = JsonSerializer.Serialize(workspace, JsonOptions);
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json, new UTF8Encoding(false));

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, destinationBackupFileName: null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }
}
