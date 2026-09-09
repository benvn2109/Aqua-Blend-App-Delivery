namespace AquaBlend.Entities;

public class OptimisationRun
{
    public int Id { get; set; }

    public int ScenarioId { get; set; }
    public Scenario Scenario { get; set; } = null!;

    public string WorkflowStatus { get; set; } = "draft";

    public string? SolverStatus { get; set; }

    public string ScenarioSnapshotJson { get; set; } = string.Empty;

    public OptimisationResult? Result { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}