using System.Text.Json;

namespace AquaBlend.DTOs.OptimisationResults;

public class OptimisationResultResponseDto
{
    public int Id { get; set; }
    public int ScenarioId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SolvedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string ContractVersion { get; set; } = string.Empty;
    public JsonElement ResultJson { get; set; }
    public decimal? TotalCost { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}