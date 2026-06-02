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
        workspace.Settings ??= new();
        return workspace;
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
