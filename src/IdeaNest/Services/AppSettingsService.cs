using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using IdeaNest.Models;

namespace IdeaNest.Services;

public static class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IdeaNest",
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            // Corrupt settings should not block startup.
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // App settings are best-effort; failure should not crash the app.
        }
    }

    public static void AddRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var settings = Load();
        settings.RecentFiles = RecentFilesService.Add(settings.RecentFiles, path).ToList();
        Save(settings);
    }

    public static void RemoveRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var settings = Load();
        var updated = RecentFilesService.Remove(settings.RecentFiles, path).ToList();
        if (updated.Count == settings.RecentFiles.Count) return;
        settings.RecentFiles = updated;
        Save(settings);
    }

    public static void ClearRecentFiles()
    {
        var settings = Load();
        if (settings.RecentFiles.Count == 0) return;
        settings.RecentFiles.Clear();
        Save(settings);
    }
}
