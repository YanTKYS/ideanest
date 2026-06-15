using System;
using System.Windows;

namespace IdeaNest.ViewModels;

/// <summary>Centralizes the WPF host-dependent operations used by the workspace.</summary>
public sealed class WorkspaceUiService
{
    private Func<Window?> _ownerResolver = () => Application.Current?.MainWindow;

    public Window? Owner => _ownerResolver();

    public void SetOwnerResolver(Func<Window?> resolver) =>
        _ownerResolver = resolver ?? (() => Application.Current?.MainWindow);

    public string? GetClipboardText()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; }
    }

    public void SetClipboardText(string text) => Clipboard.SetText(text);

    public void ShowInformation(string message) =>
        MessageBox.Show(Owner, message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowWarning(string message) =>
        MessageBox.Show(Owner, message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowError(string message) =>
        MessageBox.Show(Owner, message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Error);
}
