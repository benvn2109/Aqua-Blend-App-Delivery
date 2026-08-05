using System.Net;
using System.Net.Http.Json;
using AquaBlend.Data;
using AquaBlend.DTOs.Scenarios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AquaBlend.Tests;

public class AquaBlendApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["InMemoryDatabaseName"] = Guid.NewGuid().ToString(),
            });
        });
    }
}

public class ScenarioEndpointsTests : IDisposable
{
    private readonly AquaBlendApiFactory _factory;
    private readonly HttpClient _client;

    public ScenarioEndpointsTests()
    {
        _factory = new AquaBlendApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetAll_ReturnsScenarios()
    {
        var response = await _client.GetAsync("/api/scenarios");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var scenarios = await response.Content.ReadFromJsonAsync<List<ScenarioResponseDto>>();

        Assert.NotNull(scenarios);
        Assert.NotEmpty(scenarios!);
        Assert.Contains(scenarios!, s => s.Name == "Drought Scenario");
    }

    [Fact]
    public async Task GetById_ReturnsScenario()
    {
        var listResponse = await _client.GetAsync("/api/scenarios");
        listResponse.EnsureSuccessStatusCode();
        var scenarios = await listResponse.Content.ReadFromJsonAsync<List<ScenarioResponseDto>>();

        Assert.NotNull(scenarios);
        var existing = scenarios!.First();

        var response = await _client.GetAsync($"/api/scenarios/{existing.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var scenario = await response.Content.ReadFromJsonAsync<ScenarioResponseDto>();
        Assert.NotNull(scenario);
        Assert.Equal(existing.Id, scenario!.Id);
        Assert.Equal(existing.Name, scenario.Name);
        Assert.Equal(existing.Description, scenario.Description);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/scenarios/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedScenario()
    {
        var dto = new CreateScenarioDto
        {
            Name = "New Scenario",
            Description = "Testing scenario creation"
        };

        var response = await _client.PostAsJsonAsync("/api/scenarios", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ScenarioResponseDto>();

        Assert.NotNull(created);
        Assert.Equal(dto.Name, created!.Name);
        Assert.Equal(dto.Description, created.Description);
        Assert.True(created.Id > 0);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task Create_InvalidData_ReturnsBadRequest()
    {
        var invalidDto = new CreateScenarioDto
        {
            Name = string.Empty,
            Description = string.Empty
        };

        var response = await _client.PostAsJsonAsync("/api/scenarios", invalidDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingScenario_ReturnsNoContent()
    {
        var dto = new CreateScenarioDto
        {
            Name = "Update Test Scenario",
            Description = "Create scenario for update"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/scenarios", dto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ScenarioResponseDto>();
        Assert.NotNull(created);

        var updateDto = new UpdateScenarioDto
        {
            Name = "Updated Scenario Name",
            Description = "Updated description"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/scenarios/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/scenarios/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var updated = await getResponse.Content.ReadFromJsonAsync<ScenarioResponseDto>();

        Assert.NotNull(updated);
        Assert.Equal(updateDto.Name, updated!.Name);
        Assert.Equal(updateDto.Description, updated.Description);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var updateDto = new UpdateScenarioDto
        {
            Name = "Missing Scenario",
            Description = "This scenario id does not exist"
        };

        var response = await _client.PutAsJsonAsync("/api/scenarios/999999", updateDto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingScenario_RemovesScenario()
    {
        var dto = new CreateScenarioDto
        {
            Name = "Delete Test Scenario",
            Description = "Create scenario for delete"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/scenarios", dto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ScenarioResponseDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/scenarios/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/scenarios/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/scenarios/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
