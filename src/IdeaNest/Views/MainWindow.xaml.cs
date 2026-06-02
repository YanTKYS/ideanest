using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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
        PreviewKeyDown += OnWindowPreviewKeyDown;
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            FocusSearch();
            e.Handled = true;
        }
    }

    private void OnFocusSearchClick(object sender, RoutedEventArgs e)
    {
        FocusSearch();
    }

    private void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                _vm.SearchText = string.Empty;
            }
            else
            {
                Keyboard.ClearFocus();
            }
            e.Handled = true;
        }
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
