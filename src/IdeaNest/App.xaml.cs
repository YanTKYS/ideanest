using System;
using System.Windows;
using IdeaNest.Services;
using IdeaNest.ViewModels;
using IdeaNest.Views;

namespace IdeaNest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryResolveInitialPath(e.Args, out var initialPath))
        {
            Shutdown();
            return;
        }

        var main = new MainWindow(initialPath);
        MainWindow = main;
        main.Closed += (_, _) => Shutdown();
        main.Show();
    }

    private static bool TryResolveInitialPath(string[] args, out string? path)
    {
        var action = StartupCoordinator.Resolve(args);

        if (action.Kind == StartupActionKind.DirectOpen)
        {
            path = action.Path;
            // Only add to history when the file actually parses — a corrupt
            // file should not get a privileged slot in recent files. The
            // workspace itself is re-loaded inside MainViewModel.LoadStartup,
            // which surfaces any parse error through its own MessageBox.
            if (!string.IsNullOrEmpty(path) && IsValidWorkspace(path))
            {
                AppSettingsService.AddRecentFile(path);
            }
            return true;
        }

        return TryRunStartupDialog(out path);
    }

    private static bool TryRunStartupDialog(out string? path)
    {
        while (true)
        {
            var settings = AppSettingsService.Load();
            var vm = new StartupViewModel(settings.RecentFiles);
            var dlg = new StartupWindow(vm);
            var dialogResult = dlg.ShowDialog();

            if (dialogResult != true || vm.Choice == StartupChoice.Cancel)
            {
                path = null;
                return false;
            }

            if (vm.Choice == StartupChoice.New)
            {
                path = null;
                return true;
            }

            // Choice == Open
            var picked = vm.SelectedPath;
            if (string.IsNullOrEmpty(picked))
            {
                continue;
            }

            try
            {
                // Validate by parsing; the workspace itself is re-loaded inside
                // MainViewModel.LoadStartup so the result here is discarded.
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

    private static bool IsValidWorkspace(string path)
    {
        try
        {
            WorkspaceService.Load(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
