using System.Windows;
using IdeaNest.Models;

namespace IdeaNest.Views;

public partial class NoteNestExportOptionsWindow : Window
{
    public NoteNestExportOptions? Options { get; private set; }

    public NoteNestExportOptionsWindow()
    {
        InitializeComponent();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        Options = new NoteNestExportOptions
        {
            IncludeNoteMarker = ChkNote.IsChecked == true,
            IncludeTodoMarker = ChkTodo.IsChecked == true,
            IncludeMeta       = ChkMeta.IsChecked == true,
        };
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
