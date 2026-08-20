namespace AquaBlend.Entities;

public class OptimisationResult
{
    public int Id { get; set; }

    public int ScenarioId { get; set; }
    public Scenario Scenario { get; set; } = null!;

    public string Status { get; set; } = string.Empty;
    public DateTime SolvedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string ContractVersion { get; set; } = string.Empty;

    // Full model output contract, stored as PostgreSQL jsonb
    public string ResultJson { get; set; } = string.Empty;

    // Extracted for searchability — nullable because non-OPTIMAL
    // results omit the objective block entirely (no total_cost to extract)
    public decimal? TotalCost { get; set; }
    public string? Currency { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
