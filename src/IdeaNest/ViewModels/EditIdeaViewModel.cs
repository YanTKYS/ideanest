using System.Collections.Generic;
using IdeaNest.Models;
using IdeaNest.Services;

namespace IdeaNest.ViewModels;

public class EditIdeaViewModel : ViewModelBase
{
    public Idea Original { get; }

    private string _title;
    private string _body;
    private string _tagsText;
    private string _color;
    private bool _isPinned;
    private bool _isArchived;

    public EditIdeaViewModel(Idea idea)
    {
        Original = idea;
        _title = idea.Title;
        _body = idea.Body;
        _tagsText = string.Join(", ", idea.Tags);
        _color = idea.Color;
        _isPinned = idea.IsPinned;
        _isArchived = idea.IsArchived;
    }

    public string Title { get => _title; set => SetField(ref _title, value); }
    public string Body { get => _body; set => SetField(ref _body, value); }
    public string TagsText { get => _tagsText; set => SetField(ref _tagsText, value); }
    public string Color { get => _color; set => SetField(ref _color, value); }
    public bool IsPinned { get => _isPinned; set => SetField(ref _isPinned, value); }
    public bool IsArchived { get => _isArchived; set => SetField(ref _isArchived, value); }

    public List<string> AvailableColors { get; } = new()
    {
        "yellow", "pink", "blue", "green", "purple", "orange", "gray", "white",
    };

    public void ApplyTo(Idea idea)
    {
        idea.Title = (Title ?? string.Empty).Trim();
        idea.Body  = Body ?? string.Empty;
        idea.Tags  = WorkspaceService.NormalizeTags(
            (TagsText ?? string.Empty).Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries));
        idea.Color = string.IsNullOrWhiteSpace(Color) ? "yellow" : Color;
        idea.IsPinned = IsPinned;
        idea.IsArchived = IsArchived;
    }
}
