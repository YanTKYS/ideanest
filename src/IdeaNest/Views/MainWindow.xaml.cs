using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

    private void OnTutorialClick(object sender, RoutedEventArgs e)
    {
        var window = new TutorialWindow { Owner = this };
        window.ShowDialog();
    }

    private void OnCardMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not IdeaCardViewModel card)
            return;

        // Ignore clicks that originated inside a Button (hover ✎/📌/📥/🗑) —
        // those have their own commands and shouldn't also open the preview.
        if (e.OriginalSource is DependencyObject src && IsInsideButton(src))
            return;

        _vm.PreviewIdeaCommand.Execute(card);
    }

    private static bool IsInsideButton(DependencyObject src)
    {
        for (var d = src; d != null; d = VisualTreeHelper.GetParent(d))
        {
            if (d is ButtonBase) return true;
        }
        return false;
    }
}
