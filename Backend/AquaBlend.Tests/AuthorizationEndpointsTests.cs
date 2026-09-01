using System.Net;
using System.Net.Http.Json;
using AquaBlend.Api.Authorization;
using AquaBlend.DTOs.Scenarios;
using AquaBlend.DTOs.WaterSources;

namespace AquaBlend.Tests;

public class AuthorizationEndpointsTests : IDisposable
{
    private readonly AquaBlendApiFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationEndpointsTests()
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
    public async Task Anonymous_GetScenarios_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await _client.GetAsync("/api/scenarios");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CreateScenario_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", AppRoles.Viewer);

        var dto = new CreateScenarioDto
        {
            Name = "Viewer Test Scenario",
            Description = "Viewer must not create scenarios"
        };

        var response = await _client.PostAsJsonAsync("/api/scenarios", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_GetWaterSources_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await _client.GetAsync("/api/water-sources");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_GetWaterSources_ReturnsOk()
    {
        // CanView allows Admin, Analyst and Viewer - confirms the read side
        // isn't accidentally locked down as tightly as the write side.
        _client.DefaultRequestHeaders.Add("X-Test-Role", AppRoles.Viewer);

        var response = await _client.GetAsync("/api/water-sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CreateWaterSource_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", AppRoles.Viewer);

        var dto = new CreateWaterSourceDto
        {
            Name = "Viewer Test Source",
            Type = "Reservoir"
        };

        var response = await _client.PostAsJsonAsync("/api/water-sources", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Analyst_CreateWaterSource_ReturnsForbidden()
    {
        // Water Sources use CanAdminister (Admin only) rather than CanAnalyse,
        // unlike Scenarios - an Analyst must be rejected here even though the
        // same role is allowed to create Scenarios.
        _client.DefaultRequestHeaders.Add("X-Test-Role", AppRoles.Analyst);

        var dto = new CreateWaterSourceDto
        {
            Name = "Analyst Test Source",
            Type = "Reservoir"
        };

        var response = await _client.PostAsJsonAsync("/api/water-sources", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Analyst_DeleteWaterSource_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", AppRoles.Analyst);

        var response = await _client.DeleteAsync("/api/water-sources/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreateWaterSource_ReturnsCreated()
    {
        // Positive control: CanAdminister must still let the intended role through.
        _client.DefaultRequestHeaders.Add("X-Test-Role", AppRoles.Admin);

        var dto = new CreateWaterSourceDto
        {
            Name = "Admin Test Source",
            Type = "Reservoir"
        };

        var response = await _client.PostAsJsonAsync("/api/water-sources", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}