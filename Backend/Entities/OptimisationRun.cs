namespace AquaBlend.Entities;

public class OptimisationRun
{
    public int Id { get; set; }

    public int ScenarioId { get; set; }
    public Scenario Scenario { get; set; } = null!;

    // Runs start at queued; see RunStatusService for the vocabulary.
    public string WorkflowStatus { get; set; } = "queued";

    public string? SolverStatus { get; set; }

    // Populated when WorkflowStatus is "failed".
    // Describes why the workflow failed.
    public string? FailureReason { get; set; }

    // Identifies which actor declared the failure: "ai_team" or "backend",
    // the lowercase form of the RunStatusActor allowed to fail a run
    // (a client never can). See docs/database.md.
    // Currently inert: nothing writes FailureReason or FailureSource yet.
    public string? FailureSource { get; set; }

    public string ScenarioSnapshotJson { get; set; } = "{}";

    public OptimisationResult? Result { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}