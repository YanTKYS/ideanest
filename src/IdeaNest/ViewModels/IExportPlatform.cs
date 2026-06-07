using IdeaNest.Models;

namespace IdeaNest.ViewModels;

/// <summary>
/// WPF-dependent UI hooks that ExportViewModel relies on (file dialog, options
/// dialog, clipboard, message boxes). Implemented by WpfExportPlatform using WPF
/// types; mocked in tests so ExportViewModel itself stays cross-platform.
/// </summary>
public interface IExportPlatform
{
    /// <summary>Show a Save-As dialog. Returns the chosen path, or null if cancelled.</summary>
    string? PromptSaveFilePath(string defaultFileName);

    /// <summary>Show the NoteNest export options dialog. Returns null if cancelled.</summary>
    NoteNestExportOptions? PromptNoteNestOptions();

    /// <summary>Copy text to the system clipboard. May throw on failure.</summary>
    void SetClipboard(string text);

    /// <summary>Show an information-level dialog (e.g. "no cards to export").</summary>
    void ShowInformation(string message);

    /// <summary>Show an error-level dialog (e.g. file-write failure).</summary>
    void ShowError(string message);
}
