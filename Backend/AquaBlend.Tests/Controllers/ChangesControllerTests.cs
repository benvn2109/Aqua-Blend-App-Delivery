using AquaBlend.Controllers;
using AquaBlend.Data;
using AquaBlend.DTOs;
using AquaBlend.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AquaBlend.Tests.Controllers;

public sealed class ChangesControllerTests
{
    [Fact]
    public async Task GetChanges_InvalidTimestamp_ReturnsBadRequest()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AquaBlendDbContext(options);

        var controller = new ChangesController(context);

        var result = await controller.GetChanges(
            "invalid-timestamp",
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetChanges_ReturnsChangedWaterSourceScenarioRunAndOptimisationResult()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AquaBlendDbContext(options);

        // Timestamp before the new records are created.
        var since = DateTime.UtcNow.AddMinutes(-1);

        var waterSource = new WaterSource
        {
            Name = "Test Water Source",
            Type = "Reservoir"
        };

        var scenario = new Scenario
        {
            Name = "Test Scenario",
            Description = "Scenario used for automatic updates testing"
        };

        context.WaterSources.Add(waterSource);
        context.Scenarios.Add(scenario);

        await context.SaveChangesAsync();

        var optimisationRun = new OptimisationRun
        {
            ScenarioId = scenario.Id,
            Scenario = scenario,
            WorkflowStatus = "solved",
            SolverStatus = "OPTIMAL",
            ScenarioSnapshotJson = "{}"
        };

        context.OptimisationRuns.Add(optimisationRun);

        await context.SaveChangesAsync();

        var optimisationResult = new OptimisationResult
        {
            RunId = optimisationRun.Id,
            Run = optimisationRun,

            // Legacy column left null, so the ScenarioId assertion below can
            // only pass if the response reads it through the run.
            ScenarioId = null,
            Status = "OPTIMAL",
            SolvedAt = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            ContractVersion = "1.0",
            ResultJson = "{}",
            TotalCost = 1000.00m,
            Currency = "AUD"
        };

        context.OptimisationResults.Add(optimisationResult);

        await context.SaveChangesAsync();

        var controller = new ChangesController(context);

        var result = await controller.GetChanges(
            since.ToString("O"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<ChangesResponseDto>(okResult.Value);

        Assert.Single(response.WaterSources);
        Assert.Single(response.Scenarios);
        Assert.Single(response.OptimisationRuns);
        Assert.Single(response.OptimisationResults);

        Assert.Equal(
            "Test Water Source",
            response.WaterSources[0].Name);

        Assert.Equal(
            "Test Scenario",
            response.Scenarios[0].Name);

        Assert.Equal(
            "solved",
            response.OptimisationRuns[0].WorkflowStatus);

        Assert.Equal(
            "OPTIMAL",
            response.OptimisationRuns[0].SolverStatus);

        Assert.Equal(
            scenario.Id,
            response.OptimisationRuns[0].ScenarioId);

        Assert.Equal(
            "OPTIMAL",
            response.OptimisationResults[0].Status);

        Assert.Equal(
            scenario.Id,
            response.OptimisationResults[0].ScenarioId);

        Assert.Equal(
            optimisationRun.Id,
            response.OptimisationResults[0].RunId);
    }

    [Fact]
    public async Task GetChanges_ScenarioUpdatedAfterSince_IsReturnedViaUpdatedAtClause()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AquaBlendDbContext(options);

        var scenario = new Scenario
        {
            Name = "Pre-threshold Scenario",
            Description = "Created before the since threshold"
        };

        context.Scenarios.Add(scenario);

        // ApplyTimestamps sets CreatedAt and UpdatedAt to the same value on
        // insert, so a real delay is needed before capturing `since` or the
        // insert's own timestamp could land on or after it.
        await context.SaveChangesAsync();
        await Task.Delay(50);

        var since = DateTime.UtcNow;

        // Another real delay so the update's UpdatedAt lands strictly after
        // `since` rather than possibly matching it.
        await Task.Delay(50);

        scenario.Description = "Updated after the since threshold";
        await context.SaveChangesAsync();

        var controller = new ChangesController(context);

        var result = await controller.GetChanges(
            since.ToString("O"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<ChangesResponseDto>(okResult.Value);

        var returned = Assert.Single(response.Scenarios);

        // This is the crux of the test: the record must have been created
        // before `since`, so it can only appear in the response via the
        // UpdatedAt clause. Without this assertion, a timing slip could put
        // CreatedAt after `since`, letting the CreatedAt clause alone
        // satisfy the test while a missing/broken UpdatedAt clause goes
        // undetected.
        Assert.True(returned.CreatedAt <= since);

        Assert.Equal(
            "Updated after the since threshold",
            returned.Description);
    }

    [Fact]
    public async Task GetChanges_RunUpdatedAfterSince_IsReturnedViaUpdatedAtClause()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AquaBlendDbContext(options);

        var scenario = new Scenario
        {
            Name = "Run Status Scenario",
            Description = "Scenario used for run polling test"
        };

        context.Scenarios.Add(scenario);
        await context.SaveChangesAsync();

        var optimisationRun = new OptimisationRun
        {
            ScenarioId = scenario.Id,
            Scenario = scenario,
            WorkflowStatus = "queued",
            ScenarioSnapshotJson = "{}"
        };

        context.OptimisationRuns.Add(optimisationRun);

        await context.SaveChangesAsync();
        await Task.Delay(50);

        var since = DateTime.UtcNow;

        await Task.Delay(50);

        optimisationRun.WorkflowStatus = "solving";

        await context.SaveChangesAsync();

        var controller = new ChangesController(context);

        var result = await controller.GetChanges(
            since.ToString("O"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<ChangesResponseDto>(okResult.Value);

        var returned = Assert.Single(response.OptimisationRuns);

        // The run existed before the threshold, so it should only be
        // returned because UpdatedAt changed after `since`.
        Assert.True(returned.CreatedAt <= since);

        Assert.Equal(
            "solving",
            returned.WorkflowStatus);

        Assert.Equal(
            scenario.Id,
            returned.ScenarioId);
    }

    [Fact]
    public async Task GetChanges_NoChanges_ReturnsEmptyCollections()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AquaBlendDbContext(options);

        // One of each entity, all created before `since`. Without existing
        // rows the Assert.Empty checks below would pass even if the filters
        // were deleted, because there would be nothing to filter out.
        var scenario = new Scenario
        {
            Name = "Unchanged Scenario",
            Description = "Created before the since threshold"
        };
        context.WaterSources.Add(new WaterSource { Name = "Unchanged Source", Type = "reservoir" });
        context.Scenarios.Add(scenario);
        await context.SaveChangesAsync();

        var run = new OptimisationRun { ScenarioId = scenario.Id, WorkflowStatus = "queued" };
        context.OptimisationRuns.Add(run);
        await context.SaveChangesAsync();

        context.OptimisationResults.Add(new OptimisationResult
        {
            RunId = run.Id,
            Status = "OPTIMAL",
            SolvedAt = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            ContractVersion = "1.0",
            ResultJson = "{}"
        });
        await context.SaveChangesAsync();

        var controller = new ChangesController(context);

        // Future timestamp ensures there are no matching changes.
        var since = DateTime.UtcNow.AddMinutes(1);

        var result = await controller.GetChanges(
            since.ToString("O"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<ChangesResponseDto>(okResult.Value);

        Assert.Empty(response.WaterSources);
        Assert.Empty(response.Scenarios);
        Assert.Empty(response.OptimisationRuns);
        Assert.Empty(response.OptimisationResults);
    }
}