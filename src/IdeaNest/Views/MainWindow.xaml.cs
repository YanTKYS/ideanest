using System.ComponentModel;
using System.Windows;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _sizeTrackingEnabled;

    public MainWindow() : this(null) { }

    public MainWindow(string? initialFilePath)
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _vm.LoadStartup(initialFilePath);
        Loaded += (_, _) => { _vm.ApplyInitialWindowSize(this); _sizeTrackingEnabled = true; };
        SizeChanged += (_, _) => { if (_sizeTrackingEnabled) _vm.MarkDirty(); };
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_vm.ConfirmDiscardChanges()) e.Cancel = true;
    }
}
