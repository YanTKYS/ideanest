using System.Windows;
using System.Windows.Controls;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

public partial class TagManagementWindow : Window
{
    private readonly IdeaNestWorkspaceViewModel _vm;

    public TagManagementWindow(IdeaNestWorkspaceViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    private void OnRenameClick(object sender, RoutedEventArgs e)
    {
        var oldName = (sender as Button)?.Tag as string;
        if (string.IsNullOrEmpty(oldName)) return;

        var newName = PromptWindow.Show(
            owner: this,
            header: $"タグ「#{oldName}」をリネーム",
            initialValue: oldName,
            hint: "既存タグ名を入力すると、そのタグに統合されます");

        if (newName is null) return;  // user cancelled
        newName = newName.Trim();
        if (newName == oldName || string.IsNullOrWhiteSpace(newName)) return;

        _vm.RenameTag(oldName, newName);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var tagName = (sender as Button)?.Tag as string;
        if (string.IsNullOrEmpty(tagName)) return;

        var result = ConfirmWindow.ShowOkCancel(
            owner: this,
            header: $"タグ「#{tagName}」を削除しますか？",
            message: $"すべてのカードから「#{tagName}」タグを外します。\nカード自体は削除されません。",
            primaryText: "削除",
            cancelText: "キャンセル");

        if (result == ConfirmResult.Primary)
        {
            _vm.DeleteTag(tagName);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
