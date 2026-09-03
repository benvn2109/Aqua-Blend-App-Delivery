namespace AquaBlend.DTOs.OptimisationResults;

public class CreateOptimisationResultDto
{
    public string ScenarioExternalId  { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SolvedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? ContractVersion { get; set; }
    public string ResultJson { get; set; } = string.Empty;
    public decimal? TotalCost { get; set; }
    public string? Currency { get; set; }
}