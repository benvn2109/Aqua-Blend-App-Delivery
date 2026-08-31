using System.Net;
using System.Net.Http.Json;
using AquaBlend.Api.Authorization;
using AquaBlend.DTOs.Scenarios;

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
}