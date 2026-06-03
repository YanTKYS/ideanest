using System;
using System.IO;
using System.Windows;
using IdeaNest.Services;
using IdeaNest.Views;

namespace IdeaNest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryResolveInitialPath(e.Args, out var initialPath, out var openedFromArg))
        {
            Shutdown();
            return;
        }

        if (!string.IsNullOrEmpty(initialPath) && openedFromArg)
        {
            AppSettingsService.AddRecentFile(initialPath);
        }

        var main = new MainWindow(initialPath);
        MainWindow = main;
        main.Closed += (_, _) => Shutdown();
        main.Show();
    }

    private static bool TryResolveInitialPath(string[] args, out string? path, out bool openedFromArg)
    {
        openedFromArg = false;

        if (args.Length > 0 && File.Exists(args[0]))
        {
            path = args[0];
            try
            {
                // Validate content; result discarded. LoadStartup re-loads it.
                // Only mark openedFromArg if the file is actually parseable so that
                // corrupt files are not added to the recent-files list.
                WorkspaceService.Load(args[0]);
                openedFromArg = true;
            }
            catch
            {
                // Invalid content — LoadStartup will surface the error; skip recent.
            }
            return true;
        }

        while (true)
        {
            var settings = AppSettingsService.Load();
            var dlg = new StartupWindow(settings.RecentFiles);
            if (dlg.ShowDialog() != true)
            {
                path = null;
                return false;
            }

            if (dlg.ChoseNew)
            {
                path = null;
                return true;
            }

            var picked = dlg.SelectedPath;
            if (string.IsNullOrEmpty(picked))
            {
                // Open clicked with no selection — show dialog again.
                continue;
            }

            try
            {
                // Validate by attempting to parse; the workspace itself is reloaded
                // inside MainViewModel.LoadStartup so the result here is discarded.
                WorkspaceService.Load(picked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ファイルを開けませんでした:\n{ex.Message}",
                    "IdeaNest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                continue;
            }

            AppSettingsService.AddRecentFile(picked);
            path = picked;
            return true;
        }
    }
}
