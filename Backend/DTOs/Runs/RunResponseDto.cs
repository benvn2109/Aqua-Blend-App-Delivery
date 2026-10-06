namespace AquaBlend.DTOs.Runs;

public class RunResponseDto
{
    public int Id { get; set; }

    public int ScenarioId { get; set; }

    public string WorkflowStatus { get; set; } = string.Empty;

    public string? SolverStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}