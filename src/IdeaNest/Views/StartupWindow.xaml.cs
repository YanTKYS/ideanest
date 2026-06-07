using System.IO;
using System.Windows;
using System.Windows.Input;
using IdeaNest.Services;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

public partial class StartupWindow : Window
{
    private readonly StartupViewModel _vm;

    public StartupWindow(StartupViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        RecentList.ItemsSource = _vm.Items;
        RecentList.SelectionChanged += (_, _) => SyncButtons();

        SyncButtons();
        if (!_vm.HasItems)
            EmptyHint.Visibility = Visibility.Visible;
    }

    private void SyncButtons()
    {
        OpenButton.IsEnabled = RecentList.SelectedItem != null;
        ClearHistoryButton.IsEnabled = _vm.HasItems;
    }

    private void OnNewClick(object sender, RoutedEventArgs e)
    {
        _vm.ChooseNew();
        DialogResult = true;
        Close();
    }

    private void OnOpenSelectedClick(object sender, RoutedEventArgs e) => TryAcceptSelection();

    private void OnRecentDoubleClick(object sender, MouseButtonEventArgs e) => TryAcceptSelection();

    private void OnClearHistoryClick(object sender, RoutedEventArgs e)
    {
        AppSettingsService.ClearRecentFiles();
        _vm.ClearItems();
        EmptyHint.Visibility = Visibility.Visible;
        SyncButtons();
    }

    private void TryAcceptSelection()
    {
        if (RecentList.SelectedItem is not RecentFileItem item) return;

        if (_vm.TryChooseOpen(item.FullPath))
        {
            DialogResult = true;
            Close();
            return;
        }

        // File no longer on disk — warn, drop the entry, and re-prompt.
        MessageBox.Show(
            $"ファイルが見つかりませんでした:\n{item.FullPath}\n\n履歴から外します。",
            "IdeaNest",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        _vm.RemoveItem(item);
        AppSettingsService.RemoveRecentFile(item.FullPath);
        if (!_vm.HasItems)
            EmptyHint.Visibility = Visibility.Visible;
        SyncButtons();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _vm.Cancel();
        DialogResult = false;
        Close();
    }
}
