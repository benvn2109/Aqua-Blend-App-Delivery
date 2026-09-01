using System.Globalization;
using System.Net;
using System.Text.Json;

namespace AquaBlend.Tests;

public class HealthEndpointTests : IDisposable
{
    private readonly AquaBlendApiFactory _factory;
    private readonly HttpClient _client;

    public HealthEndpointTests()
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
    public async Task Anonymous_GetHealth_ReturnsOk()
    {
        // X-Test-Anonymous makes TestAuthHandler return AuthenticateResult.NoResult(),
        // i.e. no principal at all - this is a genuinely unauthenticated request, not
        // just one authenticated with a low-privilege role.
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_GetHealth_ReturnsExpectedBody()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal("healthy", root.GetProperty("status").GetString());
        Assert.Equal("AquaBlend.Api", root.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Anonymous_GetHealth_ReturnsValidUtcTimestamp()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var timestampRaw = document.RootElement.GetProperty("timestamp").GetString();

        Assert.False(string.IsNullOrWhiteSpace(timestampRaw));

        var parsed = DateTime.Parse(
            timestampRaw!,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
    }
}
