using AquaBlend.Services;

namespace AquaBlend.Tests.Services;

public sealed class RunStatusServiceTests
{
    private readonly RunStatusService _service = new();

    [Theory]
    [InlineData("queued", "solving", RunStatusActor.AiTeam)]
    [InlineData("solving", "queued", RunStatusActor.AiTeam)]
    [InlineData("solving", "solved", RunStatusActor.AiTeam)]
    [InlineData("solved", "analysing", RunStatusActor.Backend)]
    [InlineData("analysing", "completed", RunStatusActor.Backend)]
    public void IsValidTransition_AllowedActorAndTransition_ReturnsTrue(
        string currentStatus,
        string nextStatus,
        RunStatusActor actor)
    {
        Assert.True(
            _service.IsValidTransition(currentStatus, nextStatus, actor));
    }

    [Theory]
    [InlineData("queued", "analysing", RunStatusActor.Backend)]
    [InlineData("solved", "completed", RunStatusActor.Backend)]
    [InlineData("solving", "completed", RunStatusActor.Backend)]
    [InlineData("queued", "solved", RunStatusActor.AiTeam)]
    public void IsValidTransition_SkippedState_ReturnsFalse(
        string currentStatus,
        string nextStatus,
        RunStatusActor actor)
    {
        Assert.False(
            _service.IsValidTransition(currentStatus, nextStatus, actor));
    }

    [Theory]
    [InlineData("queued", "solving", RunStatusActor.Client)]
    [InlineData("solving", "solved", RunStatusActor.Client)]
    [InlineData("analysing", "completed", RunStatusActor.Client)]
    [InlineData("solved", "analysing", RunStatusActor.AiTeam)]
    public void IsValidTransition_WrongActor_ReturnsFalse(
        string currentStatus,
        string nextStatus,
        RunStatusActor actor)
    {
        Assert.False(
            _service.IsValidTransition(currentStatus, nextStatus, actor));
    }

    [Theory]
    [InlineData("queued", RunStatusActor.AiTeam)]
    [InlineData("queued", RunStatusActor.Backend)]
    [InlineData("solving", RunStatusActor.AiTeam)]
    [InlineData("solving", RunStatusActor.Backend)]
    [InlineData("solved", RunStatusActor.Backend)]
    [InlineData("analysing", RunStatusActor.Backend)]
    public void IsValidTransition_AllowedFailureActor_ReturnsTrue(
        string currentStatus,
        RunStatusActor actor)
    {
        Assert.True(
            _service.IsValidTransition(
                currentStatus,
                "failed",
                actor));
    }

    [Theory]
    [InlineData("queued")]
    [InlineData("solving")]
    [InlineData("solved")]
    [InlineData("analysing")]
    public void IsValidTransition_ClientCannotDeclareFailure_ReturnsFalse(
        string currentStatus)
    {
        Assert.False(
            _service.IsValidTransition(
                currentStatus,
                "failed",
                RunStatusActor.Client));
    }

    [Theory]
    [InlineData("solved")]
    [InlineData("analysing")]
    public void IsValidTransition_AiTeamCannotFailBackendOwnedState_ReturnsFalse(
        string currentStatus)
    {
        Assert.False(
            _service.IsValidTransition(
                currentStatus,
                "failed",
                RunStatusActor.AiTeam));
    }

    [Theory]
    [InlineData("queued", RunStatusActor.AiTeam)]
    [InlineData("solving", RunStatusActor.AiTeam)]
    [InlineData("solved", RunStatusActor.Backend)]
    [InlineData("analysing", RunStatusActor.Backend)]
    [InlineData("completed", RunStatusActor.Client)]
    [InlineData("failed", RunStatusActor.AiTeam)]
    public void IsValidTransition_SameState_ReturnsTrue(
        string status,
        RunStatusActor actor)
    {
        Assert.True(
            _service.IsValidTransition(
                status,
                status,
                actor));
    }

    [Theory]
    [InlineData("completed", "queued")]
    [InlineData("completed", "analysing")]
    [InlineData("failed", "queued")]
    [InlineData("failed", "solving")]
    public void IsValidTransition_FromTerminalState_ReturnsFalse(
        string currentStatus,
        string nextStatus)
    {
        Assert.False(
            _service.IsValidTransition(
                currentStatus,
                nextStatus,
                RunStatusActor.Backend));
    }

    [Fact]
    public void GetWorkflowStatuses_IncludesFailed()
    {
        var statuses = _service.GetWorkflowStatuses();

        Assert.Contains("failed", statuses);
        Assert.Contains("completed", statuses);
        Assert.Equal(6, statuses.Count);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("ready")]
    public void ScenarioReadinessStates_AreNotRunStatuses(string status)
    {
        // Readiness belongs to the scenario; a run starts at queued.
        Assert.DoesNotContain(status, _service.GetWorkflowStatuses());
        Assert.False(_service.IsValidTransition(status, "queued", RunStatusActor.Backend));
        Assert.False(_service.IsValidTransition("queued", status, RunStatusActor.Backend));
    }

    [Fact]
    public void EveryStatus_IsReachableFromQueued()
    {
        // queued is where RunService creates every run, so a status that
        // cannot be reached from it is dead vocabulary.
        var statuses = _service.GetWorkflowStatuses();
        var reached = new HashSet<string> { "queued" };
        var frontier = new Queue<string>(reached);

        while (frontier.Count > 0)
        {
            var from = frontier.Dequeue();

            foreach (var to in statuses)
            {
                if (reached.Contains(to))
                    continue;

                if (Enum.GetValues<RunStatusActor>().Any(actor =>
                        _service.IsValidTransition(from, to, actor)))
                {
                    reached.Add(to);
                    frontier.Enqueue(to);
                }
            }
        }

        Assert.Equal(statuses.OrderBy(s => s), reached.OrderBy(s => s));
    }
}