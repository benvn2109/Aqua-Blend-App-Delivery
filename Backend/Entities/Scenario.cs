namespace AquaBlend.Entities;

public class Scenario
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExternalId { get; set; }

    public string NetworkConfigJson { get; set; } = "{}";

    public bool IsReady { get; set; }
    public string ValidationIssuesJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OptimisationResult> OptimisationResults { get; set; } = new List<OptimisationResult>();
    public ICollection<OptimisationRun> OptimisationRuns { get; set; } = new List<OptimisationRun>();
}