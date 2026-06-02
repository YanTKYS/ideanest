namespace IdeaNest.Models;

public class NoteNestExportOptions
{
    public bool IncludeNoteMarker { get; set; } = true;
    public bool IncludeTodoMarker { get; set; } = false;
    public bool IncludeMeta      { get; set; } = true;
}
