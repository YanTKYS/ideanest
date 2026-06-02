using System.ComponentModel;
using System.Windows;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _vm.LoadStartup();
        Loaded += (_, _) => _vm.ApplyInitialWindowSize(this);
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_vm.ConfirmDiscardChanges())
        {
            e.Cancel = true;
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
