using System.Windows;

namespace IdeaNest.Views;

public partial class PromptWindow : Window
{
    public string? Result { get; private set; }

    private PromptWindow() { InitializeComponent(); }

    public static string? Show(Window? owner, string header, string initialValue = "",
                                string? hint = null)
    {
        var dlg = new PromptWindow
        {
            Owner = owner,
        };
        dlg.HeaderText.Text = header;
        dlg.InputBox.Text   = initialValue;
        dlg.InputBox.SelectAll();

        if (!string.IsNullOrEmpty(hint))
        {
            dlg.MessageText.Text       = hint;
            dlg.MessageText.Visibility = Visibility.Visible;
        }

        dlg.ShowDialog();
        return dlg.Result;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        Result = InputBox.Text;
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
