using AquaBlend.Data;
using AquaBlend.Entities;
using AquaBlend.Services;
using Microsoft.EntityFrameworkCore;

namespace AquaBlend.Tests.Services;

public sealed class ScenarioValidationServiceTests : IDisposable
{
    private readonly AquaBlendDbContext _context;
    private readonly ScenarioValidationService _service;

    public ScenarioValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AquaBlendDbContext(options);
        _service = new ScenarioValidationService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task ValidateAsync_AllRulesSatisfied_IsReadyWithNoIssues()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context);

        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1"], ["Z1"], qualityProfile.Id));

        var result = await _service.ValidateAsync(scenarioId);

        Assert.True(result.Found);
        Assert.True(result.IsReady);
        Assert.Empty(result.ValidationIssues);

        // IsReady is persisted, not just returned.
        var stored = await _context.Scenarios.AsNoTracking().SingleAsync(s => s.Id == scenarioId);
        Assert.True(stored.IsReady);
        Assert.Equal("[]", stored.ValidationIssuesJson);
    }

    [Fact]
    public async Task ValidateAsync_NoSourcesSelected_ReturnsSourcesRequired()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context);

        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config([], ["P1"], ["Z1"], qualityProfile.Id));

        var result = await _service.ValidateAsync(scenarioId);

        Assert.False(result.IsReady);
        var issue = Assert.Single(result.ValidationIssues);
        Assert.Equal("sources_required", issue.Code);
        Assert.Equal("$.sources", issue.Path);
    }

    [Fact]
    public async Task ValidateAsync_SelectedPlantNotInDatabase_ReturnsPlantMissingFromDatabase()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context);

        // P1 exists and carries the whole network; P404 is selected but was never seeded.
        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1", "P404"], ["Z1"], qualityProfile.Id));

        var result = await _service.ValidateAsync(scenarioId);

        Assert.False(result.IsReady);
        var issue = Assert.Single(result.ValidationIssues);
        Assert.Equal("plant_missing_from_database", issue.Code);
        Assert.Equal("$.plants", issue.Path);
        Assert.Contains("P404", issue.Message);
    }

    [Fact]
    public async Task ValidateAsync_DemandZoneWithZeroDemand_ReturnsDemandMissing()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context, zoneDemand: 0m);

        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1"], ["Z1"], qualityProfile.Id));

        var result = await _service.ValidateAsync(scenarioId);

        Assert.False(result.IsReady);
        var issue = Assert.Single(result.ValidationIssues);
        Assert.Equal("demand_missing", issue.Code);
        Assert.Equal("$.network.demand_zones", issue.Path);
    }

    [Fact]
    public async Task ValidateAsync_PlantCapacityBelowTotalDemand_ReturnsPlantCapacityInsufficient()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context, plantCapacity: 40m, zoneDemand: 50m);

        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1"], ["Z1"], qualityProfile.Id));

        var result = await _service.ValidateAsync(scenarioId);

        Assert.False(result.IsReady);
        var issue = Assert.Single(result.ValidationIssues);
        Assert.Equal("plant_capacity_insufficient", issue.Code);
        Assert.Equal("$.plants", issue.Path);
    }

    [Fact]
    public async Task ValidateAsync_ZoneOnlyServedByPlantWithNoSelectedSource_ReturnsDemandZoneUnreachable()
    {
        // S1 feeds P1, but Z1 is only linked to P2, and nothing feeds P2.
        // Z1 does have a plant link, so this checks the full source -> plant -> zone
        // path rather than just "the zone has some plant".
        var source = new WaterSource { ExternalId = "S1", Name = "Source 1", Type = "reservoir", IsModelReady = true };
        var plant1 = new Plant { ExternalId = "P1", Name = "Plant 1", MaximumProcessingCapacityMlPerDay = 100m, IsModelReady = true };
        var plant2 = new Plant { ExternalId = "P2", Name = "Plant 2", MaximumProcessingCapacityMlPerDay = 100m, IsModelReady = true };
        var zone = new DemandZone { ExternalId = "Z1", Name = "Zone 1", DemandMlPerDay = 50m };
        var qualityProfile = TestNetwork.ValidQualityProfile();

        _context.AddRange(source, plant1, plant2, zone, qualityProfile);
        await _context.SaveChangesAsync();

        _context.AddRange(
            new SourcePlantLink { WaterSourceId = source.Id, PlantId = plant1.Id, IsActive = true },
            new PlantZoneLink { PlantId = plant2.Id, DemandZoneId = zone.Id, IsActive = true });
        await _context.SaveChangesAsync();

        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1", "P2"], ["Z1"], qualityProfile.Id));

        var result = await _service.ValidateAsync(scenarioId);

        Assert.False(result.IsReady);
        var issue = Assert.Single(result.ValidationIssues);
        Assert.Equal("demand_zone_unreachable", issue.Code);
        Assert.Equal("$.network.demand_zones", issue.Path);
        Assert.Contains("Z1", issue.Message);
    }

    [Fact]
    public async Task ValidateAsync_QualityProfileMinAboveMax_ReturnsQualityProfileInvalidRange()
    {
        var qualityProfile = await TestNetwork.SeedValidAsync(_context);
        qualityProfile.ConstraintMin = 9m;
        qualityProfile.ConstraintMax = 6m;
        await _context.SaveChangesAsync();

        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1"], ["Z1"], qualityProfile.Id));

        var result = await _service.ValidateAsync(scenarioId);

        Assert.False(result.IsReady);
        var issue = Assert.Single(result.ValidationIssues);
        Assert.Equal("quality_profile_invalid_range", issue.Code);
        Assert.Equal("$.quality_profile_id", issue.Path);
    }

    [Fact]
    public async Task ValidateAsync_StoredIssuesInJsonbFormat_TreatedAsUnchanged()
    {
        // Postgres jsonb hands back reordered keys and extra whitespace, so the
        // stored text never matches JsonSerializer output. An identical verdict
        // must still be recognised as identical, or every call would save.
        var qualityProfile = await TestNetwork.SeedValidAsync(_context, plantCapacity: 40m, zoneDemand: 50m);

        var scenarioId = await TestNetwork.AddScenarioAsync(_context,
            TestNetwork.Config(["S1"], ["P1"], ["Z1"], qualityProfile.Id));

        var first = await _service.ValidateAsync(scenarioId);
        var issue = Assert.Single(first.ValidationIssues);

        var scenario = await _context.Scenarios.SingleAsync(s => s.Id == scenarioId);
        scenario.ValidationIssuesJson =
            $"[{{\"Code\": \"{issue.Code}\", \"Path\": \"{issue.Path}\", \"Message\": \"{issue.Message}\"}}]";
        await _context.SaveChangesAsync();

        var afterJsonbRewrite = await StoredUpdatedAtAsync(scenarioId);
        WaitForClockToPass(afterJsonbRewrite);

        var second = await _service.ValidateAsync(scenarioId);

        Assert.False(second.IsReady);
        Assert.Equal(afterJsonbRewrite, await StoredUpdatedAtAsync(scenarioId));
    }

    private async Task<DateTime?> StoredUpdatedAtAsync(int scenarioId)
    {
        return (await _context.Scenarios.AsNoTracking().SingleAsync(s => s.Id == scenarioId)).UpdatedAt;
    }

    // Guarantees a save in the next call would produce a different UpdatedAt,
    // so "unchanged" can't pass just because both calls landed in the same tick.
    private static void WaitForClockToPass(DateTime? timestamp)
    {
        Assert.NotNull(timestamp);
        Assert.True(SpinWait.SpinUntil(() => DateTime.UtcNow > timestamp!.Value, TimeSpan.FromSeconds(1)));
    }
}
