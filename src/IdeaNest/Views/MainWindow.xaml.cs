using System.ComponentModel;
using System.Windows;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _sizeTrackingEnabled;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _vm.LoadStartup();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.ApplyInitialWindowSize(this);
        _sizeTrackingEnabled = true;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_sizeTrackingEnabled)
        {
            _vm.MarkDirty();
        }
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
