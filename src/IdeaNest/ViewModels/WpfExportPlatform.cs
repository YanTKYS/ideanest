using System.Windows;
using IdeaNest.Models;
using IdeaNest.Views;
using Microsoft.Win32;

namespace IdeaNest.ViewModels;

/// <summary>
/// WPF implementation of <see cref="IExportPlatform"/>. Lives here (alongside
/// MainViewModel) rather than in ExportViewModel so the latter can stay
/// WPF-free and testable from cross-platform xUnit.
/// </summary>
internal sealed class WpfExportPlatform : IExportPlatform
{
    public string? PromptSaveFilePath(string defaultFileName)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt",
            DefaultExt = ".md",
            FileName = defaultFileName,
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public NoteNestExportOptions? PromptNoteNestOptions()
    {
        var dlg = new NoteNestExportOptionsWindow
        {
            Owner = Application.Current?.MainWindow,
        };
        return dlg.ShowDialog() == true ? dlg.Options : null;
    }

    public void SetClipboard(string text) => Clipboard.SetText(text);

    public void ShowInformation(string message) => MessageBox.Show(
        message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message) => MessageBox.Show(
        message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Error);
}
