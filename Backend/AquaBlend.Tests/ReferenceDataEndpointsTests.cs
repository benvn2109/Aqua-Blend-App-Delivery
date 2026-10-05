using System.Net;
using System.Net.Http.Json;
using AquaBlend.Data;
using AquaBlend.DTOs.ReferenceData;
using AquaBlend.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AquaBlend.Tests;

public class ReferenceDataEndpointsTests : IDisposable
{
    private readonly AquaBlendApiFactory _factory;
    private readonly HttpClient _client;

    public ReferenceDataEndpointsTests()
    {
        _factory = new AquaBlendApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private AquaBlendDbContext CreateDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AquaBlendDbContext>();
    }

    [Fact]
    public async Task GetSources_ReturnsFullContractShape()
    {
        // Seeded by SeedData: "Reservoir A" / "Bore Well 1", both with default
        // reference-data fields rather than just Name/Type.
        var response = await _client.GetAsync("/api/sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sources = await response.Content.ReadFromJsonAsync<List<SourceResponseDto>>();

        Assert.NotNull(sources);
        Assert.NotEmpty(sources!);

        var reservoirA = sources!.Single(s => s.Name == "Reservoir A");
        Assert.Equal("reservoir", reservoirA.Type);
        Assert.False(string.IsNullOrEmpty(reservoirA.AvailabilityStatus));
        Assert.False(string.IsNullOrEmpty(reservoirA.AvailabilityOrigin));
    }

    [Fact]
    public async Task GetPlants_ReturnsInsertedPlant()
    {
        await using (var context = CreateDbContext())
        {
            context.Plants.Add(new Plant
            {
                ExternalId = "plant_1",
                Name = "Treatment Facility 1",
                MaximumProcessingCapacityMlPerDay = 500,
                TreatmentCostPerMl = 64,
                IsModelReady = true
            });

            await context.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/plants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plants = await response.Content.ReadFromJsonAsync<List<PlantResponseDto>>();

        Assert.NotNull(plants);
        var plant = Assert.Single(plants!);
        Assert.Equal("Treatment Facility 1", plant.Name);
        Assert.Equal("plant_1", plant.ExternalId);
        Assert.True(plant.IsModelReady);
    }

    [Fact]
    public async Task GetDemandZones_ReturnsInsertedDemandZone()
    {
        await using (var context = CreateDbContext())
        {
            context.DemandZones.Add(new DemandZone
            {
                ExternalId = "zone_1",
                Name = "Zone 1",
                DemandMlPerDay = 500
            });

            await context.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/demand-zones");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var zones = await response.Content.ReadFromJsonAsync<List<DemandZoneResponseDto>>();

        Assert.NotNull(zones);
        var zone = Assert.Single(zones!);
        Assert.Equal("Zone 1", zone.Name);
        Assert.Equal(500, zone.DemandMlPerDay);
    }

    [Fact]
    public async Task GetQualityProfiles_ReturnsInsertedProfile()
    {
        await using (var context = CreateDbContext())
        {
            context.QualityProfiles.Add(new QualityProfile
            {
                Name = "Standard Drinking Water",
                Parameter = "pH",
                Unit = string.Empty,
                ConstraintMin = 6.5m,
                ConstraintMax = 8.5m
            });

            await context.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/quality-profiles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profiles = await response.Content.ReadFromJsonAsync<List<QualityProfileResponseDto>>();

        Assert.NotNull(profiles);
        var profile = Assert.Single(profiles!);
        Assert.Equal("pH", profile.Parameter);
        Assert.Equal(6.5m, profile.ConstraintMin);
        Assert.Equal(8.5m, profile.ConstraintMax);
    }

    [Fact]
    public async Task GetNetworkLinks_ReturnsBothSourceToPlantAndPlantToZone()
    {
        int waterSourceId;
        int plantId;
        int demandZoneId;

        await using (var context = CreateDbContext())
        {
            var waterSource = new WaterSource { Name = "Link Test Source", Type = "Reservoir" };
            var plant = new Plant { Name = "Link Test Plant", MaximumProcessingCapacityMlPerDay = 100, TreatmentCostPerMl = 10 };
            var zone = new DemandZone { Name = "Link Test Zone", DemandMlPerDay = 50 };

            context.WaterSources.Add(waterSource);
            context.Plants.Add(plant);
            context.DemandZones.Add(zone);
            await context.SaveChangesAsync();

            waterSourceId = waterSource.Id;
            plantId = plant.Id;
            demandZoneId = zone.Id;

            context.SourcePlantLinks.Add(new SourcePlantLink
            {
                WaterSourceId = waterSourceId,
                PlantId = plantId,
                IsActive = true,
                MaximumCapacityMlPerDay = 200
            });

            context.PlantZoneLinks.Add(new PlantZoneLink
            {
                PlantId = plantId,
                DemandZoneId = demandZoneId,
                IsActive = true,
                MaximumCapacityMlPerDay = 150
            });

            await context.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/network-links");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var links = await response.Content.ReadFromJsonAsync<NetworkLinksResponseDto>();

        Assert.NotNull(links);

        var sourceToPlant = Assert.Single(links!.SourceToPlant);
        Assert.Equal(waterSourceId, sourceToPlant.WaterSourceId);
        Assert.Equal(plantId, sourceToPlant.PlantId);

        var plantToZone = Assert.Single(links.PlantToZone);
        Assert.Equal(plantId, plantToZone.PlantId);
        Assert.Equal(demandZoneId, plantToZone.DemandZoneId);
    }
}
