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
    private readonly WorkspaceUiService _ui;

    public WpfExportPlatform(WorkspaceUiService ui) => _ui = ui;

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
            Owner = _ui.Owner,
        };
        return dlg.ShowDialog() == true ? dlg.Options : null;
    }

    public void SetClipboard(string text) => _ui.SetClipboardText(text);

    public void ShowInformation(string message) => _ui.ShowInformation(message);

    public void ShowError(string message) => _ui.ShowError(message);
}
