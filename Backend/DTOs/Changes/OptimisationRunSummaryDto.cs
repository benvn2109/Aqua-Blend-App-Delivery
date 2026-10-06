namespace AquaBlend.DTOs.Changes;

public sealed class OptimisationRunSummaryDto
{
    public int Id { get; init; }

    public int ScenarioId { get; init; }

    public string WorkflowStatus { get; init; } = string.Empty;

    public string? SolverStatus { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}