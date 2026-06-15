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

public partial class IdeaNestWorkspaceView : UserControl
{
    public static readonly DependencyProperty ShowMenuProperty = DependencyProperty.Register(
        nameof(ShowMenu), typeof(bool), typeof(IdeaNestWorkspaceView),
        new PropertyMetadata(true));

    public bool ShowMenu
    {
        get => (bool)GetValue(ShowMenuProperty);
        set => SetValue(ShowMenuProperty, value);
    }

    private IdeaNestWorkspaceViewModel? Workspace => DataContext as IdeaNestWorkspaceViewModel;

    public IdeaNestWorkspaceView()
    {
        InitializeComponent();
        PreviewKeyDown += OnWindowPreviewKeyDown;
        DataContextChanged += (_, _) => ConfigureWorkspace();
        Loaded += (_, _) =>
        {
            ConfigureWorkspace();
            FocusWorkspace();
        };
    }

    public void FocusWorkspace() => CardArea.Focus();

    private void ConfigureWorkspace()
    {
        Workspace?.SetOwnerResolver(() => Window.GetWindow(this));
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
            if (Workspace?.PasteAsNewCard() == true) e.Handled = true;
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
                if (Workspace is { } workspace) workspace.SearchText = string.Empty;
            }
            else
            {
                Keyboard.ClearFocus();
            }
            e.Handled = true;
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Workspace?.HostCommands.Exit?.Execute(null);
    }

    private void OnTutorialClick(object sender, RoutedEventArgs e)
    {
        Workspace?.HostCommands.ShowTutorial?.Execute(null);
    }

    private void OnWorkspaceHostPreviewClick(object sender, RoutedEventArgs e)
    {
        var window = new WorkspaceHostPreviewWindow { Owner = Window.GetWindow(this) };
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

        Workspace?.PreviewIdeaCommand.Execute(card);
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
        Workspace?.CreateCardsFromFiles(textFiles);
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
