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
    public async Task GetChanges_ReturnsChangedWaterSourceScenarioAndOptimisationResult()
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

        var optimisationResult = new OptimisationResult
        {
            ScenarioId = scenario.Id,
            Scenario = scenario,
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
        Assert.Single(response.OptimisationResults);

        Assert.Equal(
            "Test Water Source",
            response.WaterSources[0].Name);

        Assert.Equal(
            "Test Scenario",
            response.Scenarios[0].Name);

        Assert.Equal(
            "OPTIMAL",
            response.OptimisationResults[0].Status);
    }

    [Fact]
    public async Task GetChanges_NoChanges_ReturnsEmptyCollections()
    {
        var options = new DbContextOptionsBuilder<AquaBlendDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AquaBlendDbContext(options);

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
        Assert.Empty(response.OptimisationResults);
    }
}