using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
            return;
        }

        // Ctrl+V: create a new card from clipboard text.
        // Only fires when focus is inside the card area (CardArea or its descendants)
        // so Ctrl+V in the tag panel, sort ComboBox, search box, etc. is unaffected.
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.OriginalSource is TextBoxBase) return;
            if (!IsDescendantOrSelf(e.OriginalSource as DependencyObject, CardArea)) return;
            if (_vm.PasteAsNewCard()) e.Handled = true;
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
        // Land initial keyboard focus on the card area so Ctrl+V works
        // immediately after startup without requiring an explicit click.
        CardArea.Focus();
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

    private static bool IsDescendantOrSelf(DependencyObject? element, DependencyObject ancestor)
    {
        for (var d = element; d != null; d = VisualTreeHelper.GetParent(d))
        {
            if (d == ancestor) return true;
        }
        return false;
    }

    private void OnCardAreaMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Buttons and other focusable inner controls mark MouseDown as Handled,
        // so this bubbling handler only runs when the click hit non-interactive
        // surface (empty state, ScrollViewer background, card body). In that
        // case move keyboard focus to CardArea so Ctrl+V is accepted.
        if (!CardArea.IsKeyboardFocusWithin) CardArea.Focus();
    }

    private void OnCardAreaDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasAcceptableDropPayload(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnCardAreaDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = e.Data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
        var textFiles = paths.Where(IsAcceptableTextFile).ToArray();
        if (textFiles.Length == 0) return;
        _vm.CreateCardsFromFiles(textFiles);
        e.Handled = true;
    }

    private static bool HasAcceptableDropPayload(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop)) return false;
        var paths = data.GetData(DataFormats.FileDrop) as string[];
        return paths != null && paths.Any(IsAcceptableTextFile);
    }

    private static bool IsAcceptableTextFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        return string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase);
    }
}
