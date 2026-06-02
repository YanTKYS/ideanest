using System.Windows;

namespace IdeaNest.Views;

public partial class EditIdeaWindow : Window
{
    public EditIdeaWindow()
    {
        InitializeComponent();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
