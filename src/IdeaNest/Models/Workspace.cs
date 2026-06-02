using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IdeaNest.Models;

public class Workspace
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1.0";

    [JsonPropertyName("workspaceName")]
    public string WorkspaceName { get; set; } = "IdeaNest";

    [JsonPropertyName("ideas")]
    public List<Idea> Ideas { get; set; } = new();

    [JsonPropertyName("settings")]
    public WorkspaceSettings Settings { get; set; } = new();
}
