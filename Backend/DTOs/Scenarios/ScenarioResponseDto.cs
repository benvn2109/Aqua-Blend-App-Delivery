using System.Text.Json;

namespace AquaBlend.DTOs.Scenarios;

public class ScenarioResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public JsonElement NetworkConfig { get; set; }
    public bool IsReady { get; set; }
    public JsonElement ValidationIssues { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}