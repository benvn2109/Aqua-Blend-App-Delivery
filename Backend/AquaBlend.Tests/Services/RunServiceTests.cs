using AquaBlend.Data;
using AquaBlend.DTOs.Scenarios;
using AquaBlend.Entities;
using AquaBlend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AquaBlend.Tests.Services;

public sealed class RunServiceTests : IDisposable
{
    private readonly AquaBlendDbContext _context;
    private readonly RunService _runService;

    public RunServiceTests()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AquaBlendDbContext(options);
        _runService = new RunService(_context, new ScenarioValidationService(_context));
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ValidScenario_CreatesQueuedRunWithSnapshot()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context);
        var config = TestNetwork.Config(["S1"], ["P1"], ["Z1"], qualityProfile.Id);
        var scenarioId = await TestNetwork.AddScenarioAsync(_context, config);

        var run = await _runService.CreateAsync(scenarioId);

        Assert.NotNull(run);
        Assert.Equal("queued", run!.WorkflowStatus);
        Assert.Null(run.SolverStatus);

        var stored = await _context.OptimisationRuns.AsNoTracking().SingleAsync(r => r.Id == run.Id);
        Assert.Equal(config, stored.ScenarioSnapshotJson);
    }

    [Fact]
    public async Task CreateAsync_UnknownScenario_ReturnsNull()
    {
        Assert.Null(await _runService.CreateAsync(9999));
    }

    [Fact]
    public async Task CreateAsync_InvalidScenario_ThrowsScenarioNotReadyAndCreatesNoRun()
    {
        var scenarioId = await TestNetwork.AddScenarioAsync(_context, "{}");

        await Assert.ThrowsAsync<ScenarioNotReadyException>(
            () => _runService.CreateAsync(scenarioId));

        Assert.False(await _context.OptimisationRuns.AnyAsync());
    }

    [Fact]
    public async Task CreateAsync_ReferenceDataChangedAfterValidation_RejectsDespiteStoredIsReady()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context, plantCapacity: 100m, zoneDemand: 50m);
        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1"], ["Z1"], qualityProfile.Id));

        var validation = await new ScenarioValidationService(_context).ValidateAsync(scenarioId);
        Assert.True(validation.IsReady);

        // Scenario config untouched; only reference data changes underneath it.
        var plant = await _context.Plants.SingleAsync(p => p.ExternalId == "P1");
        plant.MaximumProcessingCapacityMlPerDay = 40m;
        await _context.SaveChangesAsync();

        var storedBefore = await _context.Scenarios.AsNoTracking().SingleAsync(s => s.Id == scenarioId);
        Assert.True(storedBefore.IsReady); // the stale cached verdict

        await Assert.ThrowsAsync<ScenarioNotReadyException>(
            () => _runService.CreateAsync(scenarioId));

        Assert.False(await _context.OptimisationRuns.AnyAsync());

        // Re-validation also refreshes the cached flag for scenario listings.
        var storedAfter = await _context.Scenarios.AsNoTracking().SingleAsync(s => s.Id == scenarioId);
        Assert.False(storedAfter.IsReady);
    }

    [Fact]
    public async Task UpdateAsync_ChangedNetworkConfig_ClearsIsReady()
    {
        var scenario = await AddScenarioAsync(isReady: true, networkConfigJson: """{"sources":[]}""");

        await new ScenarioService(_context).UpdateAsync(scenario.Id, new UpdateScenarioDto
        {
            Name = scenario.Name,
            Description = scenario.Description,
            NetworkConfig = JsonDocument.Parse("""{"sources":[{"source_id":"S2"}]}""").RootElement
        });

        var stored = await _context.Scenarios.AsNoTracking().SingleAsync(s => s.Id == scenario.Id);
        Assert.False(stored.IsReady);
    }

    [Fact]
    public async Task UpdateAsync_UnchangedOrOmittedNetworkConfig_KeepsIsReady()
    {
        var scenario = await AddScenarioAsync(isReady: true, networkConfigJson: """{"sources":[]}""");
        var service = new ScenarioService(_context);

        // Renaming only, with the same config resent - as a form save would do.
        await service.UpdateAsync(scenario.Id, new UpdateScenarioDto
        {
            Name = "Renamed",
            Description = scenario.Description,
            NetworkConfig = JsonDocument.Parse("""{"sources":[]}""").RootElement
        });

        // Renaming only, with no config in the body.
        await service.UpdateAsync(scenario.Id, new UpdateScenarioDto
        {
            Name = "Renamed again",
            Description = scenario.Description
        });

        var stored = await _context.Scenarios.AsNoTracking().SingleAsync(s => s.Id == scenario.Id);
        Assert.True(stored.IsReady);
    }

    private async Task<Scenario> AddScenarioAsync(bool isReady, string networkConfigJson)
    {
        var scenario = new Scenario
        {
            Name = "Run Test Scenario",
            Description = "Scenario for RunService tests",
            NetworkConfigJson = networkConfigJson,
            IsReady = isReady
        };

        _context.Scenarios.Add(scenario);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return scenario;
    }
}
