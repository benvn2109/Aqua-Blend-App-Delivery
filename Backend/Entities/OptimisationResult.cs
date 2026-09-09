namespace AquaBlend.Entities;

public class OptimisationResult
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public OptimisationRun Run { get; set; } = null!;

    public int? ScenarioId { get; set; }
    public Scenario? Scenario { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime SolvedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string ContractVersion { get; set; } = string.Empty;

    public string ResultJson { get; set; } = string.Empty;

    public decimal? TotalCost { get; set; }
    public string? Currency { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}