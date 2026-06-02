using System.Text.Json.Serialization;

namespace IdeaNest.Models;

public class WorkspaceSettings
{
    [JsonPropertyName("searchText")]
    public string SearchText { get; set; } = string.Empty;

    [JsonPropertyName("selectedTag")]
    public string SelectedTag { get; set; } = string.Empty;

    [JsonPropertyName("showArchived")]
    public bool ShowArchived { get; set; }

    [JsonPropertyName("windowWidth")]
    public double WindowWidth { get; set; } = 1100;

    [JsonPropertyName("windowHeight")]
    public double WindowHeight { get; set; } = 700;
}
