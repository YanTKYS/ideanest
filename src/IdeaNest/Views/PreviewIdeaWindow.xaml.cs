using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

public partial class PreviewIdeaWindow : Window
{
    private readonly IReadOnlyList<IdeaCardViewModel> _cards;
    private readonly Action<IdeaCardViewModel> _onEdit;
    private readonly Action<IdeaCardViewModel> _onTogglePin;
    private readonly Action<IdeaCardViewModel> _onToggleArchive;
    private readonly Action<IdeaCardViewModel> _onCopyMarkdown;
    private int _currentIndex;

    private IdeaCardViewModel CurrentCard => _cards[_currentIndex];

    public PreviewIdeaWindow(
        IReadOnlyList<IdeaCardViewModel> cards,
        int initialIndex,
        Action<IdeaCardViewModel> onEdit,
        Action<IdeaCardViewModel> onTogglePin,
        Action<IdeaCardViewModel> onToggleArchive,
        Action<IdeaCardViewModel> onCopyMarkdown)
    {
        InitializeComponent();
        _cards = cards;
        _onEdit = onEdit;
        _onTogglePin = onTogglePin;
        _onToggleArchive = onToggleArchive;
        _onCopyMarkdown = onCopyMarkdown;
        _currentIndex = initialIndex;
        DataContext = CurrentCard;
        UpdateButtonStates();
        CurrentCard.PropertyChanged += OnCardPropertyChanged;
        Closed += (_, _) => CurrentCard.PropertyChanged -= OnCardPropertyChanged;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void NavigateTo(int index)
    {
        if (index < 0 || index >= _cards.Count) return;
        CurrentCard.PropertyChanged -= OnCardPropertyChanged;
        _currentIndex = index;
        DataContext = CurrentCard;
        CurrentCard.PropertyChanged += OnCardPropertyChanged;
        UpdateButtonStates();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.Left:
                NavigateTo(_currentIndex - 1);
                e.Handled = true;
                break;
            case Key.Right:
                NavigateTo(_currentIndex + 1);
                e.Handled = true;
                break;
        }
    }

    private void OnCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IdeaCardViewModel.IsPinned) or nameof(IdeaCardViewModel.IsArchived))
        {
            UpdateButtonStates();
        }
    }

    private void UpdateButtonStates()
    {
        PinButton.Content = CurrentCard.IsPinned ? "📌 ピン留め解除" : "📌 ピン留め";
        ArchiveButton.Content = CurrentCard.IsArchived ? "📤 アーカイブ解除" : "📥 アーカイブ";
        PrevButton.IsEnabled = _currentIndex > 0;
        NextButton.IsEnabled = _currentIndex < _cards.Count - 1;
    }

    private void OnPrevClick(object sender, RoutedEventArgs e) => NavigateTo(_currentIndex - 1);
    private void OnNextClick(object sender, RoutedEventArgs e) => NavigateTo(_currentIndex + 1);
    private void OnEditClick(object sender, RoutedEventArgs e) => _onEdit(CurrentCard);
    private void OnTogglePinClick(object sender, RoutedEventArgs e) => _onTogglePin(CurrentCard);
    private void OnToggleArchiveClick(object sender, RoutedEventArgs e) => _onToggleArchive(CurrentCard);
    private void OnCopyClick(object sender, RoutedEventArgs e) => _onCopyMarkdown(CurrentCard);
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
