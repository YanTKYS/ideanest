using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

public partial class PreviewIdeaWindow : Window
{
    private readonly IdeaCardViewModel _card;
    private readonly Action _onEdit;
    private readonly Action _onTogglePin;
    private readonly Action _onToggleArchive;
    private readonly Action _onCopyMarkdown;

    public PreviewIdeaWindow(
        IdeaCardViewModel card,
        Action onEdit,
        Action onTogglePin,
        Action onToggleArchive,
        Action onCopyMarkdown)
    {
        InitializeComponent();
        _card = card;
        _onEdit = onEdit;
        _onTogglePin = onTogglePin;
        _onToggleArchive = onToggleArchive;
        _onCopyMarkdown = onCopyMarkdown;
        DataContext = card;
        UpdateToggleButtonLabels();
        card.PropertyChanged += OnCardPropertyChanged;
        Closed += (_, _) => card.PropertyChanged -= OnCardPropertyChanged;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IdeaCardViewModel.IsPinned) or nameof(IdeaCardViewModel.IsArchived))
        {
            UpdateToggleButtonLabels();
        }
    }

    private void UpdateToggleButtonLabels()
    {
        PinButton.Content = _card.IsPinned ? "📌 ピン留め解除" : "📌 ピン留め";
        ArchiveButton.Content = _card.IsArchived ? "📤 アーカイブ解除" : "📥 アーカイブ";
    }

    private void OnEditClick(object sender, RoutedEventArgs e) => _onEdit();
    private void OnTogglePinClick(object sender, RoutedEventArgs e) => _onTogglePin();
    private void OnToggleArchiveClick(object sender, RoutedEventArgs e) => _onToggleArchive();
    private void OnCopyClick(object sender, RoutedEventArgs e) => _onCopyMarkdown();
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
