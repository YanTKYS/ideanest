using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace IdeaNest.Views;

public partial class StartupWindow : Window
{
    public string? SelectedPath { get; private set; }
    public bool ChoseNew { get; private set; }

    public StartupWindow(IEnumerable<string> recentFiles)
    {
        InitializeComponent();

        // Filter out missing files so the user never sees stale entries.
        var visible = recentFiles
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Select(p => new RecentFileItem(p))
            .ToList();

        RecentList.ItemsSource = visible;
        if (visible.Count == 0)
        {
            EmptyHint.Visibility = Visibility.Visible;
        }
    }

    private void OnNewClick(object sender, RoutedEventArgs e)
    {
        ChoseNew = true;
        DialogResult = true;
        Close();
    }

    private void OnOpenSelectedClick(object sender, RoutedEventArgs e)
    {
        TryAcceptSelection();
    }

    private void OnRecentDoubleClick(object sender, MouseButtonEventArgs e)
    {
        TryAcceptSelection();
    }

    private void TryAcceptSelection()
    {
        if (RecentList.SelectedItem is RecentFileItem item)
        {
            SelectedPath = item.FullPath;
            DialogResult = true;
            Close();
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class RecentFileItem
{
    public string FullPath { get; }
    public string DisplayName { get; }

    public RecentFileItem(string fullPath)
    {
        FullPath = fullPath;
        DisplayName = Path.GetFileName(fullPath);
    }
}
