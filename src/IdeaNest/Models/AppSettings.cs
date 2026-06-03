using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IdeaNest.Models;

public class AppSettings
{
    [JsonPropertyName("recentFiles")]
    public List<string> RecentFiles { get; set; } = new();
}
