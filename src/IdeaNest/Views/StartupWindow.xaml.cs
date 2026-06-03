using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using IdeaNest.Services;

namespace IdeaNest.Views;

public partial class StartupWindow : Window
{
    public string? SelectedPath { get; private set; }
    public bool ChoseNew { get; private set; }

    private readonly ObservableCollection<RecentFileItem> _items;

    public StartupWindow(IEnumerable<string> recentFiles)
    {
        InitializeComponent();

        _items = new ObservableCollection<RecentFileItem>(
            recentFiles
                .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                .Select(p => new RecentFileItem(p)));

        RecentList.ItemsSource = _items;
        RecentList.SelectionChanged += (_, _) => SyncOpenButton();

        if (_items.Count == 0)
            EmptyHint.Visibility = Visibility.Visible;
    }

    private void SyncOpenButton()
    {
        OpenButton.IsEnabled = RecentList.SelectedItem != null;
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
        if (RecentList.SelectedItem is not RecentFileItem item)
            return;

        if (!File.Exists(item.FullPath))
        {
            MessageBox.Show(
                $"ファイルが見つかりませんでした:\n{item.FullPath}\n\n履歴から外します。",
                "IdeaNest",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _items.Remove(item);
            AppSettingsService.RemoveRecentFile(item.FullPath);
            if (_items.Count == 0)
                EmptyHint.Visibility = Visibility.Visible;
            SyncOpenButton();
            return;
        }

        SelectedPath = item.FullPath;
        DialogResult = true;
        Close();
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
