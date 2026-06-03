using System.Text.Json.Serialization;

namespace IdeaNest.Models;

public class WorkspaceSettings
{
    [JsonPropertyName("searchText")]
    public string SearchText { get; set; } = string.Empty;

    [JsonPropertyName("selectedTag")]
    public string SelectedTag { get; set; } = string.Empty;

    [JsonPropertyName("selectedColor")]
    public string SelectedColor { get; set; } = string.Empty;

    [JsonPropertyName("showArchived")]
    public bool ShowArchived { get; set; }

    [JsonPropertyName("tagPanelOpen")]
    public bool TagPanelOpen { get; set; } = true;

    [JsonPropertyName("cardSize")]
    public string CardSize { get; set; } = "medium";

    [JsonPropertyName("sortMode")]
    public string SortMode { get; set; } = "UpdatedDesc";

    [JsonPropertyName("windowWidth")]
    public double WindowWidth { get; set; } = 1100;

    [JsonPropertyName("windowHeight")]
    public double WindowHeight { get; set; } = 700;
}
