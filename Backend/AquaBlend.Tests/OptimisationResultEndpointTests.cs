using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AquaBlend.Data;
using AquaBlend.DTOs.OptimisationResults;
using AquaBlend.Entities;
using Microsoft.Extensions.DependencyInjection;
 
namespace AquaBlend.Tests;
 
public class OptimisationResultEndpointTests : IDisposable
{
    private readonly AquaBlendApiFactory _factory;
    private readonly HttpClient _client;
 
    public OptimisationResultEndpointTests()
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
    public async Task GetAll_ReturnsSummariesWithoutResultJson()
    {
        var response = await _client.GetAsync("/api/optimisation-results");
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        // Parse the raw body rather than the DTO: System.Text.Json silently
        // ignores unknown properties on deserialize, so binding to
        // OptimisationResultSummaryDto would not notice an extra resultJson
        // field leaking through the wire format.
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var items = document.RootElement;
 
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(items.GetArrayLength() > 0);
 
        foreach (var item in items.EnumerateArray())
        {
            foreach (var property in item.EnumerateObject())
            {
                Assert.False(
                    string.Equals(property.Name, "resultJson", StringComparison.OrdinalIgnoreCase),
                    "Summary response must not include resultJson - it is a large payload reserved for the by-id endpoint.");
            }
        }
    }
 
    [Fact]
    public async Task GetById_ReturnsResultJsonAsNestedObject()
    {
        var listResponse = await _client.GetAsync("/api/optimisation-results");
        listResponse.EnsureSuccessStatusCode();
        var summaries = await listResponse.Content.ReadFromJsonAsync<List<OptimisationResultSummaryDto>>();
 
        Assert.NotNull(summaries);
        var seeded = summaries!.First();
 
        var response = await _client.GetAsync($"/api/optimisation-results/{seeded.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        // Parse the raw body, not the DTO. Deserializing straight into
        // OptimisationResultResponseDto would pass even if resultJson were
        // serialised as an escaped string ("{\"scenario_id\":...") instead
        // of a nested object - that's the exact bug this test guards against.
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var resultJson = document.RootElement.GetProperty("resultJson");
 
        Assert.Equal(JsonValueKind.Object, resultJson.ValueKind);
        Assert.Equal("scenario_2026_07_17_001", resultJson.GetProperty("scenario_id").GetString());
    }
 
    [Fact]
    public async Task GetByScenario_ReturnsOnlyMatchingScenarioResults()
    {
        var seededList = await _client.GetFromJsonAsync<List<OptimisationResultSummaryDto>>(
            "/api/optimisation-results");
        Assert.NotNull(seededList);
        var seededScenarioId = seededList!.First().ScenarioId;
 
        // With only the one seeded scenario, a handler that ignored the
        // route parameter and returned every result would still pass a
        // "matches the requested scenario" assertion. Seed a second
        // scenario/result pair so the filter is actually exercised.
        int otherScenarioId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AquaBlendDbContext>();
 
            var otherScenario = new Scenario
            {
                Name = "Other Scenario",
                Description = "Second scenario for GetByScenario filter test"
            };
            context.Scenarios.Add(otherScenario);
            await context.SaveChangesAsync();
            otherScenarioId = otherScenario.Id;
 
            context.OptimisationResults.Add(new OptimisationResult
            {
                ScenarioId = otherScenarioId,
                Status = "OPTIMAL",
                SolvedAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                ContractVersion = "1.0",
                ResultJson = "{\"scenario_id\":\"other-scenario\"}",
                TotalCost = 1.00m,
                Currency = "AUD"
            });
            await context.SaveChangesAsync();
        }
 
        // Confirm the second result actually landed. Without this the filter
        // assertions below would pass vacuously against a single-result
        // database - the exact flaw the second scenario exists to prevent.
        var allResults = await _client.GetFromJsonAsync<List<OptimisationResultSummaryDto>>(
            "/api/optimisation-results");
        Assert.NotNull(allResults);
        Assert.Equal(2, allResults!.Count);
 
        var response = await _client.GetAsync($"/api/optimisation-results/scenario/{seededScenarioId}");
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        var results = await response.Content.ReadFromJsonAsync<List<OptimisationResultSummaryDto>>();
 
        Assert.NotNull(results);
        Assert.NotEmpty(results!);
        Assert.All(results!, r => Assert.Equal(seededScenarioId, r.ScenarioId));
    }
 
    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/optimisation-results/999999");
 
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
 
    [Theory]
    [InlineData("/api/optimisation-results")]
    [InlineData("/api/optimisation-results/1")]
    [InlineData("/api/optimisation-results/scenario/1")]
    public async Task Anonymous_Endpoints_ReturnUnauthorized(string url)
    {
        // X-Test-Anonymous makes TestAuthHandler return AuthenticateResult.NoResult(),
        // i.e. no principal at all. Without it the client authenticates as
        // Admin by default and the CanView policy goes untested.
        //
        // Authorisation short-circuits before the handler runs, so the
        // hardcoded ids never reach a lookup - a 401 is expected regardless
        // of whether those records exist.
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
 
        var response = await _client.GetAsync(url);
 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}