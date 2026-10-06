using System.Text.Json;
using AquaBlend.Data;
using AquaBlend.Entities;

namespace AquaBlend.Tests.Services;

// Shared fixture for tests that need a scenario network which passes validation.
internal static class TestNetwork
{
    // Seeds S1 -> P1 -> Z1 with active links and a valid quality profile.
    public static async Task<QualityProfile> SeedValidAsync(
        AquaBlendDbContext context,
        decimal plantCapacity = 100m,
        decimal zoneDemand = 50m)
    {
        var source = new WaterSource { ExternalId = "S1", Name = "Source 1", Type = "reservoir", IsModelReady = true };
        var plant = new Plant { ExternalId = "P1", Name = "Plant 1", MaximumProcessingCapacityMlPerDay = plantCapacity, IsModelReady = true };
        var zone = new DemandZone { ExternalId = "Z1", Name = "Zone 1", DemandMlPerDay = zoneDemand };
        var qualityProfile = ValidQualityProfile();

        context.AddRange(source, plant, zone, qualityProfile);
        await context.SaveChangesAsync();

        context.AddRange(
            new SourcePlantLink { WaterSourceId = source.Id, PlantId = plant.Id, IsActive = true },
            new PlantZoneLink { PlantId = plant.Id, DemandZoneId = zone.Id, IsActive = true });
        await context.SaveChangesAsync();

        return qualityProfile;
    }

    public static QualityProfile ValidQualityProfile() => new()
    {
        Name = "Drinking water pH",
        Parameter = "pH",
        Unit = "pH",
        ConstraintMin = 6.5m,
        ConstraintMax = 8.5m
    };

    public static async Task<int> AddScenarioAsync(
        AquaBlendDbContext context,
        string networkConfigJson)
    {
        var scenario = new Scenario
        {
            Name = "Validation Test Scenario",
            Description = "Scenario for validation tests",
            NetworkConfigJson = networkConfigJson
        };

        context.Scenarios.Add(scenario);
        await context.SaveChangesAsync();

        return scenario.Id;
    }

    public static string Config(
        string[] sourceIds,
        string[] plantIds,
        string[] zoneIds,
        int qualityProfileId)
    {
        return JsonSerializer.Serialize(new
        {
            sources = sourceIds.Select(id => new { source_id = id, enabled = true }),
            plants = plantIds.Select(id => new { plant_id = id, enabled = true }),
            network = new
            {
                demand_zones = zoneIds.Select(id => new { zone_id = id, enabled = true })
            },
            quality_profile_id = qualityProfileId
        });
    }
}
