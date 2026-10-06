using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AquaBlend.DTOs.Scenarios;

public class UpdateScenarioDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public JsonElement? NetworkConfig { get; set; }
}