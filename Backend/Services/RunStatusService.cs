namespace AquaBlend.Services;

public enum RunStatusActor
{
    Backend,
    Client,
    AiTeam
}

public sealed class RunStatusService
{
    // The single source of the workflow vocabulary, in lifecycle order.
    // Adding a status means adding it here (plus any transitions it takes part in).
    // A run starts at queued: readiness belongs to the scenario (Scenario.IsReady
    // and re-validation at run creation), so there is no draft or ready run state.
    private static readonly IReadOnlyList<string> WorkflowStatusList =
        Array.AsReadOnly(new[]
        {
            "queued",
            "solving",
            "solved",
            "analysing",
            "completed",
            "failed"
        });

    private static readonly HashSet<string> WorkflowStatuses =
        new(WorkflowStatusList, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<
        (string From, string To),
        HashSet<RunStatusActor>> AllowedTransitions = new()
        {
            [("queued", "solving")] =
                new() { RunStatusActor.AiTeam },

            [("solving", "queued")] =
                new() { RunStatusActor.AiTeam },

            [("solving", "solved")] =
                new() { RunStatusActor.AiTeam },

            [("solved", "analysing")] =
                new() { RunStatusActor.Backend },

            [("analysing", "completed")] =
                new() { RunStatusActor.Backend },

            // AI team can report failure while it owns the work.
            // Backend can also fail these states when handling a timeout.
            [("queued", "failed")] =
                new()
                {
                    RunStatusActor.AiTeam,
                    RunStatusActor.Backend
                },

            [("solving", "failed")] =
                new()
                {
                    RunStatusActor.AiTeam,
                    RunStatusActor.Backend
                },

            // Once a result has arrived, failure ownership belongs
            // to the backend.
            [("solved", "failed")] =
                new() { RunStatusActor.Backend },

            [("analysing", "failed")] =
                new() { RunStatusActor.Backend }
        };

    public bool IsValidTransition(
        string currentStatus,
        string nextStatus,
        RunStatusActor actor)
    {
        if (string.IsNullOrWhiteSpace(currentStatus) ||
            string.IsNullOrWhiteSpace(nextStatus))
        {
            return false;
        }

        var current = currentStatus.Trim().ToLowerInvariant();
        var next = nextStatus.Trim().ToLowerInvariant();

        if (!WorkflowStatuses.Contains(current) ||
            !WorkflowStatuses.Contains(next))
        {
            return false;
        }

        // Same-state transitions are successful no-ops.
        // This allows safe retries.
        if (current == next)
        {
            return true;
        }

        // completed and failed are terminal states.
        if (current is "completed" or "failed")
        {
            return false;
        }

        return AllowedTransitions.TryGetValue(
                   (current, next),
                   out var allowedActors)
               && allowedActors.Contains(actor);
    }

    public IReadOnlyList<string> GetWorkflowStatuses()
    {
        return WorkflowStatusList;
    }
}